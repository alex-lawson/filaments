using UnityEngine;

public class Spin : MonoBehaviour {

    public float SpinRate = 30;

    void Update() {
        Quaternion rotation = Quaternion.AngleAxis(SpinRate * Time.deltaTime, Vector3.up);
        transform.rotation = rotation * transform.rotation;
    }
}
