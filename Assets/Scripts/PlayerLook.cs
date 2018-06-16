using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour {

    public Transform PlayerBody;
    public float MouseSensitivity;
    public float VerticalLimit;

	private void Update () {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RotateCamera();
        RotatePlayerBody();
	}

    private void RotateCamera() {
        float mouseY = Input.GetAxis("Mouse Y");

        var cameraRot = transform.rotation.eulerAngles;

        cameraRot.x -= mouseY * MouseSensitivity;
        if (cameraRot.x > 180f)
            cameraRot.x -= 360f;
        cameraRot.x = Mathf.Clamp(cameraRot.x, -VerticalLimit, VerticalLimit);

        transform.rotation = Quaternion.Euler(cameraRot);
    }

    private void RotatePlayerBody() {
        float mouseX = Input.GetAxis("Mouse X");

        var playerBodyRot = PlayerBody.rotation.eulerAngles;

        playerBodyRot.y += mouseX * MouseSensitivity;

        PlayerBody.rotation = Quaternion.Euler(playerBodyRot);
    }
}
