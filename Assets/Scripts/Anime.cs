using UnityEngine;

/// <summary>
/// Simple animation controller for the player.
/// Assign an Animator and optional ground check info in the Inspector.
/// Animator should have triggers named: "Left", "Right", "LeftWalk", "RightWalk", "NewLeft", "NewRight".
/// The script chooses animations based on Rigidbody2D velocity and grounded state.
/// </summary>
public class Anime : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Idle")]
    [Tooltip("Seconds of standing still before triggering the New animation")]
    public float idleThreshold = 20f;
    [Tooltip("Minimum position change (in world units) to consider the sprite moving")]
    public float movePositionThreshold = 0.001f;

    float idleTimer = 0f;
    bool idleTriggered = false;

    bool prevGrounded = true;
    Vector3 lastPosition;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (groundCheckPoint == null)
        {
            GameObject go = new GameObject("GroundCheck_Anime");
            go.transform.parent = transform;
            go.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheckPoint = go.transform;
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        if (animator == null || rb == null)
            return;

        // Read the jump boolean from Animator (lowercase "jump")
        bool jump = animator.GetBool("jump");

        // Determine movement by change in sprite position (world space)
        Vector3 pos = transform.position;
        float deltaX = pos.x - lastPosition.x;
        bool movingByPosition = Mathf.Abs(deltaX) > movePositionThreshold;

        // Determine facing: prefer position change sign when moving, otherwise localScale.x
        bool facingRight = transform.localScale.x > 0f;
        if (movingByPosition)
            facingRight = deltaX > 0f;

        // Decide which animation state should play
        string nextState = null;

        // Idle handling (when not moving by position and grounded)
        if (!movingByPosition && IsGrounded())
        {
            idleTimer += Time.deltaTime;
            if (!idleTriggered && idleTimer >= idleThreshold)
            {
                idleTriggered = true;
                nextState = facingRight ? "New Animation 2" : "New Animation";
            }
        }
        else
        {
            idleTimer = 0f;
            idleTriggered = false;

            // Animation selection based on position change and jump boolean
            if (jump)
            {
                // Jumping takes priority
                if (movingByPosition)
                {
                    // moving while jumping: choose left/right walk by deltaX
                    nextState = (deltaX < 0f) ? "Left walk" : "right walk";
                }
                else
                {
                    // Jumping but not moving -> play walk based on facing
                    nextState = facingRight ? "right walk" : "Left walk";
                }
            }
            else
            {
                if (movingByPosition)
                {
                    // Moving on ground -> play run animations
                    nextState = (deltaX < 0f) ? "Left" : "right";
                }
            }
        }

        // Apply the state change only if needed
        if (nextState != null && lastState != nextState)
        {
            animator.Play(nextState, 0, 0f);
            lastState = nextState;
        }

        lastPosition = pos;
    }

    // Track last state to avoid restarting the same animation
    string lastState = null;

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer) != null;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
