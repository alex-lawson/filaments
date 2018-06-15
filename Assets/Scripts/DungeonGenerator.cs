using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class DungeonGenerator : MonoBehaviour {

    public int PartLimit;
    public int EndPartLimit;
    public DungeonPart[] PartPrefabs;

    private Dictionary<string, List<int>> connectableParts = new Dictionary<string, List<int>>();
    private List<int> startParts = new List<int>();
    private List<DungeonPart> partInstances = new List<DungeonPart>();
    private List<DungeonConnector> openOutbound = new List<DungeonConnector>();

	private void Start () {
        DigestPrefabs();
        GenerateDungeon();
	}

	private void Update () {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (partInstances.Count > 0)
                ClearDungeon();
            else
                GenerateDungeon();
        }
	}

    private void DigestPrefabs() {
        for (var i = 0; i < PartPrefabs.Length; i++) {
            DungeonPart part = PartPrefabs[i];

            if (part.CanBeStart)
                startParts.Add(i);

            foreach (DungeonConnector connector in part.InboundConnectors()) {
                if (!connectableParts.ContainsKey(connector.ConnectionTag))
                    connectableParts.Add(connector.ConnectionTag, new List<int>());
                connectableParts[connector.ConnectionTag].Add(i);
            }
        }
    }

    public void ClearDungeon() {
        for (var i = 0; i < partInstances.Count; i++) {
            Destroy(partInstances[i].gameObject);
        }
        partInstances.Clear();
        openOutbound.Clear();
    }

    public void GenerateDungeon() {
        int partsRemaining = PartLimit;
        int endPartsRemaining = EndPartLimit;

        ClearDungeon();

        DungeonPart startPart = PartPrefabs[startParts.RandomElement()];

        var startPartInstance = Instantiate(startPart.gameObject, transform).GetComponent<DungeonPart>();
        partInstances.Add(startPartInstance);

        openOutbound.AddRange(startPartInstance.OutboundConnectors());

        while (openOutbound.Count > 0 && (partsRemaining > 0 || endPartsRemaining > 0)) {
            DungeonConnector outboundConnector = openOutbound.RandomElement();

            // try to place each valid connectable part (in random order)
            List<int> candIds;
            if (connectableParts.TryGetValue(outboundConnector.ConnectionTag, out candIds)) {
                candIds.Shuffle();
                foreach (var candId in candIds) {
                    DungeonPart candPart = PartPrefabs[candId];

                    if (partsRemaining <= 0 && !candPart.CanBeEnd)
                        continue;

                    if (TryPlacePart(candPart, outboundConnector)) {
                        if (partsRemaining > 0)
                            partsRemaining--;
                        else
                            endPartsRemaining--;

                        break;
                    }
                }
            }

            openOutbound.Remove(outboundConnector);
        }
    }

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

            bool canPlace = partPrefab.SkipBoundsCheck;

            if (!canPlace) {
                // check bounding box to see whether it can be placed
                Vector3 boundsCheckPosition = targetPosition + targetOrientation * partPrefab.PartBounds.center;
                canPlace = !Physics.CheckBox(boundsCheckPosition, partPrefab.PartBounds.extents, targetOrientation);
            }

            if (canPlace) {
                inboundConnectorId = prefabIC.ConnectorId;
                break;
            } else {
                //Debug.Log($"Bounds check failed! Can't connect {outboundConnector.gameObject.name} (outbound) to {prefabIC.gameObject.name} (inbound)");
            }
        }

        if (!inboundConnectorId.HasValue)
            return false;

        var partInstance = Instantiate(partPrefab.gameObject, transform).GetComponent<DungeonPart>();
        partInstances.Add(partInstance);

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
        partInstance.transform.localPosition = targetPosition;

        //Debug.Log($"Connected {outboundConnector.gameObject.name} (outbound) to {inboundConnector.gameObject.name} (inbound)");

        var nextOutboundConnectors = partInstance.OutboundConnectors();
        nextOutboundConnectors.Remove(inboundConnector);
        openOutbound.Remove(outboundConnector);

        // check whether this part makes additional connections to other open connectors
        for (var i = nextOutboundConnectors.Count - 1; i > 0; i--) {
            var connA = nextOutboundConnectors[i];
            for (var j = openOutbound.Count - 1; j > 0; j--) {
                var connB = openOutbound[j];
                if (connA.CanConnectTo(connB)) {
                    if ((connA.transform.position - connB.transform.position).sqrMagnitude < 0.001f) {
                        nextOutboundConnectors.RemoveAt(i);
                        openOutbound.RemoveAt(j);
                        //Debug.Log($"Detected additional connection between {connA.gameObject.name} and {connB.gameObject.name}, closing both.");
                    }
                }
            }
        }

        openOutbound.AddRange(nextOutboundConnectors);

        return true;
    }
}
