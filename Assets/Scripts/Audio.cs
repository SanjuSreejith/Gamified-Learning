using UnityEngine;

/// <summary>
/// Simple inspector component to assign a music clip and/or an SFX clip
/// and play them using the project's `MusicManager` and `SFXManager`.
/// Use the context menu items in the inspector for quick testing.
/// </summary>
public class Audio : MonoBehaviour
{
    [Header("Music")]
    public AudioClip musicClip;
    public bool playMusicOnStart = false;
    public float musicFadeTime = 1f;

    [Header("SFX")]
    public AudioClip sfxClip;
    [Range(0f,1f)] public float sfxVolume = 1f;

    private void Start()
    {
        if (playMusicOnStart && musicClip != null)
        {
            if (MusicManager.Instance != null)
                MusicManager.Instance.PlayMusic(musicClip, musicFadeTime, true);
            else
                Debug.LogWarning("MusicManager not found. Add a MusicManager to the scene or enable the bootstrap.");
        }
    }

    [ContextMenu("Play Music")]
    public void PlayMusicNow()
    {
        if (musicClip == null)
        {
            Debug.LogWarning("No music clip assigned.");
            return;
        }
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMusic(musicClip, musicFadeTime, true);
        else
            Debug.LogWarning("MusicManager not found. Add a MusicManager to the scene or enable the bootstrap.");
    }

    [ContextMenu("Stop Music")]
    public void StopMusicNow()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.StopMusic(musicFadeTime);
    }

    [ContextMenu("Play SFX")]
    public void PlaySFXNow()
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("No SFX clip assigned.");
            return;
        }
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayOneShot(sfxClip, sfxVolume);
        else
            Debug.LogWarning("SFXManager not found. Add a SFXManager to the scene or enable the bootstrap.");
    }
}

