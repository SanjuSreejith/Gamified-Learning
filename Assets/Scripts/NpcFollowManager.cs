using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NPCSmartFollower2D : MonoBehaviour
{
    public enum NPCType { Abel, Kuttan }

    [Header("NPC Type")]
    public NPCType npcType = NPCType.Abel;

    [Header("References")]
    public Transform player;
    public Rigidbody2D playerRb;
    public Transform groundCheck;
    public Transform frontCheck;
    public LayerMask groundLayer;
    public LayerMask npcLayer;

    [Header("Movement")]
    public float baseMoveSpeed = 2.2f;
    public float sprintMultiplier = 1.7f;
    public float catchUpDistance = 4f;
    public float minStopDistance = 0.8f;
    public float maxStopDistance = 1.6f;
    public float acceleration = 20f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public int maxJumps = 2;
    public float wallCheckDistance = 0.45f;
    public float stuckTimeBeforeJump = 0.25f;

    [Header("Edge Detection")]
    public float edgeCheckDistance = 0.6f;
    public float edgeForwardOffset = 0.35f;
    [Header("Drop Down Logic")]
    public float maxSafeDropHeight = 3.5f;
    public float playerBelowTolerance = 0.3f;

    [Header("Group Behavior")]
    public float separationRadius = 0.8f;
    public float separationStrength = 1.2f;

    [Header("Path Memory")]
    public int maxMemoryPoints = 24;
    public float memorySpacing = 0.5f;

    [Header("Ground Check")]
    public float groundRadius = 0.18f;
    // Hold system
    bool isJumping;
    bool forcedHold;
    Transform holdPoint;
    // Path memory optimization
    float memoryTimer;
    const float MEMORY_SAMPLE_INTERVAL = 0.35f;   // seconds
    float lastSavedX;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer spriteRenderer;

    bool isGrounded;
    int jumpCount;

    float currentStopDistance;
    float targetSpeed;
    float stuckTimer;
    float lastX;

    // Distance cache
    float cachedAbsDistance;
    float cachedDirection;
    float distanceCheckTimer;
    const float DIST_CHECK_INTERVAL = 0.1f;

    // Player prediction
    float predictedPlayerSpeed;
    float playerSpeedSmooth;

    // Path memory
    Queue<float> safeXMemory = new Queue<float>();

    Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    void Start()
    {
        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p)
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
        if (!player) return;

        CheckGround();
        UpdateDistanceCache();
        PredictPlayerMovement();
        RememberSafePath();
        FollowPlayer();
        DetectStuck();
        HandleAnimations();
    }

    // ---------------- DISTANCE CACHE ----------------
    void UpdateDistanceCache()
    {
        distanceCheckTimer += Time.fixedDeltaTime;
        if (distanceCheckTimer < DIST_CHECK_INTERVAL) return;

        distanceCheckTimer = 0f;
        float dx = player.position.x - transform.position.x;
        cachedAbsDistance = Mathf.Abs(dx);
        cachedDirection = Mathf.Sign(dx);
    }

    // ---------------- PLAYER PREDICTION ----------------
    void PredictPlayerMovement()
    {
        if (!playerRb) return;

        playerSpeedSmooth = Mathf.Lerp(
            playerSpeedSmooth,
            Mathf.Abs(playerRb.linearVelocity.x),
            8f * Time.fixedDeltaTime
        );

        predictedPlayerSpeed = Mathf.Max(
            0,
            playerSpeedSmooth - 0.25f * 6f
        );
    }

    // ---------------- FOLLOW ----------------
    void FollowPlayer()
    {   // Forced hold logic (used by dialogues / cutscenes)
        if (forcedHold && holdPoint != null)
        {
            float dx = holdPoint.position.x - transform.position.x;

            if (Mathf.Abs(dx) < 0.1f)
            {
                SmoothMove(0);
                return;
            }

            SmoothMove(Mathf.Sign(dx) * baseMoveSpeed * 0.6f);
            HandleFlip(Mathf.Sign(dx));
            return;
        }
        if (cachedAbsDistance <= currentStopDistance)
        {
            SmoothMove(0);
            return;
        }

        float speed = baseMoveSpeed;

        if (cachedAbsDistance > catchUpDistance)
            speed *= sprintMultiplier;

        if (predictedPlayerSpeed < 0.3f && cachedAbsDistance < catchUpDistance)
            speed *= 0.55f;
        if (IsEdgeAhead(cachedDirection) && isGrounded)
        {
            // ✅ Allow drop-down if player is below and ahead
            if (ShouldDropDown(cachedDirection))
            {
                // Step off the edge smoothly
                SmoothMove(cachedDirection * baseMoveSpeed * 0.8f);
                return;
            }

            // ❌ Otherwise, respect safety memory
            if (!IsPathKnownSafe(cachedDirection))
            {
                SmoothMove(0);
                return;
            }
        }

        float separation = GetSeparationOffset();
        targetSpeed = cachedDirection * speed + separation;

        SmoothMove(targetSpeed);
        HandleFlip(cachedDirection);

        TrySmartJump(cachedDirection);
    }

    // ---------------- SMOOTH MOVE ----------------
    void SmoothMove(float targetX)
    {
        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetX,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    // ---------------- SMART JUMP ----------------
    void TrySmartJump(float direction)
    {
        if (jumpCount >= maxJumps) return;

        bool wallHit = false;

        if (isGrounded)
        {
            wallHit = Physics2D.Raycast(
                frontCheck.position,
                Vector2.right * direction,
                wallCheckDistance,
                groundLayer
            );
        }

        if (wallHit || stuckTimer >= stuckTimeBeforeJump)
        {
            Jump();
            stuckTimer = 0f;
        }
    }
    bool ShouldDropDown(float direction)
    {
        if (!player) return false;

        // Player must be in front
        float dx = player.position.x - transform.position.x;
        if (Mathf.Sign(dx) != direction)
            return false;

        // Player must be below
        if (player.position.y >= transform.position.y - playerBelowTolerance)
            return false;

        // Check drop height (raycast down)
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            maxSafeDropHeight,
            groundLayer
        );

        // If ground exists within safe drop distance → OK to drop
        return hit.collider != null;
    }
    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        jumpCount++;
        isJumping = true; // start jump animation
    }
    // ---------------- EDGE ----------------
    bool IsEdgeAhead(float direction)
    {
        Vector2 origin = (Vector2)transform.position +
                         Vector2.right * direction * edgeForwardOffset;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            edgeCheckDistance,
            groundLayer
        );

        return hit.collider == null;
    }

    // ---------------- PATH MEMORY ----------------
    void RememberSafePath()
    {
        if (!isGrounded) return;

        // Only remember while actually moving
        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            return;

        memoryTimer += Time.fixedDeltaTime;
        if (memoryTimer < MEMORY_SAMPLE_INTERVAL)
            return;

        memoryTimer = 0f;

        float x = transform.position.x;

        // Distance-based filter (cheap)
        if (Mathf.Abs(x - lastSavedX) < memorySpacing)
            return;

        safeXMemory.Enqueue(x);
        lastSavedX = x;

        if (safeXMemory.Count > maxMemoryPoints)
            safeXMemory.Dequeue();
    }

    bool IsPathKnownSafe(float direction)
    {
        float checkX = transform.position.x + direction * edgeForwardOffset;

        foreach (float x in safeXMemory)
        {
            if (Mathf.Abs(x - checkX) < memorySpacing)
                return true;
        }

        return false;
    }

    // ---------------- GROUP BEHAVIOR ----------------
    float GetSeparationOffset()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            separationRadius,
            npcLayer
        );

        float offset = 0f;

        foreach (var c in nearby)
        {
            if (c.transform == transform) continue;

            float dx = transform.position.x - c.transform.position.x;
            offset += Mathf.Sign(dx) * separationStrength;
        }

        return offset;
    }

    // ---------------- GROUND ----------------
    void CheckGround()
    {
        bool wasGrounded = isGrounded;

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );

        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
            isJumping = false; // stop jump animation on landing
        }
    }

    // ---------------- STUCK ----------------
    void DetectStuck()
    {
        float x = transform.position.x;

        if (Mathf.Abs(x - lastX) < 0.001f && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = 0f;

        lastX = x;
    }

    // ---------------- FLIP ----------------
    void HandleFlip(float dir)
    {
        if (!spriteRenderer || dir == 0) return;

        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(scale.x) * (dir > 0 ? 1 : -1);
        transform.localScale = scale;
    }

    // ---------------- UTILS ----------------
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

    void PickNewStopDistance()
    {
        currentStopDistance = Random.Range(minStopDistance, maxStopDistance);
    }
    void HandleAnimations()
    {
        float speed = Mathf.Abs(rb.linearVelocity.x);

        anim.SetBool("isWalking", speed > 0.1f);
        anim.SetBool("isRunning", speed > baseMoveSpeed * 1.1f);
        anim.SetBool("isGrounded", isGrounded);

        // Only Abel has jump animation
        if (npcType == NPCType.Abel)
            anim.SetBool("isJumping", isJumping);
    }
    // ---------------- HOLD / TELEPORT API ----------------

    public void MoveToHoldPoint(Transform point)
    {
        if (point == null) return;

        forcedHold = true;
        holdPoint = point;

        // Stop movement smoothly
        rb.linearVelocity = Vector2.zero;
    }

    public void TeleportToHoldPoint(Transform point)
    {
        if (point == null) return;

        forcedHold = true;
        holdPoint = point;

        rb.position = new Vector2(point.position.x, rb.position.y);
        rb.linearVelocity = Vector2.zero;
    }

    public void ReleaseFromHoldPoint()
    {
        forcedHold = false;
        holdPoint = null;
    }

    public bool IsAtHoldPoint()
    {
        if (!forcedHold || holdPoint == null) return false;

        return Mathf.Abs(transform.position.x - holdPoint.position.x) < 0.25f;
    }
    void OnDrawGizmosSelected()
    {
        // -------- Ground Check --------
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        // -------- Wall Check --------
        if (frontCheck != null)
        {
            Gizmos.color = Color.red;
            Vector3 dir = Vector3.right * (cachedDirection == 0 ? 1 : cachedDirection);
            Gizmos.DrawLine(
                frontCheck.position,
                frontCheck.position + dir * wallCheckDistance
            );
        }

        // -------- Edge Check --------
        Gizmos.color = Color.yellow;
        float dirX = cachedDirection == 0 ? 1 : cachedDirection;

        Vector2 edgeOrigin = (Vector2)transform.position +
                             Vector2.right * dirX * edgeForwardOffset;

        Gizmos.DrawLine(
            edgeOrigin,
            edgeOrigin + Vector2.down * edgeCheckDistance
        );

        // -------- Separation Radius --------
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // -------- Path Memory Points --------
        Gizmos.color = Color.cyan;
        if (safeXMemory != null)
        {
            foreach (float x in safeXMemory)
            {
                Vector3 p = new Vector3(x, transform.position.y - 0.3f, 0);
                Gizmos.DrawSphere(p, 0.06f);
            }
        }

        // -------- Stop Distance --------
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(
            transform.position + Vector3.left * currentStopDistance,
            transform.position + Vector3.right * currentStopDistance
        );

        // -------- Hold Point --------
        if (holdPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(holdPoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, holdPoint.position);
        }
    }
}