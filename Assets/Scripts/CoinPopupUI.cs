using UnityEngine;
using TMPro;
using System.Collections;

public class CoinUIController : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public float countSpeed = 0.02f;
    public float visibleTime = 1.2f;

    CanvasGroup canvasGroup;
    int displayedCoins = 0;
    Coroutine routine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Load existing coins
        displayedCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        coinText.text = displayedCoins.ToString();

        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void ShowAndAdd(int addAmount)
    {
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        canvasGroup.alpha = 1;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine(addAmount));
    }

    IEnumerator ShowRoutine(int addAmount)
    {
        int target = displayedCoins + addAmount;

        while (displayedCoins < target)
        {
            displayedCoins++;
            coinText.text = displayedCoins.ToString();
            yield return new WaitForSecondsRealtime(countSpeed);
        }

        yield return new WaitForSecondsRealtime(visibleTime);

        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
}