using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStickyLook : MonoBehaviour {

    public Transform PlayerBody;
    public float MouseSensitivity;

	private void Update () {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RotateCamera();
        RotatePlayerBody();
    }

    private void RotateCamera() {
        float mouseY = Input.GetAxis("Mouse Y");
        float rotateAmount = -mouseY * MouseSensitivity;

        transform.rotation = Quaternion.AngleAxis(rotateAmount, transform.right) * transform.rotation;
    }

    private void RotatePlayerBody() {
        float mouseX = Input.GetAxis("Mouse X");
        float rotateAmount = mouseX * MouseSensitivity;

        PlayerBody.rotation = Quaternion.AngleAxis(rotateAmount, PlayerBody.up) * PlayerBody.rotation;
    }
}
