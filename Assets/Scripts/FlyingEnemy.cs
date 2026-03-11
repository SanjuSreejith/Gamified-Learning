using UnityEngine;
using System.Collections;

public class FlyingGlitchEnemy : MonoBehaviour
{
    [Header("References")]
    public Transform armyFront;
    public Rigidbody2D rb;
    public Animator animator;

    [Header("Sky Movement")]
    public float flySpeed = 3f;
    public float heightOffset = 2f;

    [Header("Hover")]
    public float hoverAmplitude = 0.4f;
    public float hoverFrequency = 2f;

    [Header("Army Logic")]
    public float followDistance = 4f;
    public float checkInterval = 0.3f;
    public float xTolerance = 0.1f;

    private float desiredX;
    private bool hasTarget;

    void Start()
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        StartCoroutine(UpdateDesiredPositionLoop());
    }

    void Update()
    {
        animator.SetBool("isFlying", hasTarget);
    }

    void FixedUpdate()
    {
        if (!hasTarget || armyFront == null)
            return;

        FlyToDesiredPosition();
    }

    /* ================= TARGET UPDATE ================= */
    IEnumerator UpdateDesiredPositionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (armyFront == null)
            {
                hasTarget = false;
                continue;
            }

            desiredX = armyFront.position.x - followDistance;
            hasTarget = true;
        }
    }

    /* ================= FLY ================= */
    void FlyToDesiredPosition()
    {
        float hoverY =
            armyFront.position.y +
            heightOffset +
            Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

        Vector2 currentPos = rb.position;
        float deltaX = desiredX - currentPos.x;

        // If close enough, just hover
        if (Mathf.Abs(deltaX) < xTolerance)
        {
            rb.MovePosition(new Vector2(currentPos.x, hoverY));
            return;
        }

        float moveDirX = Mathf.Sign(deltaX);

        Vector2 targetPos = new Vector2(
            currentPos.x + moveDirX * flySpeed * Time.fixedDeltaTime,
            hoverY
        );

        rb.MovePosition(targetPos);

        // Face direction
        transform.localScale = new Vector3(moveDirX, 1f, 1f);
    }
}