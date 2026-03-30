using UnityEngine;
using TMPro;
using System.Collections;

public class CoinUIController : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public float countSpeed = 0.02f;
    public float visibleTime = 1.2f;

    [Header("Display Mode")]
    public bool alwaysVisible = false; // Unticked by default

    CanvasGroup canvasGroup;
    int displayedCoins = 0;
    Coroutine routine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Load existing coins
        displayedCoins = PlayerPrefs.GetInt("Coins", 0);
        coinText.text = displayedCoins.ToString();

        if (alwaysVisible)
        {
            canvasGroup.alpha = 1;
            gameObject.SetActive(true);
        }
        else
        {
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
    }

    public void ShowAndAdd(int addAmount)
    {
        if (!alwaysVisible)
        {
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            canvasGroup.alpha = 1;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AddRoutine(addAmount));
    }

    public void ShowAndRemove(int removeAmount)
    {
        if (!alwaysVisible)
        {
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            canvasGroup.alpha = 1;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(RemoveRoutine(removeAmount));
    }

    IEnumerator AddRoutine(int addAmount)
    {
        int target = displayedCoins + addAmount;

        while (displayedCoins < target)
        {
            displayedCoins++;
            coinText.text = displayedCoins.ToString();
            yield return new WaitForSecondsRealtime(countSpeed);
        }

        PlayerPrefs.SetInt("Coins", displayedCoins);

        if (!alwaysVisible)
        {
            yield return new WaitForSecondsRealtime(visibleTime);
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
    }

    IEnumerator RemoveRoutine(int removeAmount)
    {
        int target = displayedCoins - removeAmount;

        while (displayedCoins > target)
        {
            displayedCoins--;
            coinText.text = displayedCoins.ToString();
            yield return new WaitForSecondsRealtime(countSpeed);
        }

        PlayerPrefs.SetInt("Coins", displayedCoins);

        if (!alwaysVisible)
        {
            yield return new WaitForSecondsRealtime(visibleTime);
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
    }
}