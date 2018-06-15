using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DungeonPart : MonoBehaviour {

    public string PartName;
    public bool CanBeStart;
    public bool CanBeEnd;
    public bool SkipBoundsCheck = false;

    // auto populated, don't mess with 'em
    public List<DungeonConnector> Connectors;
    public Bounds PartBounds;

    public List<DungeonConnector> InboundConnectorsFor(DungeonConnector outboundConnector) {
        return Connectors.Where(c => c.AllowInbound && c.CanConnectTo(outboundConnector)).ToList();
    }

    public List<DungeonConnector> InboundConnectors() {
        return Connectors.Where(c => c.AllowInbound).ToList();
    }

    public List<DungeonConnector> OutboundConnectors() {
        return Connectors.Where(c => c.AllowOutbound).ToList();
    }

    public DungeonConnector GetConnector(int connectorId) {
        return Connectors[connectorId];
    }

    private void OnValidate() {
        Connectors = new List<DungeonConnector>(GetComponentsInChildren<DungeonConnector>());
        for (var i = 0; i < Connectors.Count; i++)
            Connectors[i].ConnectorId = i;
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

        PartBounds = newBounds;
    }
}
