using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class DungeonGenerator : MonoBehaviour {

    public int PartLimit;
    public DungeonPart[] PartPrefabs;

    private Dictionary<string, List<int>> connectableParts = new Dictionary<string, List<int>>();
    private List<int> startParts = new List<int>();
    private List<GameObject> currentInstances = new List<GameObject>();

	private void Start () {
        DigestPrefabs();
        GenerateDungeon();
	}

	private void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
            GenerateDungeon();
	}

    private void DigestPrefabs() {
        for (var i = 0; i < PartPrefabs.Length; i++) {
            DungeonPart part = PartPrefabs[i];

            if (part.CanBeStart)
                startParts.Add(i);

            var partInboundConnectors = part.GetConnectors(true, false);
            foreach (DungeonConnector connector in partInboundConnectors) {
                if (!connectableParts.ContainsKey(connector.ConnectionTag))
                    connectableParts.Add(connector.ConnectionTag, new List<int>());
                connectableParts[connector.ConnectionTag].Add(i);
            }
        }
    }

    public void GenerateDungeon() {
        int partsRemaining = PartLimit;

        ClearDungeon();

        List<DungeonConnector> open = new List<DungeonConnector>();

        DungeonPart startPart = PartPrefabs[startParts.RandomElement()];

        var startPartInstance = Instantiate(startPart.gameObject, transform).GetComponent<DungeonPart>();
        currentInstances.Add(startPartInstance.gameObject);

        open.AddRange(startPartInstance.GetConnectors(false, true));

        while (open.Count > 0 && partsRemaining > 0) {
            DungeonConnector outboundConnector = open.RandomElement();

            List<int> cands;
            if (connectableParts.TryGetValue(outboundConnector.ConnectionTag, out cands)) {
                if (cands.Count > 0) {
                    int nextPartId = cands.RandomElement();
                    DungeonPart nextPart = PartPrefabs[nextPartId];

                    var nextPartInstance = Instantiate(nextPart.gameObject, transform).GetComponent<DungeonPart>();
                    currentInstances.Add(nextPartInstance.gameObject);

                    DungeonConnector inboundConnector = null;

                    var nextInboundConnectors = nextPartInstance.GetConnectors(true, false);
                    nextInboundConnectors.Shuffle();
                    foreach (var c in nextInboundConnectors) {
                        if (c.ConnectionTag == outboundConnector.ConnectionTag) {
                            inboundConnector = c;
                            break;
                        }
                    }

                    Assert.IsNotNull(inboundConnector);

                    // rotate the placed part based on the combined rotation of the connectors, plus the facing flip between them
                    nextPartInstance.transform.localRotation = Quaternion.AngleAxis(180, outboundConnector.transform.up);
                    nextPartInstance.transform.localRotation *= outboundConnector.transform.rotation * Quaternion.Inverse(inboundConnector.transform.localRotation);

                    // move the placed part so that the connectors overlap
                    nextPartInstance.transform.localPosition = outboundConnector.transform.position - inboundConnector.transform.position;

                    //Debug.Log($"Connected {outboundConnector.gameObject.name} (outbound) to {inboundConnector.gameObject.name} (inbound)");

                    var nextOutboundConnectors = nextPartInstance.GetConnectors(false, true);
                    nextOutboundConnectors.Remove(inboundConnector);
                    open.AddRange(nextOutboundConnectors);

                    partsRemaining--;
                }
            }

            open.Remove(outboundConnector);
        }
    }

    public void ClearDungeon() {
        for (var i = 0; i < currentInstances.Count; i++) {
            Destroy(currentInstances[i]);
        }
        currentInstances.Clear();
    }
}
