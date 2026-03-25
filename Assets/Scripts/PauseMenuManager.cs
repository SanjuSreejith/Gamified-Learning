using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;

    [Header("Scene")]
    public string menuSceneName = "GameMenu";

    bool isPaused = false;

    // Reference to door trigger to check if UI is active
    DoorPythonInputLesson_Trigger doorTrigger;

    void Start()
    {
        if (pausePanel)
            pausePanel.SetActive(false);

        // Find door trigger
        doorTrigger = FindObjectOfType<DoorPythonInputLesson_Trigger>();
    }

    void Update()
    {
        // Only allow pause if not in UI state
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't pause if UI is active
            if (doorTrigger != null && doorTrigger.IsInUIState())
                return;

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

        // Lock/unlock cursor
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /* ================= RESUME ================= */

    public void ResumeGame()
    {
        isPaused = false;

        if (pausePanel)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /* ================= RESTART ================= */

    public void RestartScene()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /* ================= EXIT ================= */

    public void ExitToMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(menuSceneName);
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}