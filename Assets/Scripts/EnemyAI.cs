using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyAI2D_Smart : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform groundCheck;
    public Transform frontCheck;
    public LayerMask groundLayer;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float stopDistance = 0.8f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public float wallCheckDistance = 0.4f;
    public float stuckTimeBeforeJump = 0.2f;

    [Header("Ground Check")]
    public float groundRadius = 0.15f;

    Rigidbody2D rb;
    Animator anim;

    bool isGrounded;
    bool facingRight = true;

    float stuckTimer;
    float lastX;

    enum AIState { Idle, Chase }
    AIState state = AIState.Chase;

    // ------------------ INIT ------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        lastX = transform.position.x;
    }

    // ------------------ UPDATE ------------------

    void FixedUpdate()
    {
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

    // ------------------ AI BEHAVIOUR ------------------

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

    // ------------------ SMART JUMP LOGIC ------------------

    void TrySmartJump(float direction)
    {
        if (!isGrounded) return;

        // 1️⃣ Wall directly in front
        RaycastHit2D wallHit = Physics2D.Raycast(
            frontCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

        if (wallHit.collider != null)
        {
            Jump();
            return;
        }

        // 2️⃣ Movement blocked (stuck)
        if (stuckTimer >= stuckTimeBeforeJump)
        {
            Jump();
            stuckTimer = 0f;
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // ------------------ STUCK DETECTION ------------------

    void DetectStuck()
    {
        float currentX = transform.position.x;

        if (Mathf.Abs(currentX - lastX) < 0.001f && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        lastX = currentX;
    }

    // ------------------ GROUND CHECK ------------------

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    // ------------------ ANIMATION ------------------

    void HandleAnimations()
    {
        anim.SetBool("isWalking", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        anim.SetBool("isGrounded", isGrounded);
    }

    // ------------------ FLIP ------------------

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

    // ------------------ DEBUG ------------------

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
