using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTypewriter : MonoBehaviour
{
    [Tooltip("Delay between each visible character (seconds)")]
    public float letterDelay = 0.035f;

    TextMeshProUGUI tmp;
    Coroutine typingRoutine;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Plays typing animation by HIDING full text
    /// and UNHIDING characters one by one
    /// </summary>
    public void Play(string text)
    {
        StopTyping();

        tmp.text = text;                 // 1️⃣ Set full text ONCE
        tmp.maxVisibleCharacters = 0;    // 2️⃣ Hide everything
        tmp.ForceMeshUpdate();           // 3️⃣ Force TMP to calculate chars

        typingRoutine = StartCoroutine(TypeRoutine());
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
        // 🔑 REQUIRED: wait 1 frame so TMP updates textInfo
        yield return null;

        int totalCharacters = tmp.textInfo.characterCount;

        for (int i = 1; i <= totalCharacters; i++)
        {
            tmp.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(letterDelay);
        }

        typingRoutine = null;
    }
}