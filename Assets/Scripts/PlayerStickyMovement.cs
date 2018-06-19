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
    public Transform[] GroundChecks;
    public float AlignMaxAngle;
    public float AlignMinAngle;
    public float AlignPFactor;
    public float AlignIFactor;
    public float AlignDFactor;
    public float AlignGroundFactor;
    public float AlignLerp;

    //public float AlignRateLimit;
    //public float AlignTime;
    //public float AlignBaseRate;
    //public float AlignDistanceFactor;

    private Rigidbody body;
    private bool onGround;
    private Vector3 targetNormal;
    private Vector3 lastUpDirection;
    private float alignIntegral;
    //private float lastAlignDistance;
    //private float lastAlignVelocity;

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
        alignIntegral = 0;
        //lastAlignDistance = 0;
        //lastAlignVelocity = 0;
    }

    private void AlignToGround() {
        Vector3 groundNormal = Vector3.zero;
        RaycastHit groundCheck;
        if (Physics.Raycast(transform.position, -transform.up, out groundCheck, 0.5f)) {
            groundNormal = groundCheck.normal;
        } else {
            // not on ground; don't align
            return;
        }

        // all ground checks failed; don't align
        if (groundNormal.sqrMagnitude == 0)
            return;

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


            //// compute P
            //float pTerm = AlignPFactor * angleBetween;

            //// compute I
            //alignIntegral += AlignIFactor * angleBetween * Time.deltaTime;
            //alignIntegral = Mathf.Clamp(alignIntegral, 0, AlignMaxAngle);

            //// compute D
            //float lastDelta = Vector3.Angle(transform.up, lastUpDirection);
            //float dTerm = AlignDFactor * (lastDelta / Time.deltaTime);

            //lastUpDirection = transform.up;

            //float angleToRotate = pTerm + alignIntegral - dTerm;
            //float alignLerp = angleToRotate / angleBetween;

            //Debug.Log($"angleBetween {angleBetween:F2}, angleChange {angleToRotate:F2}, p {pTerm}, i {alignIntegral}, d {dTerm}");

            //transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, AlignLerp);

            //Debug.Log($"tnc {tnc:F2}, align change {angleBetween * AlignLerp:F2}");

            if (angleBetween < AlignMinAngle)
                transform.rotation = targetRotation;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, AlignLerp);
        } else {
            alignIntegral = 0;
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

    void OnCollisionStay() {
        onGround = true;

        //RaycastHit groundCheck;
        //if (Physics.Raycast(transform.position, -transform.up, out groundCheck, 0.5f)) {
        //    targetNormal = groundCheck.normal;
        //}
    }

    float JumpSpeed() {
        return Mathf.Sqrt(2 * JumpHeight * Gravity);
    }
}
