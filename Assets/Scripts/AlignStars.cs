using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignStars : MonoBehaviour {

    public Transform Sun;
    public Transform Moon;
	
	void Update () {
        Sun.rotation = Quaternion.AngleAxis(90, transform.right) * transform.rotation;
        Moon.rotation = Quaternion.AngleAxis(-90, transform.right) * transform.rotation;
    }
}
