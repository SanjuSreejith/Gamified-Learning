using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

public class TerminalManger : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    [Tooltip("Sequence of TextMeshProUGUI targets. If set, typing can run across these displays in order.")]
    public TextMeshProUGUI[] textDisplays;
    public TMP_InputField inputField;
    public AudioSource audioSource;
    public AudioClip typingClip;
    public float lettersPerSecond = 30f;
    [Tooltip("If true, the text already present in the TextMeshProUGUI will be typed out on Start. If false, use triggers or call TriggerTyping().")]
    public bool autoTypeOnStart = false;
    [Tooltip("Optional Canvas to watch. When this Canvas becomes active in hierarchy and `startWhenCanvasVisible` is true, typing will start.")]
    public Canvas watchedCanvas;
    [Tooltip("When true, typing will start automatically when `watchedCanvas` becomes visible (active in hierarchy).")]
    public bool startWhenCanvasVisible = false;
    [Tooltip("When true, typing will start on right mouse button down.")]
    public bool startOnRightClick = false;
    [Tooltip("If true and `textDisplays` is set, automatically sequence typing across them on Start.")]
    public bool autoSequenceOnStart = false;
    [Tooltip("Optional per-display texts. If length matches `textDisplays`, these texts will be typed into each display in order. Otherwise the initial text on each display is used.")]
    public string[] textsForDisplays;
    [Tooltip("Delay between finishing one display and starting the next (seconds).")]
    public float delayBetweenDisplays = 0.2f;
    [Tooltip("How long to keep a finished display visible before clearing it and moving to the next (seconds).")]
    public float holdDisplayDuration = 1f;
    public bool caseSensitive = false;
    public string expectedKeyword;
    public UnityEvent onCorrectMatch;
    [Tooltip("Behaviours to disable while typing (e.g. player movement scripts). Their enabled states will be restored when typing stops or finishes.")]
    public Behaviour[] behavioursToDisableDuringTyping;

    private Coroutine typingCoroutine;
    private string currentFullText = "";
    private int currentIndex;
    private bool isTyping;
    private bool inSequence = false;
    private bool prevWatchedCanvasVisible = false;
    private string initialTextCache = "";
    private string[] initialTextCachesArray;
    private bool enableInputOnlyAfterTyping = true;
    private bool originalInputInteractable = true;
    private Dictionary<Behaviour, bool> originalBehaviourStates = new Dictionary<Behaviour, bool>();
    private int sequenceIndex = 0;
    private TextMeshProUGUI currentDisplay;

    void Start()
    {
        if (inputField != null)
            inputField.onEndEdit.AddListener(CheckInput);

        // cache single display or multiple displays
        if (textDisplays != null && textDisplays.Length > 0)
        {
            initialTextCachesArray = new string[textDisplays.Length];
            for (int i = 0; i < textDisplays.Length; i++)
            {
                if (textDisplays[i] != null)
                    initialTextCachesArray[i] = textDisplays[i].text;
                else
                    initialTextCachesArray[i] = "";
            }
        }
        else
        {
            if (textDisplay != null)
                initialTextCache = textDisplay.text;
        }

        if (textDisplays != null && textDisplays.Length > 0)
        {
            if (autoSequenceOnStart)
            {
                // clear all displays then start sequence
                foreach (var d in textDisplays) if (d != null) d.text = "";
                StartTyping(null, typingClip, lettersPerSecond);
            }
        }
        else
        {
            if (autoTypeOnStart && textDisplay != null && !string.IsNullOrEmpty(initialTextCache))
            {
                textDisplay.text = "";
                StartTyping(initialTextCache, typingClip, lettersPerSecond);
            }
        }

        // initialize previous canvas visible state
        if (watchedCanvas != null)
            prevWatchedCanvasVisible = watchedCanvas.gameObject.activeInHierarchy;
    }

    void Update()
    {
        if (startWhenCanvasVisible && watchedCanvas != null)
        {
            bool currentlyVisible = watchedCanvas.gameObject.activeInHierarchy;
            // transitioned from invisible -> visible: restart typing from beginning
            if (currentlyVisible && !prevWatchedCanvasVisible)
            {
                    if (textDisplays != null && textDisplays.Length > 0)
                {
                    // restart sequence from start
                    sequenceIndex = 0;
                    foreach (var d in textDisplays) if (d != null) d.text = "";
                    StartTyping(null, typingClip, lettersPerSecond);
                }
                else
                {
                    string toType = string.IsNullOrEmpty(initialTextCache) ? currentFullText : initialTextCache;
                    if (!string.IsNullOrEmpty(toType))
                    {
                        StopTyping();
                        if (textDisplay != null) textDisplay.text = "";
                        StartTyping(toType, typingClip, lettersPerSecond);
                    }
                }
            }
            // transitioned from visible -> invisible: stop and clear so next visible restarts
            else if (!currentlyVisible && prevWatchedCanvasVisible)
            {
                StopTyping();
                if (textDisplays != null && textDisplays.Length > 0)
                {
                    foreach (var d in textDisplays) if (d != null) d.text = "";
                }
                else
                {
                    if (textDisplay != null) textDisplay.text = "";
                }
            }
            prevWatchedCanvasVisible = currentlyVisible;
        }

        if (startOnRightClick && Input.GetMouseButtonDown(1))
        {
            if (textDisplays != null && textDisplays.Length > 0)
            {
                // start sequence
                sequenceIndex = 0;
                foreach (var d in textDisplays) if (d != null) d.text = "";
                StartTyping(null, typingClip, lettersPerSecond);
            }
            else
            {
                string toType = string.IsNullOrEmpty(currentFullText) ? initialTextCache : currentFullText;
                if (!string.IsNullOrEmpty(toType)) StartTyping(toType, typingClip, lettersPerSecond);
            }
        }
    }

    public void StartTyping(string text, AudioClip clip = null, float lps = -1f)
    {
        // If multiple displays are set, run a sequence across them.
        if (textDisplays != null && textDisplays.Length > 0)
        {
            StopTyping();
            inSequence = true;
            sequenceIndex = Mathf.Clamp(sequenceIndex, 0, textDisplays.Length - 1);
            // prepare texts
            string[] textsToUse = null;
            if (textsForDisplays != null && textsForDisplays.Length >= textDisplays.Length)
                textsToUse = textsForDisplays;
            else if (initialTextCachesArray != null && initialTextCachesArray.Length == textDisplays.Length)
                textsToUse = initialTextCachesArray;
            else
            {
                textsToUse = new string[textDisplays.Length];
                for (int i = 0; i < textsToUse.Length; i++) textsToUse[i] = "";
            }
            // override starting index text if text param provided
            if (!string.IsNullOrEmpty(text)) textsToUse[sequenceIndex] = text;

            CacheAndDisableBehaviours();
            // disable input interaction until sequence is complete if requested
            if (enableInputOnlyAfterTyping && inputField != null)
            {
                originalInputInteractable = inputField.interactable;
                inputField.interactable = false;
            }
            if (clip != null) typingClip = clip;
            if (lps > 0) lettersPerSecond = lps;
            typingCoroutine = StartCoroutine(SequenceRoutine(textsToUse));
            return;
        }

        if (textDisplay == null) return;
        StopTyping();
        currentFullText = text ?? "";
        CacheAndDisableBehaviours();
        if (clip != null) typingClip = clip;
        if (lps > 0) lettersPerSecond = lps;
        typingCoroutine = StartCoroutine(TypeRoutine());
    }

    IEnumerator SequenceRoutine(string[] textsToUse)
    {
        isTyping = true;
        for (; sequenceIndex < textDisplays.Length; sequenceIndex++)
        {
            currentDisplay = textDisplays[sequenceIndex];
            string toType = (textsToUse != null && sequenceIndex < textsToUse.Length) ? textsToUse[sequenceIndex] : "";
            if (currentDisplay == null) continue;
            currentDisplay.text = "";
            int localIndex = 0;
            float delay = 1f / Mathf.Max(1f, lettersPerSecond);
            while (localIndex < toType.Length)
            {
                currentDisplay.text += toType[localIndex];
                localIndex++;
                if (audioSource != null && typingClip != null)
                {
                    audioSource.pitch = 1f + Random.Range(-0.05f, 0.05f);
                    audioSource.PlayOneShot(typingClip);
                }
                yield return new WaitForSeconds(delay);
            }
            // keep the finished text visible for a bit, then clear and wait before next
            yield return new WaitForSeconds(holdDisplayDuration);
            if (currentDisplay != null) currentDisplay.text = "";
            yield return new WaitForSeconds(delayBetweenDisplays);
        }
        isTyping = false;
        inSequence = false;
        // restore input interactivity
        if (enableInputOnlyAfterTyping && inputField != null)
            inputField.interactable = originalInputInteractable;
        RestoreBehaviours();
    }

    /// <summary>
    /// Public method other scripts can call to trigger typing now.
    /// </summary>
    public void TriggerTyping(string overrideText = null)
    {
        string toType = overrideText ?? (string.IsNullOrEmpty(currentFullText) ? initialTextCache : currentFullText);
        if (!string.IsNullOrEmpty(toType)) StartTyping(toType, typingClip, lettersPerSecond);
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
        RestoreBehaviours();
    }

    IEnumerator TypeRoutine()
    {
        isTyping = true;
        textDisplay.text = "";
        currentIndex = 0;
        float delay = 1f / Mathf.Max(1f, lettersPerSecond);
        while (currentIndex < currentFullText.Length)
        {
            textDisplay.text += currentFullText[currentIndex];
            currentIndex++;
            if (audioSource != null && typingClip != null)
            {
                audioSource.pitch = 1f + Random.Range(-0.05f, 0.05f);
                audioSource.PlayOneShot(typingClip);
            }
            yield return new WaitForSeconds(delay);
        }
        isTyping = false;
        // restore input interactivity if single display typing
        if (enableInputOnlyAfterTyping && inputField != null)
            inputField.interactable = originalInputInteractable;
        RestoreBehaviours();
    }

    public void CheckInput(string userInput)
    {
        if (string.IsNullOrEmpty(expectedKeyword)) return;
        if (userInput == null) userInput = "";
        var a = caseSensitive ? userInput.Trim() : userInput.Trim().ToLowerInvariant();
        var b = caseSensitive ? expectedKeyword.Trim() : expectedKeyword.Trim().ToLowerInvariant();
        if (a == b) onCorrectMatch?.Invoke();
    }

    public void SetTextImmediate(string text)
    {
        StopTyping();
        currentFullText = text ?? "";
        if (textDisplay != null) textDisplay.text = currentFullText;
    }

    public bool IsTyping() { return isTyping; }

    void OnDestroy()
    {
        if (inputField != null)
            inputField.onEndEdit.RemoveListener(CheckInput);
        RestoreBehaviours();
    }

    void CacheAndDisableBehaviours()
    {
        originalBehaviourStates.Clear();
        if (behavioursToDisableDuringTyping == null) return;
        foreach (var b in behavioursToDisableDuringTyping)
        {
            if (b == null) continue;
            if (!originalBehaviourStates.ContainsKey(b))
                originalBehaviourStates[b] = b.enabled;
            b.enabled = false;
        }
    }

    void RestoreBehaviours()
    {
        if (originalBehaviourStates == null) return;
        foreach (var kv in originalBehaviourStates)
        {
            var b = kv.Key;
            if (b == null) continue;
            b.enabled = kv.Value;
        }
        originalBehaviourStates.Clear();
    }
}
