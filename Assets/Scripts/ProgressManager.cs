using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SceneProgressManager : MonoBehaviour
{
    [Header("Progress UI")]
    public Slider progressSlider;

    [Header("Progress Configuration")]
    public int totalLevels = 8;  // You can set this to 8 in the Inspector

    [Header("Scenes In Order (Optional)")]
    public List<string> sceneNames = new List<string>()
    {
        "TerminalVariableExercise",
        "DoorPrintfLesson",
        "DoorPythonInputLesson",
        "AbelIntro",
        "RiverIfElseLesson"
    };

    float progress;

    void Start()
    {
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        int completed = 0;

        // Count completed scenes from the list
        foreach (string scene in sceneNames)
        {
            string key1 = "Scene_" + scene + "_Completed";
            string key2 = "SceneCompleted_" + scene;

            if (PlayerPrefs.GetInt(key1, 0) == 1 || PlayerPrefs.GetInt(key2, 0) == 1)
            {
                completed++;
            }
        }

        // Calculate progress based on totalLevels, not sceneNames.Count
        progress = (float)completed / totalLevels;

        // Clamp between 0 and 1
        progress = Mathf.Clamp01(progress);

        if (progressSlider != null)
            progressSlider.value = progress;

        Debug.Log($"Game Progress: {completed}/{totalLevels} levels completed ({progress * 100f:F1}%)");
    }

    // Optional: Add a method to get completion stats
    public string GetProgressStats()
    {
        int completed = 0;

        foreach (string scene in sceneNames)
        {
            string key1 = "Scene_" + scene + "_Completed";
            string key2 = "SceneCompleted_" + scene;

            if (PlayerPrefs.GetInt(key1, 0) == 1 || PlayerPrefs.GetInt(key2, 0) == 1)
            {
                completed++;
            }
        }

        return $"Completed: {completed}/{totalLevels}";
    }

    // Optional: Add a method to get raw progress value
    public float GetProgressValue()
    {
        int completed = 0;

        foreach (string scene in sceneNames)
        {
            string key1 = "Scene_" + scene + "_Completed";
            string key2 = "SceneCompleted_" + scene;

            if (PlayerPrefs.GetInt(key1, 0) == 1 || PlayerPrefs.GetInt(key2, 0) == 1)
            {
                completed++;
            }
        }

        return (float)completed / totalLevels;
    }
}