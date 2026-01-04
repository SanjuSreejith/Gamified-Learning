using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;
    [Tooltip("Default crossfade time in seconds.")]
    public float defaultFadeTime = 1f;

    private int activeIndex = 0;
    private float musicVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sourceA == null) sourceA = gameObject.AddComponent<AudioSource>();
        if (sourceB == null) sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.loop = true;
        sourceB.loop = true;
        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;

        musicVolume = PlayerPrefs.GetFloat("musicVolume", 1f);
        sourceA.volume = musicVolume;
        sourceB.volume = 0f;
    }

    public void PlayMusic(AudioClip clip, float fadeTime = -1f, bool loop = true)
    {
        if (clip == null) return;
        if (fadeTime < 0f) fadeTime = defaultFadeTime;
        StartCoroutine(CrossfadeTo(clip, fadeTime, loop));
    }

    private IEnumerator CrossfadeTo(AudioClip clip, float fadeTime, bool loop)
    {
        AudioSource incoming = (activeIndex == 0) ? sourceB : sourceA;
        AudioSource outgoing = (activeIndex == 0) ? sourceA : sourceB;

        incoming.clip = clip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.Play();

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeTime);
            incoming.volume = Mathf.Lerp(0f, musicVolume, p);
            outgoing.volume = Mathf.Lerp(musicVolume, 0f, p);
            yield return null;
        }

        incoming.volume = musicVolume;
        outgoing.volume = 0f;
        outgoing.Stop();
        activeIndex = 1 - activeIndex;
    }

    public void StopMusic(float fadeTime = -1f)
    {
        if (fadeTime < 0f) fadeTime = defaultFadeTime;
        StartCoroutine(StopCoroutine(fadeTime));
    }

    private IEnumerator StopCoroutine(float fadeTime)
    {
        AudioSource outgoing = (activeIndex == 0) ? sourceA : sourceB;
        float startVol = outgoing.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            outgoing.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        outgoing.Stop();
    }

    public void SetVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (sourceA.isPlaying) sourceA.volume = musicVolume;
        if (sourceB.isPlaying) sourceB.volume = musicVolume;
        PlayerPrefs.SetFloat("musicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public float GetVolume() => musicVolume;
}
