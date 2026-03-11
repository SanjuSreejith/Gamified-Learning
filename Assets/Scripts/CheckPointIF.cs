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

    const string COIN_KEY = "PlayerCoins";

    bool collected = false;

    void Awake()
    {
        if (childAnimator == null)
            childAnimator = GetComponentInChildren<Animator>();

        // Check if already collected before
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

        // Load current coins
        int coins = PlayerPrefs.GetInt(COIN_KEY, 0);

        // Add reward
        coins += rewardCoins;

        // Save coins
        PlayerPrefs.SetInt(COIN_KEY, coins);

        // Mark checkpoint collected
        PlayerPrefs.SetInt("Checkpoint_" + checkpointID, 1);

        PlayerPrefs.Save();

        // Show UI animation
        if (coinUI != null)
            coinUI.ShowAndAdd(rewardCoins);

        // Play animation
        if (childAnimator != null)
            childAnimator.SetTrigger(triggerName);
    }
}