using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoSceneController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Scene Settings")]
    public string nextSceneName;

    private bool hasEnded = false;

    void Awake()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned!");
            return;
        }

        // Ensure video doesn't auto play twice
        videoPlayer.playOnAwake = false;

        // Subscribe to video end event
        videoPlayer.loopPointReached += OnVideoEnd;

        // Play video
        videoPlayer.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        if (hasEnded) return;
        hasEnded = true;

        Debug.Log("Video Finished!");

        // Mark scene as completed
        MarkSceneCompleted();

        // Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next scene name not set!");
        }
    }

    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        PlayerPrefs.SetInt("SceneCompleted_" + sceneName, 1);

        PlayerPrefs.Save();

        Debug.Log("Scene Marked Completed: " + sceneName);
    }

    // Optional: Skip video with input
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Video Skipped!");

            videoPlayer.Stop();
            OnVideoEnd(videoPlayer);
        }
    }
}