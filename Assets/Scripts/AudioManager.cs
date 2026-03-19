using UnityEngine;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance;

    const string VOLUME_KEY = "GameVolume";

    void Awake()
    {
        // Singleton so it survives scene loads
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        AudioListener.volume = savedVolume;
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    public float GetVolume()
    {
        return PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
    }
}