using UnityEngine;

public class GameOverTrigger : MonoBehaviour
{
    [SerializeField] string gameOverLayerName = "GameOver";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(gameOverLayerName))
            return;

        // Player OR NPC touches river
        if (other.CompareTag("Player") || other.CompareTag("NPC"))
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }
}
