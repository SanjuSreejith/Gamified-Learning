using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public CanvasGroup gameOverCanvas;
    public float fadeDuration = 0.6f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gameOverSound;

    [Header("Settings")]
    public bool pauseGameOnGameOver = true;
    public bool restartOnTap = true;

    private bool isGameOver = false;
    private bool isFading = false;

    /* ================= INITIALIZATION ================= */

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameOverManager initialized");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (gameOverCanvas == null)
        {
            Debug.LogError("GameOverCanvas is NOT assigned!");
            return;
        }

        gameOverCanvas.alpha = 0f;
        gameOverCanvas.interactable = false;
        gameOverCanvas.blocksRaycasts = false;

        gameOverCanvas.gameObject.SetActive(true);
    }

    /* ================= GAME OVER ================= */

    public void ShowGameOver()
    {
        if (isGameOver || isFading) return;

        Debug.Log("GAME OVER TRIGGERED");

        isGameOver = true;

        if (audioSource != null && gameOverSound != null)
            audioSource.PlayOneShot(gameOverSound);

        StartCoroutine(FadeIn());
    }

    /* ================= FADE ================= */

    IEnumerator FadeIn()
    {
        isFading = true;

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;

            if (gameOverCanvas != null)
                gameOverCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);

            yield return null;
        }

        gameOverCanvas.alpha = 1f;
        gameOverCanvas.interactable = true;
        gameOverCanvas.blocksRaycasts = true;

        if (pauseGameOnGameOver)
            Time.timeScale = 0f;

        isFading = false;
    }

    /* ================= INPUT ================= */

    void Update()
    {
        if (!isGameOver || isFading) return;

        if (restartOnTap && Input.GetMouseButtonDown(0))
        {
            Restart();
        }
    }

    /* ================= RESTART ================= */

    public void Restart()
    {
        Debug.Log("Restarting Scene");

        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /* ================= HIDE ================= */

    public void HideGameOver()
    {
        StopAllCoroutines();

        if (gameOverCanvas != null)
        {
            gameOverCanvas.alpha = 0f;
            gameOverCanvas.interactable = false;
            gameOverCanvas.blocksRaycasts = false;
        }

        isGameOver = false;
        isFading = false;

        Time.timeScale = 1f;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }
}