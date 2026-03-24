using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTypewriter : MonoBehaviour
{
    [Tooltip("Default delay (used if no saved value)")]
    public float letterDelay = 0.035f;

    private const string PREF_KEY = "TypeSpeed";

    TextMeshProUGUI tmp;
    Coroutine typingRoutine;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        LoadSpeed();
    }

    void OnDisable()
    {
        // 🔥 Prevent coroutine errors when object disables
        StopTyping();
    }

    void LoadSpeed()
    {
        if (PlayerPrefs.HasKey(PREF_KEY))
        {
            letterDelay = PlayerPrefs.GetFloat(PREF_KEY);
        }
    }

    /// <summary>
    /// Plays typing animation safely
    /// </summary>
    public void Play(string text)
    {
        // 🔥 Safety check
        if (!isActiveAndEnabled || tmp == null)
        {
            // fallback → just set text instantly
            if (tmp != null)
                tmp.text = text;

            return;
        }

        LoadSpeed();

        StopTyping();

        tmp.text = text;
        tmp.maxVisibleCharacters = 0;
        tmp.ForceMeshUpdate();

        // 🔥 Extra safety: only start coroutine if active
        if (gameObject.activeInHierarchy)
        {
            typingRoutine = StartCoroutine(TypeRoutine());
        }
    }

    /// <summary>
    /// Instantly finishes typing
    /// </summary>
    public void Skip()
    {
        if (tmp == null) return;

        tmp.maxVisibleCharacters = tmp.textInfo.characterCount;
        StopTyping();
    }

    public bool IsTyping()
    {
        return typingRoutine != null;
    }

    void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }

    IEnumerator TypeRoutine()
    {
        // 🔥 Wait one frame safely
        yield return null;

        if (!isActiveAndEnabled || tmp == null)
            yield break;

        int totalCharacters = tmp.textInfo.characterCount;

        for (int i = 1; i <= totalCharacters; i++)
        {
            // 🔥 Stop if object disabled mid-typing
            if (!isActiveAndEnabled)
                yield break;

            tmp.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(letterDelay);
        }

        typingRoutine = null;
    }
}