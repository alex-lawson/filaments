using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shrine : MonoBehaviour {

    public Light Beacon;
    public float BeaconDistance;
    public bool BeaconEnabled = true;

	private void Start () {
        Beacon.gameObject.SetActive(BeaconEnabled);
	}

	private void Update () {
		
	}

    public void SetShrineHeight(float height) {
        Beacon.transform.position = new Vector3(0, height + BeaconDistance, 0);
    }
}
