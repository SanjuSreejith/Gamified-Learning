using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float acceleration = 12f;
    public float deceleration = 16f;
    [Header("Camera Effects")]
    public FollowingCamera followCamera;

    [Header("Jump")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Audio")]
    public AudioClip walkClip;
    public AudioClip jumpClip;
    public AudioSource footstepSource;
    public AudioSource actionSource;

    [Header("Jetpack")]
    public JetpackController2D jetpack;

    [Header("Jump Block Check")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.4f;
    public float playerHeight = 1.6f;
    [Header("Fast Fall")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Double Jump")]
    public int maxJumps = 2;

    [Header("Animation Triggers")]
    [SerializeField] string landTrigger = "Land";
    [SerializeField] string jumpTrigger = "Jump";
    [SerializeField] string doubleJumpTrigger = "DoubleJump";

    float moveIntentTimer;
    const float MoveIntentGraceTime = 0.12f; // 80 ms (1–2 frames)
    float lastFallSpeed;
    // Components
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    // State
    float moveInput;
    float coyoteCounter;
    float jumpBufferCounter;

    bool isGrounded;
    bool wasGrounded;
    bool isJumping;
    bool facingRight = true;
    int jumpCount;

    float groundIgnoreTimer;
    const float GroundIgnoreTime = 0.08f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    bool JetpackActive()
    {
        return jetpack != null && jetpack.IsFlying();
    }

    bool IsBlockedByTallGround()
    {
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);

        if (!hit) return false;

        float heightDiff = hit.collider.bounds.max.y - transform.position.y;
        return heightDiff > playerHeight * 0.8f;
    }

    void Update()
    {
        // Jetpack override
        if (JetpackActive())
        {
            footstepSource?.Stop();
            return;
        }

        // ───── INPUT ─────
        moveInput = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");

        // ───── MOVEMENT INTENT BUFFER (CRITICAL) ─────
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            moveIntentTimer = MoveIntentGraceTime;
        }
        else
        {
            moveIntentTimer -= Time.deltaTime;
        }

        // ───── FACING ─────
        if (moveInput > 0.01f)
        {
            facingRight = true;
            sr.flipX = false;
        }
        else if (moveInput < -0.01f)
        {
            facingRight = false;
            sr.flipX = true;
        }

        // ───── GROUND CHECK ─────
        wasGrounded = isGrounded;

        bool rawGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );

        if (groundIgnoreTimer > 0f)
        {
            groundIgnoreTimer -= Time.deltaTime;
            isGrounded = false;
        }
        else
        {
            isGrounded = rawGrounded;
        }

        // ───── TIMERS ─────
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
        jumpBufferCounter = jumpPressed ? jumpBufferTime : jumpBufferCounter - Time.deltaTime;

        // ───── JUMP LOGIC (INTENT DRIVEN) ─────
        if (jumpBufferCounter > 0f)
        {
            bool canNormalJump = jumpCount == 0 && coyoteCounter > 0f;
            bool canDoubleJump = jumpCount == 1;

            if (canNormalJump && IsBlockedByTallGround())
                canNormalJump = false;

            if (canNormalJump || canDoubleJump)
            {
                // ✅ FIXED: velocity (NOT linearVelocity)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                // 🎥 Camera shake on jump
                if (followCamera)
                {
                    // First jump = subtle, double jump = stronger
                    followCamera.Shake(jumpCount == 0 ? 0.08f : 0.12f);
                }

                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
                groundIgnoreTimer = GroundIgnoreTime;

                jumpCount++;
                isJumping = true;

                if (jumpCount == 1)
                {
                    anim.SetTrigger(jumpTrigger);
                    anim.SetBool("IsDoubleJumping", false);
                }
                else
                {
                    anim.SetTrigger(doubleJumpTrigger);
                    anim.SetBool("IsDoubleJumping", true);
                }

                PlayJumpSound();
            }
        }

        // ───── END JUMP STATE AT APEX ─────
        if (isJumping && rb.linearVelocity.y <= 0f)
        {
            isJumping = false;
        }
        // Track fall speed BEFORE touching ground
        if (!isGrounded && rb.linearVelocity.y < 0f)
        {
            lastFallSpeed = rb.linearVelocity.y;
        }
        // ───── LANDING ─────
        if (isGrounded && !wasGrounded)
        {
            anim.SetTrigger(landTrigger);
            anim.SetBool("IsDoubleJumping", false);

            // 💥 Impact-based camera shake (stable)
            if (followCamera)
            {
                float impactSpeed = Mathf.Abs(lastFallSpeed);
                float shake = Mathf.InverseLerp(3f, 12f, impactSpeed) * 0.35f;
                followCamera.Shake(shake);
            }

            jumpCount = 0;
            isJumping = false;
        }

        UpdateAnimator();
        HandleFootstepSound();
    }

    void FixedUpdate()
    {
        if (JetpackActive()) return;

        // ================= HORIZONTAL MOVEMENT =================
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f
            ? acceleration
            : deceleration;

        rb.AddForce(Vector2.right * speedDiff * accelRate, ForceMode2D.Force);

        // ================= FAST FALL & SHORT HOP =================
        if (!isGrounded)
        {
            // Fast fall when going down
            if (rb.linearVelocity.y < 0f)
            {
                rb.AddForce(
                    Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f),
                    ForceMode2D.Force
                );
            }
            // Short hop when jump released early
            else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
            {
                rb.AddForce(
                    Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f),
                    ForceMode2D.Force
                );
            }
        }

        // ================= CAMERA POLISH =================
        if (followCamera && isGrounded)
        {
            float speed = Mathf.Abs(rb.linearVelocity.x);
            if (speed > moveSpeed * 0.9f)
            {
                followCamera.Shake(0.015f);
            }
        }
    }
    void UpdateAnimator()
    {
        bool falling =
            !isGrounded &&
            rb.linearVelocity.y < -0.1f &&
            !isJumping &&
            !anim.GetBool("IsDoubleJumping");

        // ✅ PURE INPUT-DRIVEN MOVEMENT
        bool isMovingInput = Mathf.Abs(moveInput) > 0.01f;

        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsMoving", isMovingInput);
        anim.SetBool("IsJumping", isJumping);
        anim.SetBool("IsFalling", falling);
    }
    void HandleFootstepSound()
    {
        if (!footstepSource || !walkClip) return;

        bool shouldPlay =
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.x) > 0.1f;

        if (shouldPlay && !footstepSource.isPlaying)
        {
            footstepSource.clip = walkClip;
            footstepSource.Play();
        }
        else if (!shouldPlay && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    void PlayJumpSound()
    {
        if (actionSource && jumpClip)
            actionSource.PlayOneShot(jumpClip);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        if (wallCheck != null)
        {
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position + (Vector3)(dir * wallCheckDistance)
            );
            Gizmos.DrawWireSphere(
                wallCheck.position + (Vector3)(dir * wallCheckDistance),
                0.05f
            );
        }
    }
}