using UnityEngine;

public class HealthMachine : MonoBehaviour
{
    [Header("Target Machine")]
    public MachineGroup machine;

    [Header("Damage")]
    public float damagePerHit = 1f;

    public void TakeDamage()
    {
        if (!machine)
        {
            Debug.LogError("[HealthMachine] No MachineGroup assigned!");
            return;
        }

        machine.TakeDamage(damagePerHit);
    }
}