using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Progress Manager")]
    public SceneProgressManager progressManager;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Animator settingsAnimator;

    [Header("Coin UI")]
    public TextMeshProUGUI coinText;

    bool settingsOpen = false;

    void Start()
    {
        // Find progress manager if missing
        if (progressManager == null)
            progressManager = FindObjectOfType<SceneProgressManager>();

        // Find coin text safely
        if (coinText == null)
            coinText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        UpdateCoinText();

        // Refresh progress bar
        if (progressManager != null)
            progressManager.UpdateProgress();
    }

    void Update()
    {
        if (settingsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
        }
    }

    // -------------------------
    // COIN DISPLAY
    // -------------------------
    public void UpdateCoinText()
    {
        if (coinText == null) return;

        int coins = PlayerPrefs.GetInt("Coins", 0);
        coinText.text = coins.ToString();
    }

    // -------------------------
    // PLAY BUTTON
    // -------------------------
    public void PlayGame()
    {
        if (progressManager == null)
        {
            Debug.LogError("SceneProgressManager not assigned!");
            return;
        }

        List<string> scenes = progressManager.sceneNames;

        foreach (string scene in scenes)
        {
            string key1 = "Scene_" + scene + "_Completed";
            string key2 = "SceneCompleted_" + scene;

            if (PlayerPrefs.GetInt(key1, 0) == 0 &&
                PlayerPrefs.GetInt(key2, 0) == 0)
            {
                SceneManager.LoadScene(scene);
                return;
            }
        }

        // If all completed restart first level
        if (scenes.Count > 0)
            SceneManager.LoadScene(scenes[0]);
    }

    // -------------------------
    // SETTINGS
    // -------------------------
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(true);

        if (settingsAnimator != null)
            settingsAnimator.SetTrigger("Open");

        settingsOpen = true;
    }

    public void CloseSettings()
    {
        if (settingsAnimator != null)
            settingsAnimator.SetTrigger("Close");

        StartCoroutine(HideSettings());
    }

    IEnumerator HideSettings()
    {
        yield return new WaitForSeconds(0.35f);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        settingsOpen = false;
    }

    // -------------------------
    // RESET PROGRESS
    // -------------------------
    public void ResetPlayerPrefsOnly()
    {
        Debug.Log("Resetting progress...");

        if (progressManager != null)
        {
            foreach (string scene in progressManager.sceneNames)
            {
                PlayerPrefs.DeleteKey("Scene_" + scene + "_Completed");
                PlayerPrefs.DeleteKey("SceneCompleted_" + scene);
            }
        }

        PlayerPrefs.Save();

        if (progressManager != null)
            progressManager.UpdateProgress();
    }

    // -------------------------
    // EXIT GAME
    // -------------------------
    public void ExitGame()
    {
        Debug.Log("Exit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}