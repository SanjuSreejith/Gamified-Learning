using UnityEngine;

public class MoveWithCam : MonoBehaviour
{
    [Tooltip("Transform of the camera to follow. If left empty, will use Camera.main.")]
    public Transform cameraToFollow;

    [Tooltip("World-space offset from the camera position when not parenting.")]
    public Vector3 offset = Vector3.zero;

    [Tooltip("If true, the GameObject will become a child of the camera (keeps local offset).")]
    public bool useParenting = false;

    [Tooltip("If true, also match camera rotation.")]
    public bool followRotation = false;

    [Tooltip("If true the object will start following the camera on Start. If false, other scripts can call StartFollowing()/StopFollowing() to control behavior.")]
    public bool startFollowing = false;

    [Tooltip("When true and following, the object will be kept at the center of the camera's view (preserves the object's Z).")]
    public bool centerOnCamera = false;

    Transform originalParent;
    bool isParentedToCamera = false;
    RectTransform uiRect;
    Canvas parentCanvas;

    void Start()
    {
        originalParent = transform.parent;

        if (cameraToFollow == null && Camera.main != null)
            cameraToFollow = Camera.main.transform;

        if (cameraToFollow == null)
        {
            Debug.LogWarning($"[MoveWithCam] No camera assigned and Camera.main is null for {gameObject.name}.");
            return;
        }

        if (!startFollowing) return;

        // Cache UI components if present
        uiRect = GetComponent<RectTransform>();
        if (uiRect != null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (useParenting)
        {
            if (uiRect != null)
            {
                Debug.LogWarning($"[MoveWithCam] UI element detected on {gameObject.name}; skipping parenting to camera to preserve Canvas.");
            }
            else
            {
                transform.SetParent(cameraToFollow, true);
                transform.localPosition = centerOnCamera ? Vector3.zero : offset;
                isParentedToCamera = true;
            }
        }
        else
        {
            if (centerOnCamera)
                transform.position = new Vector3(cameraToFollow.position.x, cameraToFollow.position.y, transform.position.z);
            else
                transform.position = cameraToFollow.position + offset;
            if (followRotation) transform.rotation = cameraToFollow.rotation;
        }
    }

    void LateUpdate()
    {
        if (cameraToFollow == null) return;

        if (!startFollowing) return;

        if (centerOnCamera)
        {
            // If this is a UI element, update its anchored position to the camera center on screen
            if (uiRect != null && parentCanvas != null)
            {
                UpdateUIPositionCenter();
            }
            else
            {
                // Center on camera's X/Y while preserving our Z
                transform.position = new Vector3(cameraToFollow.position.x, cameraToFollow.position.y, transform.position.z);
                if (followRotation) transform.rotation = cameraToFollow.rotation;
            }
            return;
        }

        if (!useParenting)
        {
            transform.position = cameraToFollow.position + offset;
            if (followRotation) transform.rotation = cameraToFollow.rotation;
        }
    }

    // External control API -------------------------------------------------
    public void StartFollowing()
    {
        if (cameraToFollow == null && Camera.main != null) cameraToFollow = Camera.main.transform;
        if (cameraToFollow == null) return;

        startFollowing = true;
        // Refresh UI cache
        uiRect = GetComponent<RectTransform>();
        if (uiRect != null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (useParenting)
        {
            if (uiRect != null)
            {
                Debug.LogWarning($"[MoveWithCam] UI element detected on {gameObject.name}; skipping parenting to camera to preserve Canvas.");
                // If center requested, immediately position in UI space
                if (centerOnCamera && parentCanvas != null) UpdateUIPositionCenter();
            }
            else
            {
                transform.SetParent(cameraToFollow, true);
                transform.localPosition = centerOnCamera ? Vector3.zero : offset;
                isParentedToCamera = true;
            }
        }
        else
        {
            if (centerOnCamera)
                transform.position = new Vector3(cameraToFollow.position.x, cameraToFollow.position.y, transform.position.z);
            else
                transform.position = cameraToFollow.position + offset;
        }
        if (followRotation) transform.rotation = cameraToFollow.rotation;
    }

    void UpdateUIPositionCenter()
    {
        if (cameraToFollow == null || uiRect == null || parentCanvas == null) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cameraToFollow.GetComponent<Camera>() ?? Camera.main, cameraToFollow.position);
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (parentCanvas.worldCamera), out localPoint))
        {
            uiRect.anchoredPosition = localPoint;
        }
    }

    public void StopFollowing()
    {
        startFollowing = false;
        if (useParenting && isParentedToCamera)
        {
            transform.SetParent(originalParent, true);
            isParentedToCamera = false;
        }
    }
}
