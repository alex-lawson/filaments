using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeaconActivator : MonoBehaviour {

	void Update () {
        ShrineBeacon.ActivatorPosition = transform.position;
	}
}
