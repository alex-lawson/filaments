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

    public void Reset() {
        transform.rotation = new Quaternion();
    }

    private void RotateCamera() {
        float mouseY = Input.GetAxis("Mouse Y");
        float rotateAmount = -mouseY * MouseSensitivity;

        float toUp = Vector3.Angle(PlayerBody.up, transform.forward);
        if (rotateAmount < 0) {
            rotateAmount = Mathf.Max(-toUp + 0.1f, rotateAmount);
        } else if (rotateAmount > 0) {
            rotateAmount = Mathf.Min(179.9f - toUp, rotateAmount);
        }

        transform.rotation = Quaternion.AngleAxis(rotateAmount, transform.right) * transform.rotation;
    }

    private void RotatePlayerBody() {
        float mouseX = Input.GetAxis("Mouse X");
        float rotateAmount = mouseX * MouseSensitivity;

        PlayerBody.rotation = Quaternion.AngleAxis(rotateAmount, PlayerBody.up) * PlayerBody.rotation;
    }
}
