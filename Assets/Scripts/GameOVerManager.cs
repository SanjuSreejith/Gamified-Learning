using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public CanvasGroup gameOverCanvas;   // 👈 use CanvasGroup, NOT GameObject
    public float fadeDuration = 0.6f;

    bool isGameOver = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        gameOverCanvas.alpha = 0f;
        gameOverCanvas.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        gameOverCanvas.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            gameOverCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        gameOverCanvas.alpha = 1f;
        Time.timeScale = 0f; // pause AFTER fade
    }

    void Update()
    {
        // 👇 Tap anywhere to restart
        if (isGameOver && Input.GetMouseButtonDown(0))
        {
            Restart();
        }
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
