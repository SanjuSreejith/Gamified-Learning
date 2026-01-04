using UnityEngine;

/// <summary>
/// 2D smooth following camera for platformer.
/// Attach to the Camera object and assign the target Transform (player).
/// Preserves camera Z, supports smoothing and optional bounds.
/// </summary>
public class followingcamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("Offset (x,y) from the target. Camera Z is preserved.")]
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Smoothing")]
    [Tooltip("Approximate time for the camera to catch up. Smaller = snappier.")]
    public float smoothTime = 0.15f;
    private Vector3 velocity = Vector3.zero;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        if (useBounds)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minBounds.x, maxBounds.x);
            smoothed.y = Mathf.Clamp(smoothed.y, minBounds.y, maxBounds.y);
        }

        transform.position = smoothed;
    }
}
