using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Tooltip("Number of pooled AudioSources created when the manager is instantiated.")]
    public int poolSize = 8;

    private List<AudioSource> pool;
    private float sfxVolume = 1f;

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

        pool = new List<AudioSource>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            pool.Add(src);
        }

        sfxVolume = PlayerPrefs.GetFloat("sfxVolume", 1f);
        foreach (var s in pool) s.volume = sfxVolume;
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        var src = GetFreeSource();
        src.volume = Mathf.Clamp01(volume) * sfxVolume;
        src.clip = clip;
        src.loop = false;
        src.Play();
    }

    private AudioSource GetFreeSource()
    {
        foreach (var s in pool) if (!s.isPlaying) return s;
        return pool[0];
    }

    public void SetVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        foreach (var s in pool) if (!s.isPlaying) s.volume = sfxVolume;
        PlayerPrefs.SetFloat("sfxVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetVolume() => sfxVolume;
}
