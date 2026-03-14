using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;

    [Header("Scene")]
    public string menuSceneName = "GameMenu";

    bool isPaused = false;

    void Start()
    {
        if (pausePanel)
            pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /* ================= TOGGLE ================= */

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel)
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    /* ================= RESUME ================= */

    public void ResumeGame()
    {
        isPaused = false;

        if (pausePanel)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    /* ================= RESTART ================= */

    public void RestartScene()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /* ================= EXIT ================= */

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}