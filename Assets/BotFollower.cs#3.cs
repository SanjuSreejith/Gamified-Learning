using UnityEngine;

public class BotFollowerHorizontal : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;

    [Header("Movement Settings")]
    public float smoothTime = 0.3f;
    public float maxSpeed = 10f;

    [Header("Distance Control")]
    public float minStopDistance = 1.5f;
    public float maxStopDistance = 3.5f;
    public float recheckDistance = 4.5f;

    [Header("Grass Walk Particles")]
    public ParticleSystem grassParticles;
    public Transform footPoint;
    public float minMoveSpeedForDust = 0.2f;

    private float currentVelocityX;
    private float desiredStopOffset;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 lastPlayerPos;
    private bool playerIsMoving = false;

    ParticleSystem.EmissionModule emission;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (grassParticles != null)
            emission = grassParticles.emission;

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
        HandleGrassParticles();
    }

    // ---------------------------------------------------------
    void DetectPlayerMovement()
    {
        float movement = Mathf.Abs(player.position.x - lastPlayerPos.x);
        playerIsMoving = movement > 0.05f;
        lastPlayerPos = player.position;
    }

    // ---------------------------------------------------------
    void UpdateDesiredStopDistance()
    {
        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);

        if (distanceToPlayer > recheckDistance)
            PickNewStopOffset();

        if (playerIsMoving && distanceToPlayer > maxStopDistance)
            PickNewStopOffset();
    }

    void PickNewStopOffset()
    {
        float direction = transform.position.x < player.position.x ? -1f : 1f;
        float randomDist = Random.Range(minStopDistance, maxStopDistance);
        desiredStopOffset = randomDist * direction;
    }

    // ---------------------------------------------------------
    void MoveBot()
    {
        float targetX = player.position.x + desiredStopOffset;

        float newX = Mathf.SmoothDamp(
            transform.position.x,
            targetX,
            ref currentVelocityX,
            smoothTime,
            maxSpeed
        );

        transform.position = new Vector2(newX, transform.position.y);
    }

    // ---------------------------------------------------------
    void HandleAnimation()
    {
        float speed = Mathf.Abs(currentVelocityX);
        if (animator != null)
            animator.SetFloat("Speed", speed);
    }

    // ---------------------------------------------------------
    void HandleSpriteFlip()
    {
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    // ---------------------------------------------------------
    // 🌱 REALISTIC GRASS WALK EFFECT
    // ---------------------------------------------------------
    void HandleGrassParticles()
    {
        if (grassParticles == null) return;

        float speed = Mathf.Abs(currentVelocityX);

        // Position particles at feet
        if (footPoint != null)
            grassParticles.transform.position = footPoint.position;

        if (speed > minMoveSpeedForDust)
        {
            emission.rateOverTime = Mathf.Lerp(5f, 25f, speed / maxSpeed);

            // Direction-based burst feel
            var main = grassParticles.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);

            if (!grassParticles.isPlaying)
                grassParticles.Play();
        }
        else
        {
            emission.rateOverTime = 0f;
        }
    }
}
