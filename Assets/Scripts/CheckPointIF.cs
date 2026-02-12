using UnityEngine;

public class CheckpointReward : MonoBehaviour
{
    public int rewardCoins = 50;
    public Animator childAnimator;
    public string triggerName = "Activate";
    public CoinUIController coinUI;

    bool collected = false;

    void Awake()
    {
        if (childAnimator == null)
            childAnimator = GetComponentInChildren<Animator>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        if (coinUI != null)
            coinUI.ShowAndAdd(rewardCoins);
        else
            Debug.LogError("❌ CoinUIController not assigned");

        if (childAnimator != null)
            childAnimator.SetTrigger(triggerName);
    }
}
