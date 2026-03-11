using UnityEngine;

public class MachineGroup : MonoBehaviour
{
    public SpikeTrap[] traps;
    public float maxHealth = 3f;

    private float currentHealth;
    private bool isActive;
    private bool isDestroyed;

    public bool IsDestroyed => isDestroyed;
    public bool IsActive => isActive;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => currentHealth / maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ActivateMachine()
    {
        if (isActive || isDestroyed) return;
        isActive = true;

        // Activate ALL traps in the group
        foreach (SpikeTrap trap in traps)
        {
            if (trap != null)
                trap.Activate();
        }

        Debug.Log($"[MachineGroup] Activated. Health: {currentHealth}/{maxHealth}");
    }

    public void DeactivateMachine()
    {
        if (!isActive) return;
        isActive = false;

        // Deactivate ALL traps in the group
        foreach (SpikeTrap trap in traps)
        {
            if (trap != null)
                trap.Deactivate();
        }

        Debug.Log($"[MachineGroup] Deactivated");
    }

    public void TakeDamage(float damage)
    {
        if (!isActive || isDestroyed) return;

        currentHealth -= damage;
        Debug.Log($"[MachineGroup] HP = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            BreakMachine();
    }

    void BreakMachine()
    {
        isDestroyed = true;
        Debug.Log("[MachineGroup] Destroyed");
        DeactivateMachine();
    }

    public void ResetMachine()
    {
        currentHealth = maxHealth;
        isActive = false;
        isDestroyed = false;
        Debug.Log("[MachineGroup] Reset");
    }
}