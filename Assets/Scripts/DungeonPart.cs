using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DungeonPart : MonoBehaviour {

    public string PartName;
    public bool PlaceInStartPhase = true;
    public bool PlaceInMainPhase = true;
    public bool PlaceInEndPhase = true;
    public bool SkipBoundsCheck = false;

    [SerializeField] public Bounds Bounds { get; private set; }

    [SerializeField] private HashSet<string> inboundConnectorTags;
    [SerializeField] private List<DungeonConnector> connectors;

    public bool HasInboundConnector(string connectorTag) {
        return inboundConnectorTags.Contains(connectorTag);
    }

    public List<DungeonConnector> InboundConnectorsFor(DungeonConnector outboundConnector) {
        return connectors.Where(c => c.AllowInbound && c.CanConnectTo(outboundConnector)).ToList();
    }

    public List<DungeonConnector> InboundConnectors() {
        return connectors.Where(c => c.AllowInbound).ToList();
    }

    public List<DungeonConnector> OutboundConnectors() {
        return connectors.Where(c => c.AllowOutbound).ToList();
    }

    public DungeonConnector GetConnector(int connectorId) {
        return connectors[connectorId];
    }

    // When part is changed in the editor, compute and cache a set of connectable inbound tags,
    // a list of connectors, and the combined bounds of the object group
    private void OnValidate() {
        inboundConnectorTags = new HashSet<string>();
        connectors = new List<DungeonConnector>(GetComponentsInChildren<DungeonConnector>());
        for (var i = 0; i < connectors.Count; i++) {
            connectors[i].ConnectorId = i;
            if (connectors[i].AllowInbound)
                inboundConnectorTags.Add(connectors[i].ConnectionTag);
        }
            
        ComputeBounds();
    }

    private void ComputeBounds() {
        var newBounds = new Bounds(Vector3.zero, Vector3.zero);

        // encapsulate all *meshes* (rather than colliders) because collider bound extents
        // are always (0, 0, 0) until instantiated in scene
        var meshes = GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in meshes)
            newBounds.Encapsulate(mesh.bounds);

        // shrink slightly to allow tight packing
        newBounds.Expand(-0.01f); 

        Bounds = newBounds;
    }
}
