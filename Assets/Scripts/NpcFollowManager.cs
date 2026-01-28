using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NPCSmartFollower2D : MonoBehaviour
{
    public enum NPCType
    {
        Abel,   // confident, faster
        Kuttan  // hesitant, slower
    }

    [Header("NPC Type")]
    public NPCType npcType = NPCType.Abel;

    [Header("References")]
    public Transform player;
    public Rigidbody2D playerRb;
    public Transform groundCheck;
    public Transform frontCheck;
    public LayerMask groundLayer;

    [Header("Movement")]
    public float baseMoveSpeed = 2.2f;
    public float sprintMultiplier = 1.7f; // Speed increase when catching up
    public float catchUpDistance = 4.0f; // Distance where they start sprinting
    public float minStopDistance = 0.8f;
    public float maxStopDistance = 1.6f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public float wallCheckDistance = 0.5f;
    public float stuckTimeBeforeJump = 0.25f;

    [Header("Ground Check")]
    public float groundRadius = 0.18f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer spriteRenderer;
    bool forcedHold;
    Transform holdPoint;
    Vector3 originalScale;

    bool isGrounded;
    float stuckTimer;
    float lastX;
    float currentStopDistance;
    float currentDynamicSpeed;

    // Thinking logic
    float thinkTimer;
    float thinkCooldown = 0.6f;
    int thinkDecision; // 0 wait, 1 move closer, 2 reposition

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerRb = p.GetComponent<Rigidbody2D>();
            }
        }

        ApplyPersonality();
        PickNewStopDistance();
        lastX = transform.position.x;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        CheckGround();
        Think();
        FollowPlayer();
        DetectStuck();
        HandleAnimations();
    }

    public void MoveToHoldPoint(Transform point)
    {
        holdPoint = point;
        forcedHold = true;
        // Stop thinking when being held
        thinkCooldown = 999f;
    }

    public void ReleaseFromHoldPoint()
    {
        forcedHold = false;
        holdPoint = null;
        // Restore thinking
        thinkCooldown = 0.6f;
    }

    // ---------------- PERSONALITY ----------------
    void ApplyPersonality()
    {
        if (npcType == NPCType.Abel)
        {
            baseMoveSpeed = 2.4f;
            jumpForce = 6.5f;
        }
        else
        {
            baseMoveSpeed = 2.0f;
            jumpForce = 5.5f;
        }
    }

    // ---------------- THINKING ----------------
    void Think()
    {
        if (forcedHold) return;

        thinkTimer += Time.fixedDeltaTime;
        if (thinkTimer < thinkCooldown) return;

        thinkTimer = 0f;

        float r = Random.value;
        if (r < 0.6f) thinkDecision = 0;      // wait
        else if (r < 0.85f) thinkDecision = 1; // move closer
        else thinkDecision = 2;               // reposition

        PickNewStopDistance();
    }

    // ---------------- FOLLOW LOGIC ----------------
    void FollowPlayer()
    {
        if (forcedHold && holdPoint != null)
        {
            float dx = holdPoint.position.x - transform.position.x;

            if (Mathf.Abs(dx) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }

            float dir = Mathf.Sign(dx);
            rb.linearVelocity = new Vector2(dir * baseMoveSpeed * 0.6f, rb.linearVelocity.y);
            HandleFlip(dir);
            return;
        }

        float distanceX = player.position.x - transform.position.x;
        float absDistance = Mathf.Abs(distanceX);
        float direction = Mathf.Sign(distanceX);

        bool playerStopped = playerRb != null && Mathf.Abs(playerRb.linearVelocity.x) < 0.05f;

        // --- DYNAMIC SPEED ADJUSTMENT ---
        if (absDistance > catchUpDistance)
        {
            float speedFactor = Mathf.Clamp(absDistance / catchUpDistance, 1f, sprintMultiplier);
            currentDynamicSpeed = baseMoveSpeed * speedFactor;
        }
        else
        {
            currentDynamicSpeed = baseMoveSpeed;
        }

        // Idle Behavior (Repositioning/Waiting)
        if (playerStopped)
        {
            if (thinkDecision == 0)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
            else if (thinkDecision == 2)
            {
                rb.linearVelocity = new Vector2(-direction * baseMoveSpeed * 0.4f, rb.linearVelocity.y);
                HandleFlip(-direction);
                return;
            }
        }

        // Stop within chosen distance
        if (absDistance <= currentStopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Movement
        rb.linearVelocity = new Vector2(direction * currentDynamicSpeed, rb.linearVelocity.y);
        HandleFlip(direction);

        // Smart Jump Integration
        TrySmartJump(direction);
    }

    // ---------------- SMART JUMP (Enemy Logic) ----------------
    void TrySmartJump(float direction)
    {
        if (!isGrounded) return;

        // Wall check
        RaycastHit2D wallHit = Physics2D.Raycast(
            frontCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

        // Jump if wall is hit OR if stuck in place for too long
        if (wallHit.collider != null || stuckTimer >= stuckTimeBeforeJump)
        {
            Jump();
            stuckTimer = 0f;
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // ---------------- UTILS ----------------
    void PickNewStopDistance()
    {
        currentStopDistance = Random.Range(minStopDistance, maxStopDistance);
    }

    void DetectStuck()
    {
        float currentX = transform.position.x;
        // If speed is set but X isn't changing significantly
        if (Mathf.Abs(currentX - lastX) < 0.001f && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = 0f;

        lastX = currentX;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
    }

    void HandleFlip(float direction)
    {
        if (spriteRenderer == null || direction == 0) return;

        // Flip sprite by changing scale instead of using flipX
        // This prevents issues with other sprite renderer components
        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(scale.x) * (direction > 0 ? 1 : -1);
        transform.localScale = scale;
    }
    public void TeleportToHoldPoint(Transform point)
    {
        if (point == null) return;

        rb.position = new Vector2(point.position.x, rb.position.y);
        rb.linearVelocity = Vector2.zero;
    }

    void HandleAnimations()
    {
        float speed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetBool("isWalking", speed > 0.1f);
        anim.SetBool("isRunning", speed > baseMoveSpeed * 1.1f);
        anim.SetBool("isGrounded", isGrounded);
    }

    public bool IsAtHoldPoint()
    {
        if (holdPoint == null || !forcedHold) return false;
        return Vector2.Distance(transform.position, holdPoint.position) < 0.3f;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        if (frontCheck != null)
        {
            Gizmos.color = Color.red;
            float dir = (player != null && player.position.x > transform.position.x) ? 1 : -1;
            Gizmos.DrawRay(frontCheck.position, Vector2.right * dir * wallCheckDistance);
        }
    }
}