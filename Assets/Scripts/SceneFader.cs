using UnityEngine;
using System.Collections;

public class StartFadeOut : MonoBehaviour
{
    [Header("Fade Settings")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    void Awake()
    {
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("StartFadeOut: No CanvasGroup assigned!");
            return;
        }

        // Scene start → fade OUT (black → clear)
        fadeCanvasGroup.alpha = 1f;
        StartCoroutine(FadeOut());
    }

    public IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    // ⭐ NEW: Fade IN (clear → black)
    public IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }
}