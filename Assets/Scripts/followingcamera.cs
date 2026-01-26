using UnityEngine;

/// <summary>
/// 2D smooth following camera for platformer.
/// Supports optional X-axis lock (camera never moves horizontally).
/// </summary>
public class followingcamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Smoothing")]
    public float smoothTime = 0.15f;
    private Vector3 velocity = Vector3.zero;

    [Header("Axis Locks")]
    public bool lockX = false;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    float lockedX;

    void Awake()
    {
        lockedX = transform.position.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            lockX ? lockedX : target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        Vector3 smoothed = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            smoothTime
        );

        if (useBounds)
        {
            if (!lockX)
                smoothed.x = Mathf.Clamp(smoothed.x, minBounds.x, maxBounds.x);

            smoothed.y = Mathf.Clamp(smoothed.y, minBounds.y, maxBounds.y);
        }

        if (lockX)
            smoothed.x = lockedX;

        transform.position = smoothed;
    }

    // ================= NEW METHODS =================

    /// <summary>
    /// Unlock X movement (camera follows player horizontally)
    /// </summary>
    public void UnlockX()
    {
        lockX = false;
    }

    /// <summary>
    /// Lock X at current camera position
    /// </summary>
    public void LockXAtCurrentPosition()
    {
        lockedX = transform.position.x;
        lockX = true;
    }

    /// <summary>
    /// Instantly center camera on target (used after teleport)
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        transform.position = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        lockedX = transform.position.x;
        velocity = Vector3.zero;
    }
}
