using UnityEngine;

public class CheckpointReward : MonoBehaviour
{
    [Header("Reward")]
    public int rewardCoins = 50;

    [Header("Checkpoint ID (Unique)")]
    public string checkpointID;

    [Header("Animation")]
    public Animator childAnimator;
    public string triggerName = "Activate";

    [Header("UI")]
    public CoinUIController coinUI;

    bool collected = false;

    void Awake()
    {
        if (childAnimator == null)
            childAnimator = GetComponentInChildren<Animator>();

        if (PlayerPrefs.GetInt("Checkpoint_" + checkpointID, 0) == 1)
        {
            collected = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        // Add coins
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(rewardCoins);
        }
        else
        {
            // Fallback if CoinManager not in scene
            int coins = PlayerPrefs.GetInt("Coins", 0);
            coins += rewardCoins;
            PlayerPrefs.SetInt("Coins", coins);
            PlayerPrefs.Save();

            Debug.Log("Coins Added (fallback). Total: " + coins);
        }

        // Mark checkpoint collected
        PlayerPrefs.SetInt("Checkpoint_" + checkpointID, 1);
        PlayerPrefs.Save();

        // UI animation
        if (coinUI != null)
            coinUI.ShowAndAdd(rewardCoins);

        // Play animation
        if (childAnimator != null)
            childAnimator.SetTrigger(triggerName);
    }
}