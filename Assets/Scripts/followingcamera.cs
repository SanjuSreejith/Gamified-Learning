using UnityEngine;

/// <summary>
/// Professional 2D platformer camera:
/// - Intent-based directional framing
/// - Dead-zone based smooth following
/// - Stable look-ahead
/// - Cinematic camera shake
/// - Backward-compatible public API
/// </summary>
public class FollowingCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1f);

    // ================= PLAYER INTENT =================
    int facingDir; // -1 = left, 1 = right

    // ================= DEAD ZONE =================
    [Header("Dead Zone (Core Smoothness)")]
    public float deadZoneWidth = 1.2f;
    public float deadZoneHeight = 0.6f;

    // ================= FRAMING =================
    [Header("Directional Framing")]
    public float forwardScreenOffset = 1.8f;
    public float framingSmoothTime = 0.18f;

    float framingOffsetX;
    float framingVelocity;

    // ================= LOOK AHEAD =================
    [Header("Look Ahead (Polish)")]
    public float lookAheadDistance = 1.2f;
    public float lookAheadSmoothTime = 0.25f;

    float lookAheadX;
    float lookAheadVelocity;

    // ================= SMOOTHING =================
    [Header("Catch-Up Smoothing")]
    public float followSmoothTimeX = 0.25f;
    public float followSmoothTimeY = 0.2f;

    Vector3 velocity;

    // ================= AXIS LOCK (LEGACY SUPPORT) =================
    bool lockX;
    float lockedX;

    // ================= BOUNDS =================
    [Header("Bounds")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // ================= CAMERA SHAKE =================
    [Header("Camera Shake")]
    public float shakeDecay = 6f;
    public float shakeFrequency = 18f;
    public float maxShakeOffset = 0.35f;

    float shakeStrength;
    float shakeTime;

    void Awake()
    {
        lockedX = transform.position.x;
    }

    void LateUpdate()
    {
        if (!target) return;

        UpdateDirectionalFraming();
        UpdateLookAhead();
        Vector2 shakeOffset = UpdateShake();

        Vector3 desired = CalculateDeadZoneTarget();

        float x = Mathf.SmoothDamp(
            transform.position.x,
            desired.x,
            ref velocity.x,
            followSmoothTimeX
        );

        float y = Mathf.SmoothDamp(
            transform.position.y,
            desired.y,
            ref velocity.y,
            followSmoothTimeY
        );

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

        if (lockX)
            finalPos.x = lockedX;

        transform.position = finalPos;
    }

    // ================= DEAD ZONE LOGIC =================
    Vector3 CalculateDeadZoneTarget()
    {
        Vector3 camPos = transform.position;
        Vector3 playerPos = target.position;

        float desiredX = camPos.x;
        float desiredY = camPos.y;

        float frameOffset = framingOffsetX + lookAheadX;

        float dx = (playerPos.x + offset.x + frameOffset) - camPos.x;
        if (Mathf.Abs(dx) > deadZoneWidth)
            desiredX += dx - Mathf.Sign(dx) * deadZoneWidth;

        float dy = (playerPos.y + offset.y) - camPos.y;
        if (Mathf.Abs(dy) > deadZoneHeight)
            desiredY += dy - Mathf.Sign(dy) * deadZoneHeight;

        return new Vector3(desiredX, desiredY, camPos.z);
    }

    // ================= FRAMING =================
    void UpdateDirectionalFraming()
    {
        if (facingDir == 0)
        {
            framingOffsetX = Mathf.SmoothDamp(
                framingOffsetX, 0f,
                ref framingVelocity, framingSmoothTime
            );
            return;
        }

        float desired = -facingDir * forwardScreenOffset;

        framingOffsetX = Mathf.SmoothDamp(
            framingOffsetX, desired,
            ref framingVelocity, framingSmoothTime
        );
    }

    // ================= LOOK AHEAD =================
    void UpdateLookAhead()
    {
        if (facingDir == 0)
        {
            lookAheadX = Mathf.SmoothDamp(
                lookAheadX, 0f,
                ref lookAheadVelocity, lookAheadSmoothTime
            );
            return;
        }

        float desired = facingDir * lookAheadDistance;

        lookAheadX = Mathf.SmoothDamp(
            lookAheadX, desired,
            ref lookAheadVelocity, lookAheadSmoothTime
        );
    }

    // ================= CAMERA SHAKE =================
    Vector2 UpdateShake()
    {
        if (shakeStrength <= 0f)
            return Vector2.zero;

        shakeTime += Time.deltaTime * shakeFrequency;

        shakeStrength = Mathf.MoveTowards(
            shakeStrength, 0f,
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
            0f, maxShakeOffset
        );
    }

    // ================= PLAYER INTENT =================
    public void SetFacingDirection(bool facingRight)
    {
        int newDir = facingRight ? 1 : -1;

        if (newDir != facingDir)
        {
            velocity.x = 0f;
            lookAheadVelocity = 0f;
        }

        facingDir = newDir;
    }

    // ================= LEGACY PUBLIC API (FIX) =================

    /// <summary>
    /// Unlock horizontal camera movement (kept for compatibility)
    /// </summary>
    public void UnlockX()
    {
        lockX = false;
    }

    /// <summary>
    /// Instantly centers camera on target and resets smoothing
    /// </summary>
    public void SnapToTarget()
    {
        if (!target) return;

        Vector3 snapPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        transform.position = snapPos;

        lockedX = snapPos.x;
        lockX = false;

        framingOffsetX = 0f;
        lookAheadX = 0f;
        shakeStrength = 0f;
        velocity = Vector3.zero;
    }
}