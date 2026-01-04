using UnityEngine;

public class TerminalControl : MonoBehaviour
{
    [Tooltip("Assign the Canvas GameObject to show/hide when interacting.")]
    public GameObject canvasObject;

    [Tooltip("Enable to print debug messages when player enters/exits and interacts.")]
    public bool enableDebugLogs = true;

    bool playerInRange = false;

    void Start()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || canvasObject == null) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(1))
        {
            canvasObject.SetActive(!canvasObject.activeSelf);
            if (enableDebugLogs) Debug.Log($"[TerminalControl] Toggled canvas to {canvasObject.activeSelf} on {gameObject.name}");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebugLogs) Debug.Log($"[TerminalControl] OnTriggerEnter2D: {other.gameObject.name} (tag={other.tag}) on {gameObject.name}");
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (enableDebugLogs) Debug.Log("[TerminalControl] Player in range (2D)");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (enableDebugLogs) Debug.Log($"[TerminalControl] OnTriggerExit2D: {other.gameObject.name} (tag={other.tag}) on {gameObject.name}");
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (enableDebugLogs) Debug.Log("[TerminalControl] Player left range (2D)");
        }
    }

    // Fallback for 3D trigger colliders (optional)
    void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs) Debug.Log($"[TerminalControl] OnTriggerEnter: {other.gameObject.name} (tag={other.tag}) on {gameObject.name}");
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (enableDebugLogs) Debug.Log("[TerminalControl] Player in range (3D)");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (enableDebugLogs) Debug.Log($"[TerminalControl] OnTriggerExit: {other.gameObject.name} (tag={other.tag}) on {gameObject.name}");
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (enableDebugLogs) Debug.Log("[TerminalControl] Player left range (3D)");
        }
    }
}
