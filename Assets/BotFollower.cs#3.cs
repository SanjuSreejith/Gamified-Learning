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

    float currentVelocityX;
    float desiredStopOffset;

    Animator animator;
    SpriteRenderer spriteRenderer;
    ParticleSystem.EmissionModule emission;

    Vector2 lastPlayerPos;
    float playerMoveDir; // -1 = left, +1 = right, 0 = idle
    bool allowMovement = true;

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
        DecideIfBotCanMove();
        UpdateDesiredStopDistance();

        if (allowMovement)
            MoveBot();
        else
            currentVelocityX = 0f; // FULL STOP

        HandleAnimation();
        HandleSpriteFlip();
        HandleGrassParticles();
    }

    // ---------------------------------------------------------
    void DetectPlayerMovement()
    {
        float delta = player.position.x - lastPlayerPos.x;

        if (Mathf.Abs(delta) > 0.02f)
            playerMoveDir = Mathf.Sign(delta);
        else
            playerMoveDir = 0f;

        lastPlayerPos = player.position;
    }

    // ---------------------------------------------------------
    void DecideIfBotCanMove()
    {
        float botToPlayerDir =
            Mathf.Sign(player.position.x - transform.position.x);

        // If player walks toward bot → STOP
        if (playerMoveDir != 0 && playerMoveDir != botToPlayerDir)
            allowMovement = false;
        else
            allowMovement = true;
    }

    // ---------------------------------------------------------
    void UpdateDesiredStopDistance()
    {
        float distance = Mathf.Abs(transform.position.x - player.position.x);

        if (distance > recheckDistance)
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
        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(currentVelocityX));
    }

    // ---------------------------------------------------------
    void HandleSpriteFlip()
    {
        if (currentVelocityX != 0)
            spriteRenderer.flipX = currentVelocityX < 0;
    }

    // ---------------------------------------------------------
    void HandleGrassParticles()
    {
        if (grassParticles == null) return;

        float speed = Mathf.Abs(currentVelocityX);

        if (footPoint != null)
            grassParticles.transform.position = footPoint.position;

        if (speed > minMoveSpeedForDust)
        {
            emission.rateOverTime = Mathf.Lerp(5f, 25f, speed / maxSpeed);

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
