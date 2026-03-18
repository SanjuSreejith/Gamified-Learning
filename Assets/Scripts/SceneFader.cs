using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartFadeOut : MonoBehaviour
{
    [Header("Fade Settings")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.5f;

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

        // IMPORTANT: Set this in Inspector also to avoid flicker
        fadeCanvasGroup.alpha = 1f;

        StartCoroutine(FadeOut());
    }

    // 🔥 Smooth Fade Out (black → clear)
    public IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / fadeDuration;

            // Smooth cinematic easing
            t = t * t * (3f - 2f * t);

            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    // 🔥 Smooth Fade In (clear → black)
    public IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / fadeDuration;

            // Smooth cinematic easing
            t = t * t * (3f - 2f * t);

            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    // 🔥 Fade and Load Scene (NO sudden cuts)
    public void FadeAndLoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        yield return StartCoroutine(FadeIn());

        // Small delay for polish (optional)
        yield return new WaitForSecondsRealtime(0.1f);

        SceneManager.LoadScene(sceneName);
    }
}