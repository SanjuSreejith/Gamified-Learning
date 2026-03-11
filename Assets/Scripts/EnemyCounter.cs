using UnityEngine;
using TMPro;
using System.Collections;

public class EnemyCounter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI enemyCountText;

    [Header("Enemy Count")]
    [SerializeField] private int enemyCount = 100;

    void Start()
    {
        UpdateUI();
    }

    /* ================= REAL ENEMY ================= */

    // Call this from REAL enemy scripts (single enemy kill)
    public void RealEnemyKilled()
    {
        DecreaseEnemy(1);
    }

    // Called from towers to reduce multiple enemies
    public void RealEnemyKilled(int amount)
    {
        DecreaseEnemy(amount);
    }

    /* ================= FAKE DEATH ================= */

    // Fake deaths (visual/logic illusion, not real enemies)
    public void FakeEnemyKilled(int amount)
    {
        DecreaseEnemy(amount);
        Debug.Log($"[FAKE] Enemies faked as dead: {amount}");
    }
    /* ================= COMMON ================= */

    void DecreaseEnemy(int amount)
    {
        if (enemyCount <= 0) return;

        enemyCount -= amount;
        if (enemyCount < 0) enemyCount = 0;

        UpdateUI();

        Debug.Log($"Enemies decreased by {amount}. Remaining: {enemyCount}");
    }

    void UpdateUI()
    {
        if (enemyCountText != null)
            enemyCountText.text = $"Enemies Remaining: {enemyCount}";
    }

    /* ================= UTILITY METHODS ================= */

    public int GetEnemyCount()
    {
        return enemyCount;
    }

    public bool HasEnemiesRemaining()
    {
        return enemyCount > 0;
    }

    public void SetEnemyCount(int newCount)
    {
        enemyCount = newCount;
        UpdateUI();
    }

    // Optional: Reset for new game/level
    public void ResetEnemyCount(int startingCount)
    {
        enemyCount = startingCount;
        UpdateUI();
    }
}