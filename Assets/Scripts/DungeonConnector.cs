using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonConnector : MonoBehaviour {

    public string ConnectionTag;
    public bool AllowOutbound = true;
    public bool AllowInbound = true;
    public bool RandomRotation = false;

    // auto-assigned by DungeonPart
    public int ConnectorId;

    public bool CanConnectTo(DungeonConnector other) {
        return other != this
                && (ConnectionTag == other.ConnectionTag)
                && ((AllowOutbound && other.AllowInbound) || (AllowInbound && other.AllowOutbound));
    }
}
