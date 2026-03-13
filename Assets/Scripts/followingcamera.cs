using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    public static FollowingCamera Instance;

    [Header("Target")]
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1f);

    int facingDir = 1;

    [Header("Directional Look Ahead")]
    public float lookAheadDistance = 4f;
    public float lookAheadSmoothTime = 0.25f;

    float currentLookAhead;
    float lookAheadVelocity;

    [Header("Dead Zone")]
    public float deadZoneWidth = 1.5f;
    public float deadZoneHeight = 1.2f;

    [Header("Smooth Follow")]
    public float followSmoothTimeX = 0.2f;
    public float followSmoothTimeY = 0.15f;

    Vector3 velocity;

    [Header("Bounds")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Camera Shake")]
    public float shakeDecay = 6f;
    public float shakeFrequency = 18f;
    public float maxShakeOffset = 0.35f;

    float shakeStrength;
    float shakeTime;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (!target) return;

        UpdateLookAhead();

        Vector3 camPos = transform.position;
        Vector3 playerPos = target.position;

        float desiredX = camPos.x;
        float desiredY = camPos.y;

        float targetX = playerPos.x + offset.x + currentLookAhead;
        float targetY = playerPos.y + offset.y;

        float dx = targetX - camPos.x;

        if (Mathf.Abs(dx) > deadZoneWidth)
            desiredX += dx - Mathf.Sign(dx) * deadZoneWidth;

        float dy = targetY - camPos.y;

        if (Mathf.Abs(dy) > deadZoneHeight)
            desiredY += dy - Mathf.Sign(dy) * deadZoneHeight;

        float x = Mathf.SmoothDamp(
            transform.position.x,
            desiredX,
            ref velocity.x,
            followSmoothTimeX
        );

        float y = Mathf.SmoothDamp(
            transform.position.y,
            desiredY,
            ref velocity.y,
            followSmoothTimeY
        );

        Vector2 shakeOffset = UpdateShake();

        Vector3 finalPos = new Vector3(
            x + shakeOffset.x,
            y + shakeOffset.y,
            transform.position.z
        );

        if (useBounds)
        {
            finalPos.x = Mathf.Clamp(finalPos.x, minBounds.x, maxBounds.x);
            finalPos.y = Mathf.Clamp(finalPos.y, minBounds.y, maxBounds.y);
        }

        transform.position = finalPos;
    }

    void UpdateLookAhead()
    {
        float targetLookAhead = facingDir * lookAheadDistance;

        currentLookAhead = Mathf.SmoothDamp(
            currentLookAhead,
            targetLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );
    }

    Vector2 UpdateShake()
    {
        if (shakeStrength <= 0f)
            return Vector2.zero;

        shakeTime += Time.deltaTime * shakeFrequency;

        shakeStrength = Mathf.MoveTowards(
            shakeStrength,
            0f,
            shakeDecay * Time.deltaTime
        );

        float x = (Mathf.PerlinNoise(shakeTime, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0f, shakeTime) - 0.5f) * 2f;

        return Vector2.ClampMagnitude(
            new Vector2(x, y) * shakeStrength,
            maxShakeOffset
        );
    }

    public void Shake(float intensity)
    {
        shakeStrength = Mathf.Clamp(
            shakeStrength + intensity,
            0f,
            maxShakeOffset
        );
    }

    public void SetFacingDirection(bool facingRight)
    {
        facingDir = facingRight ? 1 : -1;
    }
    // ================= LEGACY API (Compatibility) =================

    bool lockX;
    float lockedX;

    public void UnlockX()
    {
        lockX = false;
    }

    public void LockX()
    {
        lockX = true;
        lockedX = transform.position.x;
    }

    public void SnapToTarget()
    {
        if (!target) return;

        Vector3 snapPos = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        transform.position = snapPos;

        velocity = Vector3.zero;
        currentLookAhead = facingDir * lookAheadDistance;
    }
}