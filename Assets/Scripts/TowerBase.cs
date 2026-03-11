using UnityEngine;

public class Tower : MonoBehaviour
{
    public enum TowerType
    {
        Button,
        Timer,
        Health,
        Emergency
    }

    [Header("Tower Type")]
    public TowerType towerType;

    [Header("Controlled Machine")]
    public MachineGroup machine;

    [Header("Timer Settings (Only for Timer Towers)")]
    public float timerDuration = 5f;

    [Header("Runtime State")]
    public bool isActive = false;
    private float timerRemaining;
    private bool buttonHeld = false;
    private float machineHealth = 100f;

    void Update()
    {
        if (!machine) return;
        if (!isActive) return;

        switch (towerType)
        {
            case TowerType.Button:
                // Button tower: active only while button is held
                if (!buttonHeld)
                {
                    DeactivateMachine();
                }
                break;

            case TowerType.Timer:
                // Timer tower: active until timer runs out
                timerRemaining -= Time.deltaTime;
                if (timerRemaining <= 0)
                {
                    DeactivateMachine();
                }
                break;

            case TowerType.Health:
                // Health tower: active until machine health reaches 0
                if (machineHealth <= 0)
                {
                    DeactivateMachine();
                }
                break;

            case TowerType.Emergency:
                // Emergency tower: active until break command
                // This runs indefinitely until manually stopped
                break;
        }
    }

    public void Activate()
    {
        if (!machine) return;

        isActive = true;

        // Initialize based on tower type
        switch (towerType)
        {
            case TowerType.Timer:
                timerRemaining = timerDuration;
                break;

            case TowerType.Button:
                buttonHeld = true;
                break;

            case TowerType.Health:
                machineHealth = 100f; // Or get from machine
                break;

            case TowerType.Emergency:
                // No initialization needed
                break;
        }

        machine.ActivateMachine();
    }

    public void Deactivate()
    {
        if (!machine) return;

        isActive = false;
        machine.DeactivateMachine();
    }

    // Call this when button is released
    public void ReleaseButton()
    {
        if (towerType == TowerType.Button)
        {
            buttonHeld = false;
        }
    }

    // Call this when machine takes damage
    public void DamageMachine(float damage)
    {
        if (towerType == TowerType.Health)
        {
            machineHealth -= damage;
        }
    }

    private void DeactivateMachine()
    {
        if (isActive)
        {
            Deactivate();
        }
    }

    // Reset tower state
    public void ResetTower()
    {
        isActive = false;
        buttonHeld = false;
        timerRemaining = 0f;
        machineHealth = 100f;
    }
}