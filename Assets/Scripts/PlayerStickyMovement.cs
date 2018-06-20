using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStickyMovement : MonoBehaviour {

    public bool AutoRun;
    public float WalkSpeed;
    public float RunSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float JumpHeight;
    public float Gravity;
    public Transform GroundSensor;
    public float AlignMaxAngle;
    public float AlignMinAngle;
    public float AlignGroundFactor;
    public float AlignLerp;

    private Rigidbody body;
    private bool onGround;
    private Vector3 targetNormal;

    private void Awake () {
        body = GetComponent<Rigidbody>();
        body.freezeRotation = true;
        body.useGravity = false;
        targetNormal = transform.up;
    }

    private void FixedUpdate () {
        AlignToGround();
        MovePlayer();
    }

    public void Reset() {
        transform.rotation = new Quaternion();
        targetNormal = transform.up;
        onGround = false;
        body.velocity = Vector3.zero;
    }

    private void AlignToGround() {
        Vector3 groundNormal = Vector3.zero;
        RaycastHit groundCheck;
        if (Physics.Raycast(GroundSensor.position, -GroundSensor.up, out groundCheck, 0.3f)) {
            groundNormal = groundCheck.normal;
            onGround = true;
        } else {
            // not on ground; don't align
            onGround = false;
            return;
        }

        Vector3 lastTN = targetNormal;

        if (Vector3.Angle(targetNormal, groundNormal) < AlignMinAngle) {
            targetNormal = groundNormal;
        } else {
            targetNormal = targetNormal * (1 - AlignGroundFactor) + groundNormal * AlignGroundFactor;
        }

        float tnc = Vector3.Angle(targetNormal, lastTN);

        float angleBetween = Vector3.Angle(transform.up, targetNormal);
        if (angleBetween > 0 && angleBetween <= AlignMaxAngle) {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetNormal) * transform.rotation;

            if (angleBetween < AlignMinAngle)
                transform.rotation = targetRotation;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, AlignLerp);
        }
    }

    private void MovePlayer() {
        // get normalized horizontal input direction in local space
        float inputLat = Input.GetAxis("Horizontal");
        float inputLon = Input.GetAxis("Vertical");
        Vector3 hControlDir = new Vector3(inputLat, 0, inputLon).normalized;

        // determine target horizontal velocity
        bool running = !onGround || (Input.GetKey(KeyCode.LeftShift) ^ AutoRun);
        hControlDir *= running ? RunSpeed : WalkSpeed;

        Vector3 targetVelocity = new Vector3(hControlDir.x, 0, hControlDir.z);
        targetVelocity = transform.TransformDirection(targetVelocity);

        // include existing component vertical velocity in target velocity
        Vector3 velocity = body.velocity;
        float verticalComponent = Vector3.Dot(velocity, transform.up);
        targetVelocity += transform.up * verticalComponent;

        // calculate change needed to reach target horizontal velocity
        Vector3 velocityChange = (targetVelocity - velocity);

        // limit acceleration
        float acceleration = onGround ? GroundAcceleration : AirAcceleration;
        acceleration *= Time.deltaTime;
        if (velocityChange.magnitude > acceleration) {
            velocityChange.Normalize();
            velocityChange *= acceleration;
        }

        // apply horizontal movement
        body.AddForce(velocityChange, ForceMode.VelocityChange);

        // handle jumps
        if (onGround && Input.GetKeyDown(KeyCode.Space)) {
            Vector3 jumpVelocity = transform.TransformDirection(Vector3.up * JumpSpeed());
            body.AddForce(jumpVelocity, ForceMode.VelocityChange);
        }

        // apply gravity
        Vector3 gravityForce = transform.TransformDirection(Vector3.down * Gravity * body.mass);
        body.AddForce(gravityForce);

        onGround = false;
    }

    float JumpSpeed() {
        return Mathf.Sqrt(2 * JumpHeight * Gravity);
    }
}
