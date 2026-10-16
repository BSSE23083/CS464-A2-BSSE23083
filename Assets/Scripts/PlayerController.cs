using System.Collections;
using UnityEngine;

// Part A + B: movement, grounded check, jump from a height in metres,
// coyote time, jump buffer, fall gravity, and one piece of juice (landing squash).
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 5f;                 // m/s

    [Header("Jump")]
    [SerializeField] float jumpHeight = 1.8f;          // metres
    [SerializeField] float fallMultiplier = 2.2f;      // extra gravity while falling
    [SerializeField] float coyoteTime = 0.1f;          // s
    [SerializeField] float jumpBuffer = 0.12f;         // s

    [Header("Ground check")]
    [SerializeField] Vector3 feetOffset = new Vector3(0f, -0.9f, 0f); // local, near capsule bottom
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundMask;             // ONLY the Ground layer

    [Header("Juice: landing squash")]
    [SerializeField] Transform body;                   // the child mesh, NO collider on it
    [SerializeField] float squashDuration = 0.25f;     // s
    [SerializeField] float squashY = 0.7f;
    [SerializeField] float minAirTime = 0.1f;          // only real landings

    Rigidbody rb;
    Transform cam;

    Vector3 move;                  // from Update, camera-relative, length <= 1
    bool isGrounded, jumpQueued;
    float lastGroundedTime = -100f, lastPressedTime = -100f;

    float airTime;
    bool wasGrounded = true;
    Coroutine squashRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Zero friction so the player doesn't stick to walls in mid-air.
        var mat = new PhysicsMaterial("PlayerNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        GetComponent<CapsuleCollider>().material = mat;

        if (Camera.main != null) cam = Camera.main.transform;
        if (body == null && transform.childCount > 0) body = transform.GetChild(0);
    }

    void Update()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        // ---- 1. Read input (legacy Input, Active Input Handling = Both) ----
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f); // diagonals clamped

        // ---- 2. Camera-relative direction ----
        Vector3 forward = Vector3.forward, right = Vector3.right;
        if (cam != null)
        {
            forward = cam.forward; forward.y = 0f; forward.Normalize();
            right = cam.right;     right.y = 0f;   right.Normalize();
        }
        move = Vector3.ClampMagnitude(forward * input.z + right * input.x, 1f);

        // ---- 3. Ground check ----
        isGrounded = Physics.CheckSphere(transform.TransformPoint(feetOffset),
                                         checkRadius, groundMask,
                                         QueryTriggerInteraction.Ignore);

        // ---- 4. Coyote + buffer (timestamps) ----
        // Stamp the ground only when no jump is queued and we aren't rising.
        if (isGrounded && !jumpQueued && rb.linearVelocity.y <= 0.01f)
            lastGroundedTime = Time.time;

        if (Input.GetKeyDown(KeyCode.Space))
            lastPressedTime = Time.time;

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantJump = Time.time - lastPressedTime <= jumpBuffer;

        if (canJump && wantJump && !jumpQueued)
        {
            jumpQueued = true;
            lastGroundedTime = -100f;   // use up both windows
            lastPressedTime = -100f;
        }

        // ---- 5. Juice trigger: a real landing ----
        if (!isGrounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            if (!wasGrounded && airTime > minAirTime && body != null)
            {
                if (squashRoutine != null) StopCoroutine(squashRoutine);
                squashRoutine = StartCoroutine(Squash());
            }
            airTime = 0f;
        }
        wasGrounded = isGrounded;
        if (move.sqrMagnitude > 0.01f && body != null)
        {
        Quaternion targetRot = Quaternion.LookRotation(move);
        float turn = 1f - Mathf.Exp(-12f * Time.deltaTime);   // frame-rate independent
        body.rotation = Quaternion.Slerp(body.rotation, targetRot, turn);
        }
    }

    void FixedUpdate()
    {
        // Horizontal velocity from input, keep vertical velocity.
        rb.linearVelocity = new Vector3(move.x * speed,
                                        rb.linearVelocity.y,
                                        move.z * speed);

        if (jumpQueued)
        {
            jumpQueued = false;

            float g = -Physics.gravity.y;                       // 9.81
            float v0 = Mathf.Sqrt(2f * g * jumpHeight);         // launch speed

            // Zero vertical velocity first, so a jump taken while falling isn't short.
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;

            rb.AddForce(Vector3.up * rb.mass * v0, ForceMode.Impulse);
        }
        else if (rb.linearVelocity.y < 0f)
        {
            // Extra gravity only while falling.
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    IEnumerator Squash()
    {
        for (float elapsed = 0f; elapsed < squashDuration; elapsed += Time.deltaTime)
        {
            float p = Ease.OutBack(elapsed / squashDuration);     // 0 = squashed, 1 = rest
            float y = Mathf.LerpUnclamped(squashY, 1f, p);        // unclamped: keeps the overshoot
            float xz = 1f / Mathf.Sqrt(y);                        // keep volume
            body.localScale = new Vector3(xz, y, xz);
            yield return null;
        }
        body.localScale = Vector3.one;                            // exactly at rest
        squashRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(feetOffset), checkRadius);
    }
}
