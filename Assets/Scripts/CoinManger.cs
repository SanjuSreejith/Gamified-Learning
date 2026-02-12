using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    public int coins;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        coins = PlayerPrefs.GetInt("Coins", 0);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        PlayerPrefs.SetInt("Coins", coins);
        PlayerPrefs.Save();

        Debug.Log("Coins Added! Total Coins: " + coins);
    }
}
