using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverRest : MonoBehaviour
{
    // Tag to identify the player object (set in the Inspector or use "Player")
    public string playerTag = "Player";

    // Optional: if set, this scene name will be loaded; otherwise scene at index 0 is loaded
    public string restartSceneName = "";

    void Start()
    {
    }

    void Update()
    {
    }

    // Centralized restart helper
    void Restart()
    {
        if (!string.IsNullOrEmpty(restartSceneName))
            SceneManager.LoadScene(restartSceneName);
        else
            SceneManager.LoadScene(0);
    }

    // Called when a 2D trigger collider enters this object's trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag(playerTag))
            Restart();
    }

    // Called when a 3D trigger collider enters this object's trigger
    void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag(playerTag))
            Restart();
    }

    // Handle clicks/touches on the object (works for mobile touch as well if object has a collider)
    void OnMouseDown()
    {
        Restart();
    }
}
