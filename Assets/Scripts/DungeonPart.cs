using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DungeonPart : MonoBehaviour {

    public string PartName;
    public bool SkipBoundsCheck = false;
    public Vector3 ExtraBoundsExpansion;

    [SerializeField] private HashSet<string> inboundConnectorTags;
    [SerializeField] private DungeonConnector[] connectors;
    [SerializeField] private Bounds[] bounds;

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

    public bool BoundsCheck(Transform outboundConnector, int inboundConnectorId) {
        var connectorBounds = bounds[inboundConnectorId];
        Vector3 boundsCheckPosition = outboundConnector.TransformPoint(connectorBounds.center);
        return !Physics.CheckBox(boundsCheckPosition, connectorBounds.extents, outboundConnector.rotation);
    }

    // When part is changed in the editor, compute and cache a set of connectable inbound tags,
    // a list of connectors, and the combined bounds of the object group
    private void OnValidate() {
        inboundConnectorTags = new HashSet<string>();
        connectors = GetComponentsInChildren<DungeonConnector>().ToArray();
        for (int i = 0; i < connectors.Length; i++) {
            connectors[i].ConnectorId = i;
            if (connectors[i].AllowInbound)
                inboundConnectorTags.Add(connectors[i].ConnectionTag);
        }
            
        ComputeBounds();
    }

    private void ComputeBounds() {
        bounds = new Bounds[connectors.Length];

        // Since bounding boxes are axis-aligned, build a separate bounding box in local space
        // for each connector by manually expanding each point on each mesh in the part
        // This allows inbound connectors to have arbitrary orientations with fewer limitations
        for (int connectorId = 0; connectorId < connectors.Length; connectorId++) {
            var connector = connectors[connectorId];
            Quaternion flipFacing = Quaternion.AngleAxis(180, Vector3.up);

            Bounds newBounds = new Bounds();
            MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < meshes.Length; i++) {
                var meshTransform = meshes[i].transform;
                Mesh mesh = meshes[i].sharedMesh;
                int vc = mesh.vertexCount;
                var vertices = mesh.vertices;
                for (int j = 0; j < vc; j++) {
                    var meshPoint = meshTransform.TransformPoint(mesh.vertices[j]);
                    var relativePoint = flipFacing * connector.transform.InverseTransformPoint(meshPoint);
                    if (i == 0 && j == 0) {
                        newBounds = new Bounds(relativePoint, Vector3.zero);
                    } else {
                        newBounds.Encapsulate(relativePoint);
                    }
                }
            }

            // extra bounds expansion does *not* expand toward the connector (to preserve
            // a flat plane for tight packing)
            newBounds.Expand(ExtraBoundsExpansion);
            newBounds.center += new Vector3(0, 0, ExtraBoundsExpansion.z * 0.5f);

            // shrink slightly to allow tight packing
            newBounds.Expand(-0.01f);

            bounds[connectorId] = newBounds;
        }
    }
}
