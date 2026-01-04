using UnityEngine;

[HelpURL("https://example.com/docs/audio-bootstrap")]
public class MusicManger : MonoBehaviour
{
    [Header("Auto-create managers at runtime")]
    [Tooltip("If true the MusicManager will be created if none exists.")]
    public bool createMusicManager = true;
    [Tooltip("If true the SFXManager will be created if none exists.")]
    public bool createSFXManager = true;
    [Tooltip("If true the managers will be added to this GameObject. Otherwise they will be created as separate persistent objects.")]
    public bool attachToThisGameObject = true;

    [Header("SFX settings (applies when creating SFXManager)")]
    public int sfxPoolSize = 8;

    [Header("Music settings (applies when creating MusicManager)")]
    public float musicFadeTime = 1f;

    private void Awake()
    {
        if (createMusicManager && MusicManager.Instance == null)
        {
            if (attachToThisGameObject)
            {
                var mm = gameObject.GetComponent<MusicManager>();
                if (mm == null) mm = gameObject.AddComponent<MusicManager>();
                mm.defaultFadeTime = musicFadeTime;
            }
            else
            {
                var go = new GameObject("MusicManager");
                DontDestroyOnLoad(go);
                var mm = go.AddComponent<MusicManager>();
                mm.defaultFadeTime = musicFadeTime;
            }
        }

        if (createSFXManager && SFXManager.Instance == null)
        {
            if (attachToThisGameObject)
            {
                var sm = gameObject.GetComponent<SFXManager>();
                if (sm == null) sm = gameObject.AddComponent<SFXManager>();
                sm.poolSize = sfxPoolSize;
            }
            else
            {
                var go = new GameObject("SFXManager");
                DontDestroyOnLoad(go);
                var sm = go.AddComponent<SFXManager>();
                sm.poolSize = sfxPoolSize;
            }
        }
    }
}
