using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    public bool AutoRun;
    public float WalkSpeed;
    public float RunSpeed;
    public float AirControl;
    public float JumpSpeed;
    public float Gravity;

    private CharacterController characterController;

    private void Awake () {
        characterController = GetComponent<CharacterController>();
    }

    private void Update () {
        MovePlayer();
    }

    private void MovePlayer() {
        Vector3 movement = Vector3.zero;

        // get normalized horizontal input direction in local space
        float inputLat = Input.GetAxis("Horizontal");
        float inputLon = Input.GetAxis("Vertical");
        Vector3 hControlDir = new Vector3(inputLat, 0, inputLon).normalized;
        hControlDir = transform.TransformDirection(hControlDir);

        if (characterController.isGrounded) {
            // if on ground, use pure horizontal movement without friction
            bool running = Input.GetKey(KeyCode.LeftShift) ^ AutoRun;
            hControlDir *= running ? RunSpeed : WalkSpeed;
            movement.x = hControlDir.x;
            movement.z = hControlDir.z;

            // handle jumps
            if (Input.GetKeyDown(KeyCode.Space))
                movement.y += JumpSpeed;

        } else {
            // if in air, apply control over time, clamped to maximum horizontal move speed
            hControlDir *= AirControl * Time.deltaTime;
            movement.x = characterController.velocity.x + hControlDir.x;
            movement.z = characterController.velocity.z + hControlDir.z;

            if (movement.magnitude > RunSpeed) {
                movement.Normalize();
                movement *= RunSpeed;
            }

            movement.y = characterController.velocity.y;
        }

        // apply gravity
        movement.y -= Gravity * Time.deltaTime;

        // perform movement
        characterController.Move(movement *= Time.deltaTime);
    }
}
