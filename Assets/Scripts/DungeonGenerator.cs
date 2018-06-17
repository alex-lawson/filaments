using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class DungeonGenerationPhaseConfig {
    public string PhaseName;
    public int PartLimit;
    public DungeonPart[] PartPool;
}

public class DungeonGenerator : MonoBehaviour {
    private class DungeonGenerationPhaseStatus {
        public DungeonGenerationPhaseConfig Config;
        public int RemainingPartCount;
        public List<DungeonPart> PartPool;
        public List<DungeonConnector> OpenOutbound;
        public List<DungeonConnector> FailedOutbound;

        public DungeonGenerationPhaseStatus(DungeonGenerationPhaseConfig config) {
            Config = config;
            RemainingPartCount = config.PartLimit;
            // copy the part pool so that we can safely shuffle without disrupting static randomization
            PartPool = new List<DungeonPart>(config.PartPool);
            OpenOutbound = new List<DungeonConnector>();
            FailedOutbound = new List<DungeonConnector>();
        }
    }

    public int Seed;
    public bool UseRandomSeed = true;
    public List<DungeonPart> StartParts;
    public DungeonGenerationPhaseConfig[] Phases;
    public UnityEvent OnGenerationComplete;
    [HideInInspector] public bool Generating { get; private set; }

    private List<DungeonPart> currentDungeonPartInstances = new List<DungeonPart>();
    private DungeonGenerationPhaseStatus currentPhaseStatus;
    private Random.State randomState;

    private void Awake() {
        OnGenerationComplete = new UnityEvent();
    }

    /// <summary>
    /// Clears the current dungeon and Destroy all pieces. A new dungeon cannot be generated
    /// until after the next physics update clears colliders for these destroyed objects
    /// </summary>
    public void Clear() {
        for (var i = 0; i < currentDungeonPartInstances.Count; i++) {
            Destroy(currentDungeonPartInstances[i].gameObject);
        }
        currentDungeonPartInstances.Clear();

        Generating = false;
        currentPhaseStatus = null;
    }

    /// <summary>
    /// Triggers generation of a new dungeon. If synchronous is true, the generation will be
    /// completed synchronously. If false, generation will need to be advanced by repeated
    /// calls to StepGeneration (until StepGeneration returns false)
    /// </summary>
    public void Generate(bool synchronous = true) {
        if (StartParts.Count == 0) {
            Debug.LogError("Generation failed: no Start Parts configured!");
            return;
        } else if (Phases.Length == 0) {
            Debug.LogError("Generation failed: no Phases configured!");
            return;
        }

        Generating = true;

        if (UseRandomSeed)
            Seed = Random.Range(int.MinValue, int.MaxValue);

        var oldRandomState = Random.state;
        Random.InitState(Seed);

        DungeonPart startPart = StartParts.RandomElement();
        var startPartInstance = Instantiate(startPart.gameObject, transform).GetComponent<DungeonPart>();
        currentDungeonPartInstances.Add(startPartInstance);

        currentPhaseStatus = new DungeonGenerationPhaseStatus(Phases[0]);
        currentPhaseStatus.OpenOutbound.AddRange(startPartInstance.OutboundConnectors());

        randomState = Random.state;
        Random.state = oldRandomState;

        if (synchronous) {
            while (Generating)
                StepGeneration();
        }
    }

    /// <summary>
    /// Attempts to satisfy one connector in the current phase or advance to the next phase
    /// Returns true if generation is still in progress, false if it is complete or has failed
    /// </summary>
    public bool StepGeneration() {
        if (Generating) {
            var oldRandomState = Random.state;
            Random.state = randomState;

            if (currentPhaseStatus.RemainingPartCount > 0 && currentPhaseStatus.OpenOutbound.Count > 0) {
                DungeonConnector outboundConnector = currentPhaseStatus.OpenOutbound.RandomElement();

                // Try to place each potentially connectable part (in random order)
                currentPhaseStatus.PartPool.Shuffle();

                foreach (var candPart in currentPhaseStatus.PartPool) {
                    if (candPart.HasInboundConnector(outboundConnector.ConnectionTag)) {
                        if (TryPlacePart(candPart, outboundConnector)) {
                            currentPhaseStatus.RemainingPartCount--;
                            break;
                        }
                    }
                }

                // Store failed connectors to be attempted in later phases
                if (currentPhaseStatus.OpenOutbound.Remove(outboundConnector))
                    currentPhaseStatus.FailedOutbound.Add(outboundConnector);
            } else {
                //var placedPhaseParts = currentPhaseStatus.Config.PartLimit - currentPhaseStatus.RemainingPartCount;
                //Debug.Log($"Phase '{currentPhaseStatus.Config.PhaseName}' complete: {placedPhaseParts} parts placed, " +
                //    $"{currentPhaseStatus.FailedOutbound.Count + currentPhaseStatus.OpenOutbound.Count} open connectors");

                Generating = AdvanceGenerationPhase();
            }

            randomState = Random.state;
            Random.state = oldRandomState;

            if (!Generating && OnGenerationComplete != null)
                OnGenerationComplete.Invoke();
        }

        return Generating;
    }

    /// <summary>
    /// Attempts to start the next generation phase, if available
    /// Returns true if a new phase was started, false if not (e.g. this was the last phase)
    /// </summary>
    private bool AdvanceGenerationPhase() {
        int phaseIndex = Array.IndexOf(Phases, currentPhaseStatus.Config);
        phaseIndex++;

        if (phaseIndex > 0 && phaseIndex < Phases.Length) {
            var newPhaseStatus = new DungeonGenerationPhaseStatus(Phases[phaseIndex]);
            newPhaseStatus.OpenOutbound.AddRange(currentPhaseStatus.OpenOutbound);
            newPhaseStatus.OpenOutbound.AddRange(currentPhaseStatus.FailedOutbound);
            currentPhaseStatus = newPhaseStatus;

            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Attempts to attach the specified part to the specified outbound connector, performing bounds
    /// checks as appropriate. Will attempt placement with all valid matching inbound connectors
    /// Returns true if the part was placed successfully, false if not
    /// </summary>
    private bool TryPlacePart(DungeonPart partPrefab, DungeonConnector outboundConnector) {
        Quaternion targetOrientation = new Quaternion();
        Vector3 targetPosition = Vector3.zero;
        int? inboundConnectorId = null;

        var prefabICs = partPrefab.InboundConnectorsFor(outboundConnector);
        prefabICs.Shuffle();

        foreach (var prefabIC in prefabICs) {
            // calculate prospective orientation and position for part instance
            targetOrientation = Quaternion.AngleAxis(180, outboundConnector.transform.up) * outboundConnector.transform.rotation * Quaternion.Inverse(prefabIC.transform.localRotation);
            targetPosition = outboundConnector.transform.position - targetOrientation * prefabIC.transform.localPosition;

            bool canPlace = partPrefab.SkipBoundsCheck || partPrefab.BoundsCheck(outboundConnector.transform, prefabIC.ConnectorId);

            if (canPlace) {
                inboundConnectorId = prefabIC.ConnectorId;
                break;
            } else {
                //Debug.Log($"Bounds check failed! Can't connect {outboundConnector.gameObject.name} (outbound) to {partPrefab.PartName} {prefabIC.gameObject.name} (inbound)");
            }
        }

        if (!inboundConnectorId.HasValue)
            return false;

        var partInstance = Instantiate(partPrefab.gameObject, transform).GetComponent<DungeonPart>();
        currentDungeonPartInstances.Add(partInstance);

        var inboundConnector = partInstance.GetConnector(inboundConnectorId.Value);

        // random rotation added separately from bounds check so that bounds check is deterministic
        if (inboundConnector.RandomRotation || outboundConnector.RandomRotation) {
            float rotateDegrees = Random.Range(0, 360);
            targetOrientation = Quaternion.AngleAxis(180, outboundConnector.transform.up)
                    * Quaternion.AngleAxis(rotateDegrees, outboundConnector.transform.forward)
                    * outboundConnector.transform.rotation
                    * Quaternion.Inverse(inboundConnector.transform.localRotation);
            targetPosition = outboundConnector.transform.position - targetOrientation * inboundConnector.transform.localPosition;
        }

        partInstance.transform.localRotation = targetOrientation;
        partInstance.transform.position = targetPosition;

        //Debug.Log($"Connected {outboundConnector.gameObject.name} (outbound) to {partPrefab.PartName} {inboundConnector.gameObject.name} (inbound)");

        var nextOutboundConnectors = partInstance.OutboundConnectors();
        nextOutboundConnectors.Remove(inboundConnector);
        currentPhaseStatus.OpenOutbound.Remove(outboundConnector);

        // check whether this part makes additional connections to other open connectors
        for (var i = nextOutboundConnectors.Count - 1; i > 0; i--) {
            var connA = nextOutboundConnectors[i];
            for (var j = currentPhaseStatus.OpenOutbound.Count - 1; j > 0; j--) {
                var connB = currentPhaseStatus.OpenOutbound[j];
                if (connA.CanConnectTo(connB)) {
                    if ((connA.transform.position - connB.transform.position).sqrMagnitude < 0.01f) {
                        nextOutboundConnectors.RemoveAt(i);
                        currentPhaseStatus.OpenOutbound.RemoveAt(j);
                        //Debug.Log($"Detected additional connection at {connA.transform.position} between {connA.gameObject.name} and {connB.gameObject.name}, closing both.");
                    }
                }
            }
        }

        currentPhaseStatus.OpenOutbound.AddRange(nextOutboundConnectors);

        return true;
    }
}
