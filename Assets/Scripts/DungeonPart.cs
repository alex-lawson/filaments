using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class DungeonPart : MonoBehaviour {

    public string PartName;
    public bool CanBeStart;
    
    public List<DungeonConnector> GetConnectors(bool requireInbound = true, bool requireOutbound = true) {
        var res = new List<DungeonConnector>(GetComponentsInChildren<DungeonConnector>());
        res = res.Where(c => (!requireInbound || c.AllowInbound) && (!requireOutbound || c.AllowOutbound)).ToList();
        return res;
    }
}
