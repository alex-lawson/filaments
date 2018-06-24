using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStickyMovement : MonoBehaviour {

    public bool AutoRun;
    public float WalkSpeed;
    public float RunSpeed;
    public float AirSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float JumpHeight;
    public float Gravity;
    public Transform GroundSensor;
    public float AlignMaxAngle;
    public float AlignMinAngle;
    public float AlignGroundFactor;
    public float AlignLerp;
    public bool OnGround {
        get {
            return groundCheck || collisionCheck;
        }
    }

    private Rigidbody body;
    private bool groundCheck;
    private bool collisionCheck;
    private Vector3 targetNormal;

    private void Awake () {
        body = GetComponent<Rigidbody>();
        body.freezeRotation = true;
        body.useGravity = false;
        targetNormal = transform.up;
        body.Sleep();
    }

    private void FixedUpdate () {
        AlignToGround();
        MovePlayer();
    }

    public void Reset(Transform target) {
        transform.rotation = target.rotation;
        transform.position = target.position;
        targetNormal = target.up;
        groundCheck = false;
        collisionCheck = false;
        body.velocity = Vector3.zero;
        body.Sleep();
    }

    private void AlignToGround() {
        Vector3 groundNormal = Vector3.zero;
        RaycastHit groundHit;
        if (Physics.Raycast(GroundSensor.position, -GroundSensor.up, out groundHit, 0.3f)) {
            groundNormal = groundHit.normal;

            if (Vector3.Angle(targetNormal, groundNormal) < AlignMinAngle) {
                targetNormal = groundNormal;
            } else {
                targetNormal = targetNormal * (1 - AlignGroundFactor) + groundNormal * AlignGroundFactor;
            }

            groundCheck = true;
        } else {
            groundCheck = false;
        }

        if (!(groundCheck || collisionCheck))
            return;

        float angleBetween = Vector3.Angle(transform.up, targetNormal);
        if (angleBetween > 0 && angleBetween <= AlignMaxAngle) {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetNormal) * transform.rotation;

            if (angleBetween < AlignMinAngle)
                transform.rotation = targetRotation;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, AlignLerp);
        }

        collisionCheck = false;
    }

    private void MovePlayer() {
        // get normalized horizontal input direction in local space
        float inputLat = Input.GetAxis("Horizontal");
        float inputLon = Input.GetAxis("Vertical");
        Vector3 hControlDir = new Vector3(inputLat, 0, inputLon).normalized;

        // determine target horizontal velocity
        Vector3 targetVelocity = hControlDir;
        if (OnGround) {
            bool running = Input.GetKey(KeyCode.LeftShift) ^ AutoRun;
            targetVelocity *= running ? RunSpeed : WalkSpeed;
        } else {
            targetVelocity *= AirSpeed;
        }

        Vector3 velocity = body.velocity;

        // don't passively slow down in the air
        if (!OnGround) {
            Vector3 relativeCurrent = transform.InverseTransformDirection(velocity);
            Vector3 hCurrent = new Vector3(relativeCurrent.x, 0, relativeCurrent.z);
            if (targetVelocity.sqrMagnitude < hCurrent.sqrMagnitude) {
                if (targetVelocity.sqrMagnitude > 0)
                    targetVelocity = targetVelocity.normalized * hCurrent.magnitude;
                else
                    targetVelocity = hCurrent;
            }
        }

        targetVelocity = transform.TransformDirection(targetVelocity);

        // include existing component vertical velocity in target velocity
        float verticalComponent = Vector3.Dot(velocity, transform.up);
        targetVelocity += transform.up * verticalComponent;

        // calculate change needed to reach target horizontal velocity
        Vector3 velocityChange = (targetVelocity - velocity);

        // limit acceleration
        float acceleration = OnGround ? GroundAcceleration : AirAcceleration;
        acceleration *= Time.deltaTime;
        if (velocityChange.magnitude > acceleration) {
            velocityChange.Normalize();
            velocityChange *= acceleration;
        }

        // apply horizontal movement
        body.AddForce(velocityChange, ForceMode.VelocityChange);

        // handle jumps
        if (groundCheck && Input.GetKeyDown(KeyCode.Space)) {
            Vector3 jumpVelocity = transform.TransformDirection(Vector3.up * JumpSpeed());
            body.AddForce(jumpVelocity, ForceMode.VelocityChange);
        }

        // apply gravity
        Vector3 gravityForce = transform.TransformDirection(Vector3.down * Gravity * body.mass);
        body.AddForce(gravityForce);
    }

    private void OnCollisionStay(Collision collision) {
        // use this as a secondary method of aligning to ground when we're
        // e.g. hanging on ledges
        if (!groundCheck) {
            Vector3 collisionNormal = Vector3.zero;
            foreach (var cp in collision.contacts) {
                // to (imperfectly) get the normal of the surface we're colliding with,
                // take the collision normal and raycast just below it (in local space)
                // so that we get the surface *below* sharp lips
                Vector3 checkRayOrigin = cp.point + cp.normal - transform.up * 0.1f;
                Ray checkRay = new Ray(checkRayOrigin, -cp.normal);
                RaycastHit hit;
                if (cp.otherCollider.Raycast(checkRay, out hit, 2f)) {
                    Vector3 surfaceNormal = hit.normal;
                    if (Vector3.Angle(Vector3.up, surfaceNormal) < AlignMaxAngle)
                        collisionNormal += surfaceNormal;
                }
            }

            if (collisionNormal.sqrMagnitude > 0) {
                collisionNormal.Normalize();

                if (Vector3.Angle(targetNormal, collisionNormal) < AlignMinAngle) {
                    targetNormal = collisionNormal;
                } else {
                    targetNormal = targetNormal * (1 - AlignGroundFactor) + collisionNormal * AlignGroundFactor;
                }

                collisionCheck = true;
            }
        }
    }

    private float JumpSpeed() {
        return Mathf.Sqrt(2 * JumpHeight * Gravity);
    }
}
