using UnityEngine;
using System.Collections;

public class StartFadeOut : MonoBehaviour
{
    [Header("Fade Settings")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    void Start()
    {
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponent<CanvasGroup>();

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("StartFadeOut: No CanvasGroup assigned or found!");
            return;
        }

        // Start fully opaque
        fadeCanvasGroup.alpha = 1f;
        // Begin fading out
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        // Optionally disable the panel to save performance
        // fadeCanvasGroup.gameObject.SetActive(false);
    }
}