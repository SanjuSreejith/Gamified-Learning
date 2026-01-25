using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class StatueTrigger2D_Event : MonoBehaviour
{
    [Header("Trigger Settings")]
    public bool triggerOnce = true;
    public bool disableColliderAfterTrigger = true;

    [Header("On Trigger")]
    public UnityEvent onTrigger;   // Drag any statue method here

    bool hasTriggered = false;

    void Reset()
    {
        // Ensure trigger collider
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;

        if (onTrigger == null)
        {
            Debug.LogWarning("[StatueTrigger2D] No event assigned.");
            return;
        }

        onTrigger.Invoke();
        hasTriggered = true;

        if (triggerOnce && disableColliderAfterTrigger)
            GetComponent<Collider2D>().enabled = false;
    }
}
