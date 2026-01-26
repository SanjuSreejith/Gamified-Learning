using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AbelIntroTrigger2D : MonoBehaviour
{
    public AbelIntroNPC abelNPC;

    public bool triggerOnce = true;
    bool hasTriggered;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;

        if (!other.CompareTag("Player")) return;

        if (abelNPC == null)
        {
            Debug.LogError("[AbelIntroTrigger2D] AbelIntroNPC not assigned!");
            return;
        }

        abelNPC.StartDialogue();
        hasTriggered = true;

        GetComponent<Collider2D>().enabled = false;
    }
}
