using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement;

public class TerminalIntro : MonoBehaviour
{
    // ================= UI =================
    [Header("UI")]
    public TextMeshProUGUI terminalText;
    public ScrollRect scrollRect;
    public RectTransform terminalPanel;
    public Image corruptionOverlay;
    public CanvasGroup fadeCanvas;

    // ================= TYPING =================
    [Header("Typing")]
    public float normalSpeed = 0.04f;
    public float slowSpeed = 0.07f;
    public float cursorBlinkRate = 0.5f;
    [Range(0.1f, 2f)] public float typingSpeedMultiplier = 1f;

    // ================= CREDITS SCROLL =================
    [Header("Credits Scroll")]
    [Tooltip("Base scroll speed")]
    public float baseScrollSpeed = 15f;
    [Tooltip("Speed boost when viewport is full")]
    public float boostScrollSpeed = 60f;
    [Tooltip("Viewport fill percentage to trigger auto-scroll")]
    [Range(0.5f, 0.95f)] public float autoScrollThreshold = 0.85f;
    [Tooltip("Smooth time for scroll speed changes")]
    public float scrollSmoothTime = 0.3f;

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
    public AudioClip typeSoundClip; // Single type sound
    public AudioClip typeSpaceClip;
    public AudioClip typeEnterClip;
    [Range(0f, 1f)] public float typingVolume = 0.16f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.85f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.15f;

    [Header("Typing Sound Profile")]
    [Range(0f, 0.2f)] public float minVolumeVariation = 0.9f;
    [Range(0f, 0.2f)] public float maxVolumeVariation = 1.1f;
    public float letterSoundInterval = 0.04f;
    public float spaceSoundInterval = 0.06f;
    public float enterSoundInterval = 0.1f;

    [Header("NULL Voice")]
    public AudioSource voiceSource;
    public AudioClip[] nullVoiceClips;
    public int[] nullVoiceBlockMap;

    // ================= STORY =================
    [Header("Story Blocks")]
    [TextArea(4, 12)]
    public string[] blocks;

    // ================= INTERNAL =================
    private StringBuilder buffer = new StringBuilder();
    private bool cursorVisible = true;
    private bool terminalFinished = false;
    private bool isTyping = false;

    private RectTransform content;
    private float viewportHeight;
    private float contentHeight;

    // Scrolling
    private float currentScrollSpeed;
    private float targetScrollSpeed;
    private float scrollVelocity;
    private bool needsAutoScroll = false;

    // Typing sound management
    private float lastSoundTime = 0f;
    private int sameCharCount = 0;
    private char lastChar = '\0';

    // ================= START =================
    void Start()
    {
        content = scrollRect.content;
        viewportHeight = scrollRect.viewport.rect.height;

        terminalText.text = "";
        buffer.Clear();

        corruptionOverlay.color = new Color(1, 0, 0, 0);
        fadeCanvas.alpha = 0f;

        // Start content BELOW the viewport
        Vector2 startPos = content.anchoredPosition;
        startPos.y = -viewportHeight;
        content.anchoredPosition = startPos;

        currentScrollSpeed = baseScrollSpeed;
        targetScrollSpeed = baseScrollSpeed;

        StartCoroutine(PlayTerminal());
        StartCoroutine(CursorBlink());
    }

    // ================= MAIN FLOW =================
    IEnumerator PlayTerminal()
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            string block = blocks[i];
            float speed = (block.Contains("NULL") ? slowSpeed : normalSpeed) / typingSpeedMultiplier;

            int voice = GetVoiceForBlock(i);
            if (voice != -1)
                PlayNullVoice(voice);

            // Type this block
            yield return StartCoroutine(TypeBlock(block, speed));

            // Check viewport fill and auto-scroll if needed
            CheckAndAutoScroll();

            // Pause after block
            if (block.Contains("NULL"))
            {
                yield return new WaitForSeconds(nullPause / 2f);
                yield return GlitchEffect();
                yield return RedPulse();
                yield return new WaitForSeconds(nullPause / 2f);
            }
            else
            {
                yield return new WaitForSeconds(blockPause);
            }
        }

        terminalFinished = true;
        yield return new WaitForSeconds(1.2f);
        yield return FadeAndLoadLearningCore();
    }

    // ================= TYPING =================
    IEnumerator TypeBlock(string block, float speed)
    {
        isTyping = true;

        foreach (char c in block)
        {
            buffer.Append(c);

            // Update text and cursor
            terminalText.text = buffer.ToString() + (cursorVisible ? "_" : "");

            // Play typing sound with character-aware variations
            PlayTypingSound(c);

            // Check if we need to auto-scroll while typing
            if (CheckViewportFill() > autoScrollThreshold)
            {
                needsAutoScroll = true;
            }

            yield return new WaitForSeconds(speed);
        }

        isTyping = false;
        // Add line breaks after block
        buffer.Append("\n\n");
        terminalText.text = buffer.ToString();

        // Reset same character counter at end of block
        sameCharCount = 0;
    }

    // ================= IMPROVED TYPING SOUNDS =================
    void PlayTypingSound(char c)
    {
        if (typeSoundClip == null) return;

        float timeSinceLastSound = Time.time - lastSoundTime;
        float requiredInterval = letterSoundInterval;

        // Determine sound characteristics based on character
        float pitch = Random.Range(minPitch, maxPitch);
        float volume = typingVolume * Random.Range(minVolumeVariation, maxVolumeVariation);

        // Character-specific adjustments
        if (c == '\n' && typeEnterClip != null)
        {
            // Enter/return key
            typingSource.pitch = Random.Range(0.95f, 1.05f);
            typingSource.PlayOneShot(typeEnterClip, volume * 1.2f);
            requiredInterval = enterSoundInterval;
        }
        else if (c == ' ')
        {
            // Space bar
            if (typeSpaceClip != null)
            {
                typingSource.pitch = Random.Range(0.88f, 0.98f);
                typingSource.PlayOneShot(typeSpaceClip, volume * 0.8f);
            }
            else if (typeSoundClip != null)
            {
                typingSource.pitch = Random.Range(0.85f, 0.95f);
                typingSource.PlayOneShot(typeSoundClip, volume * 0.7f);
            }
            requiredInterval = spaceSoundInterval;
            sameCharCount = 0; // Reset on space
        }
        else if (char.IsLetterOrDigit(c) || c == '.' || c == ',' || c == '!' || c == '?')
        {
            // Letters, digits, and common punctuation
            if (timeSinceLastSound >= requiredInterval)
            {
                // Detect repeated characters
                if (c == lastChar)
                {
                    sameCharCount++;
                    // Adjust pitch slightly for repeated characters
                    pitch += sameCharCount * 0.03f;
                    // Lower volume for rapid repeats
                    volume *= Mathf.Clamp01(1f - (sameCharCount * 0.1f));
                }
                else
                {
                    sameCharCount = 0;

                    // Different pitch ranges for different character types
                    if (char.IsUpper(c))
                    {
                        pitch = Random.Range(1.05f, 1.15f); // Higher pitch for caps
                        volume *= 1.1f;
                    }
                    else if (char.IsDigit(c))
                    {
                        pitch = Random.Range(0.9f, 1.0f); // Slightly lower for numbers
                    }
                    else if ("aeiou".Contains(c.ToString().ToLower()))
                    {
                        // Vowels get slight variation
                        pitch += Random.Range(-0.02f, 0.02f);
                    }
                    else
                    {
                        // Consonants
                        pitch += Random.Range(-0.01f, 0.01f);
                    }

                    // Punctuation adjustments
                    if (c == '.' || c == '!' || c == '?')
                    {
                        volume *= 0.9f;
                        pitch = Random.Range(0.95f, 1.05f);
                    }
                    else if (c == ',')
                    {
                        volume *= 0.8f;
                        pitch = Random.Range(1.0f, 1.1f);
                    }
                }

                lastChar = c;
                typingSource.pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                typingSource.PlayOneShot(typeSoundClip, Mathf.Clamp01(volume));
                lastSoundTime = Time.time;
            }
        }
    }

    // ================= SCROLLING SYSTEM =================
    void Update()
    {
        // Check viewport fill continuously
        float fillPercentage = CheckViewportFill();

        // Determine target scroll speed
        if (needsAutoScroll)
        {
            // Boost speed when viewport is almost full
            targetScrollSpeed = boostScrollSpeed;

            // Check if we've scrolled enough
            float textBottom = content.anchoredPosition.y + terminalText.preferredHeight;
            float viewportBottom = content.anchoredPosition.y + viewportHeight;

            if (textBottom < viewportBottom * 0.7f) // When text is 70% up in viewport
            {
                needsAutoScroll = false;
            }
        }
        else
        {
            // Normal scrolling
            targetScrollSpeed = baseScrollSpeed;

            // Check if we need to start auto-scrolling
            if (fillPercentage > autoScrollThreshold && isTyping)
            {
                needsAutoScroll = true;
            }
        }

        // Smoothly adjust scroll speed
        currentScrollSpeed = Mathf.SmoothDamp(currentScrollSpeed, targetScrollSpeed,
            ref scrollVelocity, scrollSmoothTime);

        // Apply scrolling
        Vector2 pos = content.anchoredPosition;
        pos.y += currentScrollSpeed * Time.deltaTime;
        content.anchoredPosition = pos;
    }

    // Check how full the viewport is with text
    float CheckViewportFill()
    {
        // Force layout update to get accurate text height
        Canvas.ForceUpdateCanvases();

        // Calculate visible text ratio
        float textBottom = content.anchoredPosition.y + terminalText.preferredHeight;
        float viewportBottom = content.anchoredPosition.y + viewportHeight;

        // How much text is visible compared to viewport height
        float visibleTextHeight = Mathf.Min(terminalText.preferredHeight, viewportBottom - content.anchoredPosition.y);

        return visibleTextHeight / viewportHeight;
    }

    void CheckAndAutoScroll()
    {
        float fillPercentage = CheckViewportFill();

        // If viewport is more than threshold full, trigger auto-scroll
        if (fillPercentage > autoScrollThreshold)
        {
            needsAutoScroll = true;
        }
    }

    // ================= EFFECTS =================
    IEnumerator CursorBlink()
    {
        while (!terminalFinished)
        {
            cursorVisible = !cursorVisible;

            // Update text with or without cursor
            terminalText.text = buffer.ToString() + (cursorVisible ? "_" : "");

            yield return new WaitForSeconds(cursorBlinkRate);
        }

        terminalText.text = buffer.ToString();
    }

    IEnumerator GlitchEffect()
    {
        Vector2 startPos = terminalPanel.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < glitchDuration)
        {
            elapsed += Time.deltaTime;

            float intensity = Mathf.Clamp01(1f - (elapsed / glitchDuration));
            float currentShake = shakeStrength * intensity;

            terminalPanel.anchoredPosition = startPos + Random.insideUnitCircle * currentShake;

            yield return null;
        }

        terminalPanel.anchoredPosition = startPos;
    }

    IEnumerator RedPulse()
    {
        float elapsed = 0f;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * 2f;

            float alpha = Mathf.Sin(elapsed * Mathf.PI) * 0.35f;
            corruptionOverlay.color = new Color(1, 0, 0, alpha);

            yield return null;
        }

        corruptionOverlay.color = new Color(1, 0, 0, 0);
    }

    // ================= VOICE =================
    int GetVoiceForBlock(int blockIndex)
    {
        for (int i = 0; i < nullVoiceBlockMap.Length; i++)
        {
            if (nullVoiceBlockMap[i] == blockIndex)
                return i;
        }
        return -1;
    }

    void PlayNullVoice(int index)
    {
        if (index < 0 || index >= nullVoiceClips.Length) return;

        voiceSource.clip = nullVoiceClips[index];
        voiceSource.Play();
    }

    // ================= TRANSITION =================
    IEnumerator FadeAndLoadLearningCore()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Clamp01(elapsed / fadeDuration);

            // Slow down scrolling during fade
            targetScrollSpeed = Mathf.Lerp(baseScrollSpeed, 0f, elapsed / fadeDuration);

            yield return null;
        }

        SceneManager.LoadScene(learningCoreScene);
    }
}