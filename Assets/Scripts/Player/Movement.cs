// Movement.cs
// Attach to your player GameObject (requires Rigidbody + CapsuleCollider).
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Movement : MonoBehaviour
{
    // Handles walking, sprinting, jumping, air control, and ground detection.
    [Header("Movement")]
    public float walkSpeed = 6f;                // target speed on ground
    public float sprintSpeed = 9f;
    public bool attack = false;
    [Range(0f, 1f)] public float groundAcceleration = 0.2f; // how fast velocity reaches target
    [Range(0f, 1f)] public float groundFriction = 0.08f;    // slows when no input
    public float groundAccelerationRate = 28f;  // units/sec^2 while input is held
    public float groundDecelerationRate = 34f;  // units/sec^2 when input is released

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
    public Transform playerObj;                // the player model/object to determine facing direction
    public bool allowSprint = true;

    [Header("Animation")]
    public Animator animator;
    public string speedParameter = "speed";
    public string jumpParameter = "jump";
    public string airParameter = "air";

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
    int speedParameterHash;
    int jumpParameterHash;
    int airParameterHash;

    void Awake()
    {
        // Cache the physics components and configure the rigidbody for character movement.
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // Rigidbody recommended settings:
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Start()
    {
        // Initialize jump state and default references.
        jumpsRemaining = extraJumps;
        if (orientation == null)
            orientation = Camera.main ? Camera.main.transform : transform;
        
        if (playerObj == null)
            playerObj = transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        speedParameterHash = Animator.StringToHash(speedParameter);
        jumpParameterHash = Animator.StringToHash(jumpParameter);
        airParameterHash = Animator.StringToHash(airParameter);
    }

    void Update()
    {
        // Read movement and jump input every frame.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump");
        bool sprintHeld = allowSprint && Input.GetKey(KeyCode.LeftShift);

        // Get camera direction from main camera
        Transform cameraTransform = Camera.main ? Camera.main.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
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
        // Physics update for movement and jump handling.
        GroundCheck();

        // handle jump buffer + coyote + extra jumps
        TryHandleJumping();

        // horizontal movement: compute desired velocity on ground plane
        Vector3 desiredVel = ComputeDesiredVelocity();

        // smoothly adjust current XZ velocity towards desired
        Vector3 vel = rb.linearVelocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);

        // choose how much control in air
        if (!isGrounded)
        {
            float accel = airAcceleration;
            // blend between current velocity and desired with air control factor
            velXZ = Vector3.Lerp(velXZ, desiredVel, accel * (airControl));
        }
        else
        {
            bool hasInput = inputDirection.sqrMagnitude > 0.001f;
            float rate = hasInput ? groundAccelerationRate : groundDecelerationRate;

            // Preserve old tuning fields while adding predictable accel/decel in units/sec^2.
            float legacyScale = hasInput ? Mathf.Lerp(0.5f, 1.5f, groundAcceleration) : Mathf.Lerp(0.5f, 1.5f, groundFriction);
            float maxDelta = rate * legacyScale * Time.fixedDeltaTime;

            velXZ = Vector3.MoveTowards(velXZ, desiredVel, maxDelta);
        }

        rb.linearVelocity = new Vector3(velXZ.x, rb.linearVelocity.y, velXZ.z);

        UpdateAnimatorSpeed();

        // gravity modifications for better jump feel
        ApplyCustomGravity();
    }

    void UpdateAnimatorSpeed()
    {
        // Update animator parameters for movement and air state.
        if (animator == null) return;

        Vector3 planarVelocity = rb.linearVelocity;
        planarVelocity.y = 0f;
        animator.SetFloat(speedParameterHash, planarVelocity.magnitude);
        animator.SetBool(airParameterHash, !isGrounded);
    }

    Vector3 ComputeDesiredVelocity()
    {
        // Compute the desired velocity vector based on input and ground slope.
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

        if (rb.linearVelocity.y < -0.01f)
        {
            multiplier = fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0.01f && _jumpHeld)
        {
            multiplier = holdJumpGravityMultiplier;
        }

        // Apply extra gravity force to emulate modified gravity without changing global gravity.
        Vector3 extraGravity = (multiplier - 1f) * gravity;
        rb.AddForce(extraGravity, ForceMode.Acceleration);
    }

    void TryHandleJumping()
    {
        // Use jump buffering and coyote time to make jumping feel responsive.
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
        if (rb.linearVelocity.y > 0f && IsHeadBlocked())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }
    }

    void DoJump()
    {
        // Apply vertical velocity for a jump and reset jump timers.
        Vector3 v = rb.linearVelocity;
        // Remove any downward velocity before applying jump for consistency
        if (v.y < 0f) v.y = 0f;

        v.y = jumpForce;
        rb.linearVelocity = v;

        // Clear jump buffer
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;

        if (animator != null)
            animator.SetTrigger(jumpParameterHash);
    }

    void GroundCheck()
    {
        // Robust world-space feet check based on collider bounds (works with center offsets/scaling).
        int mask = GetGroundMask();
        Bounds b = capsule.bounds;
        float feetToCenter = Mathf.Max(0f, b.extents.y - capsule.radius);
        Vector3 feetSphereCenter = b.center + Vector3.down * feetToCenter;
        float checkRadius = Mathf.Max(0.01f, capsule.radius * 0.9f);

        // First: simple overlap at feet so we reliably know we're touching ground.
        bool touchingGround = Physics.CheckSphere(
            feetSphereCenter + Vector3.down * Mathf.Max(0.01f, groundCheckDistance * 0.5f),
            checkRadius,
            mask,
            QueryTriggerInteraction.Ignore);

        if (!touchingGround)
        {
            isGrounded = false;
            contactNormal = Vector3.up;
            return;
        }

        // Second: ray for normal/slope evaluation.
        float rayLength = feetToCenter + Mathf.Max(0.05f, groundCheckDistance) + 0.2f;
        Vector3 rayOrigin = b.center + Vector3.up * 0.05f;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, mask, QueryTriggerInteraction.Ignore))
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
        int mask = GetGroundMask();
        Bounds b = capsule.bounds;
        float headToCenter = Mathf.Max(0f, b.extents.y - capsule.radius);
        Vector3 top = b.center + Vector3.up * headToCenter;

        return Physics.CheckSphere(top, Mathf.Max(0.01f, capsule.radius * 0.85f), mask, QueryTriggerInteraction.Ignore);
    }

    int GetGroundMask()
    {
        // If unset in Inspector, treat as all layers except this object's layer.
        if (groundLayer.value == 0)
            return ~ (1 << gameObject.layer);

        return groundLayer.value;
    }

    // Optional: visualize ground check for debugging
    void OnDrawGizmosSelected()
    {
        // Draw the ground check sphere in the editor for debugging.
        if (capsule == null) capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return;

        Gizmos.color = Color.cyan;
        Bounds b = capsule.bounds;
        float feetToCenter = Mathf.Max(0f, b.extents.y - capsule.radius);
        Vector3 feetSphereCenter = b.center + Vector3.down * feetToCenter;
        Gizmos.DrawWireSphere(feetSphereCenter + Vector3.down * Mathf.Max(0.01f, groundCheckDistance * 0.5f), Mathf.Max(0.01f, capsule.radius * 0.9f));
    }
}
