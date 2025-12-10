using UnityEngine;

public class BotFollowerHorizontal : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;

    [Header("Movement Settings")]
    public float smoothTime = 0.3f;
    public float maxSpeed = 10f;

    [Header("Distance Control")]
    public float minStopDistance = 1.5f;  // Bot will stop anywhere between
    public float maxStopDistance = 3.5f;  // these distances
    public float recheckDistance = 4.5f;  // If player goes farther than this → bot follows again

    private float currentVelocityX;
    private float desiredStopOffset;      // Where bot WANTS to stand
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 lastPlayerPos;
    private bool playerIsMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        PickNewStopOffset();
        lastPlayerPos = player.position;
    }

    void Update()
    {
        if (player == null) return;

        DetectPlayerMovement();
        UpdateDesiredStopDistance();
        MoveBot();
        HandleAnimation();
        HandleSpriteFlip();
    }

    // ---------------------------------------------------------
    //  PLAYER MOVEMENT CHECK
    // ---------------------------------------------------------
    void DetectPlayerMovement()
    {
        float movement = Mathf.Abs(player.position.x - lastPlayerPos.x);

        playerIsMoving = movement > 0.05f;

        lastPlayerPos = player.position;
    }

    // ---------------------------------------------------------
    // LOGIC FOR WHEN BOT SHOULD FOLLOW
    // ---------------------------------------------------------
    void UpdateDesiredStopDistance()
    {
        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);

        // If player is far → bot must follow
        if (distanceToPlayer > recheckDistance)
        {
            PickNewStopOffset();
        }

        // If player moves → we update offset but not too often
        if (playerIsMoving && distanceToPlayer > maxStopDistance)
        {
            PickNewStopOffset();
        }
    }

    void PickNewStopOffset()
    {
        // Decide left or right based on current position
        float direction = transform.position.x < player.position.x ? -1f : 1f;

        // Random comfortable stop range
        float randomDist = Random.Range(minStopDistance, maxStopDistance);

        desiredStopOffset = randomDist * direction;
    }

    // ---------------------------------------------------------
    // MOVEMENT
    // ---------------------------------------------------------
    void MoveBot()
    {
        float targetX = player.position.x + desiredStopOffset;
        float targetY = transform.position.y;

        float newX = Mathf.SmoothDamp(
            transform.position.x,
            targetX,
            ref currentVelocityX,
            smoothTime,
            maxSpeed
        );

        transform.position = new Vector2(newX, targetY);
    }

    // ---------------------------------------------------------
    // ANIMATION SPEED
    // ---------------------------------------------------------
    void HandleAnimation()
    {
        float speed = Mathf.Abs(currentVelocityX);

        if (animator != null)
            animator.SetFloat("Speed", speed);
    }

    // ---------------------------------------------------------
    // FACE PLAYER
    // ---------------------------------------------------------
    void HandleSpriteFlip()
    {
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }
}
