using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StatueDialogueTrigger2D : MonoBehaviour
{
    [Header("References")]
    public StatueDialogueTriggerSystem2D statueSystem;

    [Header("Trigger Settings")]
    public bool triggerOnce = true;
    public bool disableColliderAfterTrigger = true;

    bool hasTriggered = false;

    void Reset()
    {
        // Safety: ensure trigger is set
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug to confirm trigger fires
        Debug.Log("[StatueTrigger2D] Entered by: " + other.name);

        // Prevent re-trigger
        if (hasTriggered && triggerOnce)
            return;

        // Only player can trigger
        if (!other.CompareTag("Player"))
        {
            Debug.Log("[StatueTrigger2D] Ignored (not player)");
            return;
        }

        // Safety check
        if (statueSystem == null)
        {
            Debug.LogError("[StatueTrigger2D] StatueDialogueTriggerSystem2D not assigned!");
            return;
        }

        // Start dialogue
        statueSystem.StartDialogue();
        hasTriggered = true;

        // Optional: disable trigger collider
        if (triggerOnce && disableColliderAfterTrigger)
        {
            GetComponent<Collider2D>().enabled = false;
        }

        Debug.Log("[StatueTrigger2D] Dialogue started");
    }
}
