using UnityEngine;
using TMPro;
using System.Collections;

public class IntroTypewriterUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup panel;
    public TMPTypewriter typewriter;
    public TextMeshProUGUI speakerNameText;

    [Header("Text")]
    public string speakerName = "???";
    [TextArea]
    public string introText = "Where are you...";

    [Header("Timing")]
    public float delayBeforeStart = 0.5f;
    public float fadeInDuration = 0.6f;
    public float stayAfterTyping = 1.5f;
    public float fadeOutDuration = 0.8f;

    [Header("Distance Effect")]
    public float startScale = 0.8f;   // far
    public float endScale = 1.05f;    // closer
    public float scaleSpeed = 1.5f;

    RectTransform panelRect;

    void Start()
    {
        panelRect = panel.GetComponent<RectTransform>();

        panel.alpha = 0f;
        panel.gameObject.SetActive(false);

        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(delayBeforeStart);

        panel.gameObject.SetActive(true);

        // Set speaker
        speakerNameText.text = speakerName;

        // Start from "far"
        panelRect.localScale = Vector3.one * startScale;

        // Fade IN
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // Start scaling (distance feel)
        StartCoroutine(ScaleEffect());

        // Play typing
        typewriter.Play(introText);

        // Wait for typing
        while (typewriter.IsTyping())
            yield return null;

        yield return new WaitForSeconds(stayAfterTyping);

        // Fade OUT
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        panel.gameObject.SetActive(false);
    }

    IEnumerator Fade(float start, float end, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            panel.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        panel.alpha = end;
    }

    IEnumerator ScaleEffect()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;

            float scale = Mathf.Lerp(startScale, endScale, t);
            panelRect.localScale = Vector3.one * scale;

            yield return null;
        }
    }
}