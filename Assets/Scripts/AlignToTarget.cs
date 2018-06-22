using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignToTarget : MonoBehaviour {

    public Transform Target;

	private void Update () {
        Align();
	}

    private void Align() {
        transform.rotation = Target.rotation;
    }
}
