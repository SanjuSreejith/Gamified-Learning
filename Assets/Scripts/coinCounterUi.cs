using TMPro;
using UnityEngine;
using System.Collections;

public class CoinCounterUI : MonoBehaviour
{
    public static CoinCounterUI Instance;

    TextMeshProUGUI text;
    int currentCoins;

    void Awake()
    {
        Instance = this;
        text = GetComponent<TextMeshProUGUI>();

        if (CoinManager.Instance != null)
            currentCoins = CoinManager.Instance.coins;
        else
            currentCoins = 0;

        text.text = currentCoins.ToString();
    }

    public void AnimateCoinIncrease(int target)
    {
        StopAllCoroutines();
        StartCoroutine(CountUp(target));
    }

    IEnumerator CountUp(int target)
    {
        while (currentCoins < target)
        {
            currentCoins++;
            text.text = currentCoins.ToString();
            yield return new WaitForSeconds(0.01f);
        }
    }
}
