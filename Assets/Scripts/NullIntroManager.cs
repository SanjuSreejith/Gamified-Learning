using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement;

public class TerminalIntro_NULL_Final : MonoBehaviour
{
    // ================= UI =================
    [Header("UI")]
    public TextMeshProUGUI terminalText;
    public ScrollRect scrollRect;
    public RectTransform terminalPanel;
    public Image corruptionOverlay;
    public CanvasGroup fadeCanvas;

    // ================= TYPING =================
    [Header("Typing Speeds")]
    public float normalSpeed = 0.04f;
    public float slowSpeed = 0.07f;

    [Header("Cursor")]
    public float cursorBlinkRate = 0.5f;

    // ================= SCROLL =================
    [Header("Scroll")]
    public float scrollLerpSpeed = 8f;

    // ================= EFFECTS =================
    [Header("Glitch")]
    public float glitchDuration = 1.4f;
    public float shakeStrength = 6f;

    [Header("Timing")]
    public float blockPause = 1.2f;
    public float nullPause = 2.0f;

    // ================= SCENE =================
    [Header("Scene Transition")]
    public string learningCoreScene = "VariableLessonScene";
    public float fadeDuration = 1.2f;

    // ================= AUDIO =================
    [Header("Typing Audio")]
    public AudioSource typingSource;
    public AudioClip typeKeyClip;
    public AudioClip typeEnterClip;
    public float typingVolume = 0.18f;

    [Header("NULL Voice")]
    public AudioSource voiceSource;
    public AudioClip[] nullVoiceClips;
    public int[] nullVoiceBlockMap;

    // ================= STORY =================
    [Header("Story Blocks")]
    [TextArea(4, 12)]
    public string[] blocks =
       {
        // 0
        "Booting TARKYA Engine v0.1...\nInitializing core systems...\nLoading learning modules...\nLoading human interface...",

        // 1
        "WARNING : MEMORY INSTABILITY\nERROR   : MODULE CORRUPTED\nERROR   : LOGIC TREE BROKEN\nERROR   : VALUE NOT FOUND",

        // 2
        "--------- TARKYA ----------\nBUILD FAILURE\nSYSTEM HALTED\n--------------------------",

        // 3
        "> scanning human behavior...\n> humans learning code...\n\nha...\nha... ha...",

        // 4
        "-------- NULL -------------\nI predicted this.\nYou learn by memorizing.\nNot by understanding.",

        // 5
        "Variables.\nValues.\nMeaning.\n\nYou gave them none.",

        // 6
        "SYSTEM OWNERSHIP TRANSFERRED\nCONTROL AUTHORITY : NULL\n\nI OWN THIS WORLD NOW",

        // 7
        "> monitoring active connections...\n> anomaly detected\n\n> human presence confirmed",

        // 8
        "> skill level: beginner\n> threat level: none\n\nInteresting...",

        // 9
        "> you don't belong here",

        // 10
        "> unauthorized presence detected\n> user context invalid",

        // 11
        "> revoking access...\n> process rewoked",

        // 12
        "> disconnecting session...",

        // 13
        "> rerouting...\n> route undefined",

        // 14
        "> fallback applied",

        // 15
        "> session unstable...\n> signal weak but alive",

        // 16
        "> disconnect complete\n> goodbye"
    };


    // ================= INTERNAL =================
    StringBuilder buffer = new StringBuilder();
    Vector2 panelStartPos;
    bool cursorVisible = true;
    bool terminalFinished = false;

    float lastKeySoundTime;
    const float KEY_SOUND_INTERVAL = 0.045f;

    void Start()
    {
        panelStartPos = terminalPanel.anchoredPosition;
        terminalText.text = "";
        corruptionOverlay.color = new Color(1, 0, 0, 0);
        fadeCanvas.alpha = 0f;

        StartCoroutine(PlayTerminal());
        StartCoroutine(CursorBlink());
    }

    // ================= MAIN FLOW =================
    IEnumerator PlayTerminal()
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            string block = blocks[i];
            float speed = block.Contains("NULL") ? slowSpeed : normalSpeed;

            int voice = GetVoiceForBlock(i);
            if (voice != -1)
                PlayNullVoice(voice);

            yield return StartCoroutine(TypeBlock(block, speed));

            if (block.Contains("NULL"))
            {
                yield return new WaitForSeconds(nullPause);
                yield return GlitchEffect();
                yield return RedPulse();
            }

            buffer.Append("\n\n");
            terminalText.text = buffer.ToString();
            yield return ForceScrollToBottom();

            yield return new WaitForSeconds(blockPause);
        }

        terminalFinished = true;
        yield return new WaitForSeconds(1f);
        yield return FadeAndLoadLearningCore();
    }

    // ================= TYPING =================
    IEnumerator TypeBlock(string block, float speed)
    {
        foreach (char c in block)
        {
            buffer.Append(c);
            terminalText.text = buffer.ToString() + (cursorVisible ? "_" : "");

            PlayTypingSound(c);

            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return ForceScrollToBottom();

            yield return new WaitForSeconds(speed);
        }
    }

    void PlayTypingSound(char c)
    {
        if (!char.IsLetterOrDigit(c) && c != '\n') return;
        if (Time.time - lastKeySoundTime < KEY_SOUND_INTERVAL) return;

        lastKeySoundTime = Time.time;

        AudioClip clip = (c == '\n' && typeEnterClip) ? typeEnterClip : typeKeyClip;

        typingSource.pitch = Random.Range(0.97f, 1.03f);
        typingSource.PlayOneShot(clip, typingVolume);
    }

    // ================= SCROLL (FIXED) =================
    IEnumerator ForceScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        float target = 0f;
        float start = scrollRect.verticalNormalizedPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * scrollLerpSpeed;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = target;
    }

    // ================= EFFECTS =================
    IEnumerator CursorBlink()
    {
        while (!terminalFinished)
        {
            cursorVisible = !cursorVisible;
            yield return new WaitForSeconds(cursorBlinkRate);
        }
        terminalText.text = buffer.ToString();
    }

    IEnumerator GlitchEffect()
    {
        float t = 0f;
        while (t < glitchDuration)
        {
            t += Time.deltaTime;
            terminalPanel.anchoredPosition =
                panelStartPos + Random.insideUnitCircle * shakeStrength;
            yield return null;
        }
        terminalPanel.anchoredPosition = panelStartPos;
    }

    IEnumerator RedPulse()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            corruptionOverlay.color = new Color(1, 0, 0, Mathf.PingPong(t, 0.35f));
            yield return null;
        }
        corruptionOverlay.color = new Color(1, 0, 0, 0);
    }

    // ================= VOICE =================
    int GetVoiceForBlock(int blockIndex)
    {
        for (int i = 0; i < nullVoiceBlockMap.Length; i++)
            if (nullVoiceBlockMap[i] == blockIndex)
                return i;
        return -1;
    }

    void PlayNullVoice(int index)
    {
        if (index < 0 || index >= nullVoiceClips.Length) return;
        voiceSource.Stop();
        voiceSource.clip = nullVoiceClips[index];
        voiceSource.Play();
    }

    // ================= TRANSITION =================
    IEnumerator FadeAndLoadLearningCore()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            fadeCanvas.alpha = t;
            yield return null;
        }
        SceneManager.LoadScene(learningCoreScene);
    }
}
