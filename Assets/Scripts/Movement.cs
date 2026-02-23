// AdvancedMovement.cs
// Attach to your player GameObject (requires Rigidbody + CapsuleCollider).
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class AdvancedMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;                // target speed on ground
    public float sprintSpeed = 9f;              // optional sprint speed
    [Range(0f, 1f)] public float groundAcceleration = 0.2f; // how fast velocity reaches target
    [Range(0f, 1f)] public float groundFriction = 0.08f;    // slows when no input

    [Header("Air")]
    public float airAcceleration = 0.05f;       // slower responsiveness in air
    public float airControl = 0.6f;             // how much input affects direction in air (0-1)

    [Header("Jump")]
    public float jumpForce = 7f;               // initial upward velocity for jump
    public int extraJumps = 0;                 // extra jumps (double jumps: set 1)
    public float coyoteTime = 0.12f;           // grace period after leaving ground
    public float jumpBufferTime = 0.12f;       // buffer before landing
    public float holdJumpGravityMultiplier = 0.5f; // while holding jump reduce gravity (variable jump)
    public float fallGravityMultiplier = 2.2f;     // stronger gravity when falling

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.12f;  // slightly above collider bottom
    public float slopeLimit = 60f;             // maximum slope angle regarded as ground

    [Header("Misc")]
    public Transform orientation;              // optional transform to use for movement direction (usually camera)
    public bool allowSprint = true;

    // private
    Rigidbody rb;
    CapsuleCollider capsule;
    Vector3 inputDirection;
    Vector3 contactNormal = Vector3.up;
    bool isGrounded;
    float lastGroundedTime = -999f;
    float lastJumpPressedTime = -999f;
    int jumpsRemaining;
    float currentSpeedTarget;
    Vector3 currentVelocityXZ;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // Rigidbody recommended settings:
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Start()
    {
        jumpsRemaining = extraJumps;
        if (orientation == null)
            orientation = Camera.main ? Camera.main.transform : transform;
    }

    void Update()
    {
        // Read input (swap to new Input System if needed)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump");
        bool sprintHeld = allowSprint && Input.GetKey(KeyCode.LeftShift);

        // store directional input relative to orientation (e.g., camera)
        Vector3 forward = Vector3.ProjectOnPlane(orientation.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(orientation.right, Vector3.up).normalized;
        inputDirection = (forward * v + right * h);
        if (inputDirection.sqrMagnitude > 1f) inputDirection.Normalize();

        // timing for coyote & jump buffer
        if (jumpPressed) lastJumpPressedTime = Time.time;

        // target speed depends on sprint
        currentSpeedTarget = sprintHeld ? sprintSpeed : walkSpeed;

        // store jump held state for FixedUpdate gravity handling
        _jumpHeld = jumpHeld;
    }

    bool _jumpHeld; // updated in Update, used in FixedUpdate

    void FixedUpdate()
    {
        GroundCheck();

        // handle jump buffer + coyote + extra jumps
        TryHandleJumping();

        // horizontal movement: compute desired velocity on ground plane
        Vector3 desiredVel = ComputeDesiredVelocity();

        // smoothly adjust current XZ velocity towards desired
        Vector3 vel = rb.velocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);
        float accel = isGrounded ? (inputDirection.sqrMagnitude > 0.001f ? groundAcceleration : groundFriction) : airAcceleration;

        // choose how much control in air
        if (!isGrounded)
        {
            // blend between current velocity and desired with air control factor
            velXZ = Vector3.Lerp(velXZ, desiredVel, accel * (airControl));
        }
        else
        {
            velXZ = Vector3.Lerp(velXZ, desiredVel, accel * 50f); // accelerate fast on ground
        }

        rb.velocity = new Vector3(velXZ.x, rb.velocity.y, velXZ.z);

        // gravity modifications for better jump feel
        ApplyCustomGravity();
    }

    Vector3 ComputeDesiredVelocity()
    {
        // Project movement onto contact plane so we can walk slopes naturally
        Vector3 planarForward = Vector3.Cross(transform.right, contactNormal).normalized;
        Vector3 planarRight = Vector3.Cross(contactNormal, transform.forward).normalized;
        // But simpler: project input direction onto plane
        Vector3 desired = Vector3.ProjectOnPlane(inputDirection, contactNormal).normalized * currentSpeedTarget;
        // If zero input, desired is 0
        if (inputDirection.sqrMagnitude < 0.001f) desired = Vector3.zero;
        return desired;
    }

    void ApplyCustomGravity()
    {
        // Use stronger gravity when falling for snappy feel, and reduced gravity while jump is held for variable jump heights.
        Vector3 gravity = Physics.gravity; // Default gravity (-9.81 on Y)
        float multiplier = 1f;

        if (rb.velocity.y < -0.01f)
        {
            multiplier = fallGravityMultiplier;
        }
        else if (rb.velocity.y > 0.01f && _jumpHeld)
        {
            multiplier = holdJumpGravityMultiplier;
        }

        // Apply extra gravity force to emulate modified gravity without changing global gravity.
        Vector3 extraGravity = (multiplier - 1f) * gravity;
        rb.AddForce(extraGravity, ForceMode.Acceleration);
    }

    void TryHandleJumping()
    {
        // if grounded update grounded timer
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            jumpsRemaining = extraJumps; // reset extra jumps when you hit ground
        }

        // Can we consume a jump?
        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (bufferedJump)
        {
            if (isGrounded && canCoyote)
            {
                DoJump();
            }
            else if (!isGrounded && jumpsRemaining > 0)
            {
                // Use extra jump in air
                DoJump();
                jumpsRemaining--;
                lastJumpPressedTime = -999f; // consume buffer
            }
        }

        // Small optimization: if touching ceiling, cut upward velocity
        if (rb.velocity.y > 0f && IsHeadBlocked())
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }
    }

    void DoJump()
    {
        Vector3 v = rb.velocity;
        // Remove any downward velocity before applying jump for consistency
        if (v.y < 0f) v.y = 0f;

        v.y = jumpForce;
        rb.velocity = v;

        // Clear jump buffer
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;
    }

    void GroundCheck()
    {
        // Cast a sphere from slightly above the bottom of the capsule downward to detect ground and surface normal
        Vector3 origin = transform.position + Vector3.up * (capsule.height * 0.5f - capsule.radius);
        float checkRadius = capsule.radius * 0.9f;
        float checkDistance = capsule.height * 0.5f + groundCheckDistance;

        RaycastHit hit;
        if (Physics.SphereCast(origin, checkRadius, Vector3.down, out hit, checkDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle <= slopeLimit)
            {
                isGrounded = true;
                contactNormal = hit.normal;
                return;
            }
        }

        isGrounded = false;
        contactNormal = Vector3.up;
    }

    bool IsHeadBlocked()
    {
        // quick check for ceiling collisions above the top of the capsule
        Vector3 top = transform.position + Vector3.up * (capsule.height * 0.5f - capsule.radius);
        return Physics.CheckSphere(top, capsule.radius * 0.9f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    // Optional: visualize ground check for debugging
    void OnDrawGizmosSelected()
    {
        if (capsule == null) capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * (capsule.height * 0.5f - capsule.radius);
        Gizmos.DrawWireSphere(origin + Vector3.down * (capsule.height * 0.5f + groundCheckDistance), capsule.radius * 0.9f);
    }
}
