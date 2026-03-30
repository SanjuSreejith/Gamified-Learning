using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AbelDeathCutscene : MonoBehaviour
{
    [Header("UI (Single Panel)")]
    public CanvasGroup panelCanvas;      // Black panel with text inside
    public TextMeshProUGUI storyText;

    [Header("Settings")]
    public float fadeDuration = 1.5f;
    public float textSpeed = 0.03f;
    public float lineDelay = 1.2f;

    [Header("Scene")]
    public string nextSceneName = "NextScene";

    [TextArea]
    public string[] messages =
    {
        "The attack was unstoppable.",
        "Abel stepped in front of you.",
        "He took the hit.",
        "He didn’t get up.",
        "Silence.",
        "Kuttan changed.",
        "Routing protocol active.",
        "Danger level rising.",
        "RUN."
    };

    private void Start()
    {
        panelCanvas.alpha = 0f; // Ensure panel starts invisible
    }
    public void PlayCutscene()
    {
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        // 🔥 Fade panel in (black)
        yield return StartCoroutine(Fade(0, 1));

        // 🔥 Show text
        for (int i = 0; i < messages.Length; i++)
        {
            yield return StartCoroutine(TypeText(messages[i]));
            yield return new WaitForSecondsRealtime(lineDelay);
        }

        // 🔥 Pause
        yield return new WaitForSecondsRealtime(1f);

        // 🔥 Load next scene
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator Fade(float start, float end)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;

            float smooth = t * t * (3f - 2f * t);
            panelCanvas.alpha = Mathf.Lerp(start, end, smooth);

            yield return null;
        }

        panelCanvas.alpha = end;
    }

    IEnumerator TypeText(string line)
    {
        storyText.text = "";

        foreach (char c in line)
        {
            storyText.text += c;
            yield return new WaitForSecondsRealtime(textSpeed);
        }
    }
}