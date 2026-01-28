using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI2D_Smart : MonoBehaviour
{
    /* ================= REFERENCES ================= */
    [Header("References")]
    public Transform player;
    public Transform groundCheck;
    public Transform frontCheck;
    public LayerMask groundLayer;

    /* ================= MOVEMENT ================= */
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float stopDistance = 0.8f;

    /* ================= JUMP ================= */
    [Header("Jump")]
    public float jumpForce = 6f;
    public float wallCheckDistance = 0.4f;
    public float stuckTimeBeforeJump = 0.2f;

    /* ================= GROUND CHECK ================= */
    [Header("Ground Check")]
    public float groundRadius = 0.15f;

    /* ================= GAME OVER ================= */
    [Header("Game Over")]
    public string gameOverLayerName = "GameOver";

    Rigidbody2D rb;
    Animator anim;
    Collider2D col;

    bool isGrounded;
    bool facingRight = true;

    float stuckTimer;
    float lastX;

    float originalSpeed;
    bool slowed;

    bool isDisabled;
    Vector3 spawnPosition;

    enum AIState { Idle, Chase }
    AIState state = AIState.Chase;

    /* ================= INIT ================= */

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        originalSpeed = moveSpeed;
        spawnPosition = transform.position;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        lastX = transform.position.x;
    }

    /* ================= SPEED CONTROL ================= */

    public void SetSlow(bool state, float multiplier)
    {
        slowed = state;
        moveSpeed = slowed ? originalSpeed * multiplier : originalSpeed;
    }

    /* ================= UPDATE ================= */

    void FixedUpdate()
    {
        if (isDisabled) return;

        if (player == null)
        {
            SetIdle();
            return;
        }

        CheckGround();

        switch (state)
        {
            case AIState.Chase:
                ChasePlayer();
                break;
        }

        HandleAnimations();
        DetectStuck();
    }

    /* ================= AI BEHAVIOUR ================= */

    void ChasePlayer()
    {
        float distanceX = player.position.x - transform.position.x;

        if (Mathf.Abs(distanceX) <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float direction = Mathf.Sign(distanceX);

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        Flip(direction);
        TrySmartJump(direction);
    }

    void SetIdle()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        anim.SetBool("isWalking", false);
    }

    /* ================= SMART JUMP ================= */

    void TrySmartJump(float direction)
    {
        if (!isGrounded) return;

        RaycastHit2D wallHit = Physics2D.Raycast(
            frontCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

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

    /* ================= STUCK ================= */

    void DetectStuck()
    {
        float currentX = transform.position.x;

        if (Mathf.Abs(currentX - lastX) < 0.001f && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = 0f;

        lastX = currentX;
    }

    /* ================= GROUND ================= */

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    /* ================= ANIMATION ================= */

    void HandleAnimations()
    {
        anim.SetBool("isWalking", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        anim.SetBool("isGrounded", isGrounded);
    }

    /* ================= FLIP ================= */

    void Flip(float direction)
    {
        if (direction > 0 && !facingRight)
            FlipSprite();
        else if (direction < 0 && facingRight)
            FlipSprite();
    }

    void FlipSprite()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    /* ================= GAME OVER (LAKE) ================= */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(gameOverLayerName))
        {
            DisableEnemy();
        }
    }

    void DisableEnemy()
    {
        if (isDisabled) return;
        isDisabled = true;

        // Stop AI & physics
        state = AIState.Idle;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;

        // Animation
        anim.SetBool("isWalking", false);
        anim.SetTrigger("Fall"); // optional animation

        // Disable collisions
        col.enabled = false;
    }

    /* ================= REUSE ================= */

    public void RespawnEnemy()
    {
        isDisabled = false;

        transform.position = spawnPosition;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;

        state = AIState.Chase;
        col.enabled = true;

        gameObject.SetActive(true);
    }

    /* ================= DEBUG ================= */

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
            Gizmos.DrawRay(frontCheck.position, Vector2.right * wallCheckDistance);
            Gizmos.DrawRay(frontCheck.position, Vector2.left * wallCheckDistance);
        }
    }
}
