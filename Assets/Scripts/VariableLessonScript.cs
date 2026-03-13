using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TerminalVariableLesson : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI terminalText;
    public TextMeshProUGUI dialogueText;
    public Image botFaceImage;

    [Header("Bot Faces")]
    public Sprite idleFace;
    public Sprite happyFace;
    public Sprite thinkingFace;
    public Sprite warningFace;
    public Sprite proudFace;

    [Header("Terminal Settings")]
    public float cursorBlinkRate = 0.5f;
    public float systemLineDelay = 0.25f;

    [Header("Dialogue Settings")]
    public float dialogueSpeed = 0.035f;

    [Header("Dialogue Control (DEV OPTION)")]
    public bool autoAdvanceDialogue = true;      // kept for compatibility, but now overridden by Enter wait
    public KeyCode advanceKey = KeyCode.Return;

    [Header("ID Card UI")]
    public GameObject idCardPanel;
    public TextMeshProUGUI idNameText;
    public TextMeshProUGUI idAgeText;
    public Animator idCardAnimator;

    [Header("Typing Audio")]
    public AudioSource typingAudio;
    public AudioClip typeLetter;
    public AudioClip typeSpace;
    public AudioClip typeBackspace;

    [Header("Exercise")]
    public TerminalVariableExercise exerciseScript;

    // --- NEW: Hint System ---
    [Header("Hint System")]
    public BotHintSystem hintSystem;   // Reference to the hint UI

    string currentInput = "";
    bool inputEnabled;
    bool cursorVisible = true;
    Coroutine cursorRoutine;

    string playerName;
    int playerAge;

    int step = 0;           // 1=name, 2=age, 3=confirmation, etc.

    bool waitingForAdvance;
    bool skipRequested;

    bool waitingForConfirmation;
    bool waitingForCorrectionChoice;

    void Start()
    {
        terminalText.text = "";
        dialogueText.text = "";
        SetFace(idleFace);
        StartCoroutine(TerminalBoot());
    }

    void Update()
    {
        HandleDialogueAdvance();

        if (!inputEnabled) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b' && currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                PlayTypingSound(c);
            }
            else if (c == '\n' || c == '\r')
            {
                SubmitInput();
            }
            else
            {
                bool accepted = false;

                if (step == 1 && (char.IsLetterOrDigit(c) || c == '_'))
                    accepted = true;
                else if (step == 2 && char.IsDigit(c))
                    accepted = true;
                else if (step == 3 && char.IsLetter(c))
                    accepted = true;

                if (accepted)
                {
                    currentInput += c;
                    PlayTypingSound(c);
                }
            }
        }

        RefreshInputLine();
    }

    // ================= BOOT =================
    IEnumerator TerminalBoot()
    {
        yield return AddSystemLine(">>> MEMORY OS v0.1 <<<");
        yield return AddSystemLine("Booting core modules...");
        yield return AddSystemLine("Python runtime active");
        yield return AddSystemLine("----------------------------");

        SetFace(thinkingFace);
        yield return Speak("That was close…");
        yield return Speak("He almost noticed you.");

        yield return Speak("The terminal you just saw…");
        yield return Speak("That world belongs to NULL now.");

        yield return Speak("I can’t exist there.");
        yield return Speak("He controls everything in that space.");

        yield return Speak("This place is different.");
        yield return Speak("It’s a learning core.");

        yield return Speak("As long as you’re learning…");
        yield return Speak("NULL can’t see you.");

        yield return Speak("…Okay.");
        yield return Speak("You’re safe now.");

        yield return Speak("Oh… hey.");
        yield return Speak("I don’t get visitors often.");
        yield return Speak("But I’m glad you’re here.");
        yield return Speak("Let’s start simple.");

        SetFace(idleFace);
        yield return Speak("What’s your name?");

        // --- NEW: Set first hint ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Enter your name (letters, digits, underscore allowed)" });

        EnableInput();
        step = 1;
    }

    // ================= INPUT =================
    void SubmitInput()
    {
        if (string.IsNullOrWhiteSpace(currentInput)) return;
        AppendLine($"> {currentInput}");

        if (step == 1)
        {
            playerName = currentInput;
            DisableInput();
            StartCoroutine(HandleName());
        }
        else if (step == 2)
        {
            if (!int.TryParse(currentInput, out playerAge))
            {
                SetFace(warningFace);
                AppendLine("! SYSTEM: Enter a valid number");
                currentInput = "";
                return;
            }
            DisableInput();
            StartCoroutine(HandleAge());
        }
        else if (step == 3 && waitingForConfirmation)
        {
            string ans = currentInput.ToLower();
            DisableInput();

            if (ans == "yes" || ans == "true")
                StartCoroutine(HandleConfirmationYes());
            else if (ans == "no" || ans == "false")
                StartCoroutine(HandleConfirmationNo());
            else
            {
                AppendLine("! SYSTEM: Type yes or no");
                EnableInput();
            }
        }
        else if (waitingForCorrectionChoice)
        {
            string choice = currentInput.ToLower();
            DisableInput();

            if (choice == "name") StartCoroutine(ReenterName());
            else if (choice == "age") StartCoroutine(ReenterAge());
            else
            {
                AppendLine("! SYSTEM: Type name or age");
                EnableInput();
            }
        }

        currentInput = "";
    }

    // ================= PYTHON STRING =================
    IEnumerator HandleName()
    {
        SetFace(happyFace);
        yield return Speak($"Nice to meet you, {playerName}!");
        ShowIDCardName(playerName);
        yield return Speak("In Python, text is easy.");

        yield return Speak("You don’t declare types.");
        yield return AddSystemLine($"name = \"{playerName}\"");

        yield return Speak("Python understands it automatically.");
        yield return Speak("Clean. Simple.");

        yield return TerminalRefresh();

        SetFace(idleFace);
        yield return Speak("Now tell me your age.");

        // --- NEW: Update hint ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Enter your age (digits only)" });

        EnableInput();
        step = 2;
    }

    // ================= PYTHON INT + BOOL =================
    IEnumerator HandleAge()
    {
        SetFace(thinkingFace);

        if (playerAge < 0 || playerAge > 150)
        {
            SetFace(warningFace);
            yield return Speak("That doesn’t look right.");
            EnableInput();
            yield break;
        }

        yield return AddSystemLine($"age = {playerAge}");
        UpdateIDCardAge(playerAge);
        yield return Speak("Numbers don’t need a type either.");

        yield return TerminalRefresh();

        yield return Speak("Are these details correct?");
        yield return Speak("Type yes or no.");

        // --- NEW: Update hint ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Type 'yes' or 'no'" });

        EnableInput();
        step = 3;
        waitingForConfirmation = true;
    }

    IEnumerator HandleConfirmationYes()
    {
        SetFace(happyFace);
        yield return AddSystemLine("details_confirmed = True");
        yield return Speak("True means proceed.");

        // --- NEW: Update hint for next step (optional) ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Now we'll learn about floats" });

        StartCoroutine(ContinueWithFloat());
    }

    IEnumerator HandleConfirmationNo()
    {
        SetFace(thinkingFace);
        yield return AddSystemLine("details_confirmed = False");
        yield return Speak("Smart move.");

        yield return Speak("What should we change?");
        yield return Speak("Type: name or age");

        // --- NEW: Update hint ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Type 'name' or 'age'" });

        waitingForCorrectionChoice = true;
        EnableInput();
    }

    IEnumerator ReenterName()
    {
        yield return Speak("Alright, enter your name again.");

        // --- NEW: Update hint ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Enter your name (letters, digits, underscore allowed)" });

        EnableInput();
        step = 1;
    }

    IEnumerator ReenterAge()
    {
        yield return Speak("Okay, enter your age again.");

        // --- NEW: Update hint ---
        if (hintSystem != null)
            hintSystem.SetHints(new string[] { "Enter your age (digits only)" });

        EnableInput();
        step = 2;
    }

    // ================= PYTHON FLOAT =================
    IEnumerator ContinueWithFloat()
    {
        SetFace(thinkingFace);
        yield return Speak("Decimals are just numbers too.");

        yield return AddSystemLine("stability = 0.85");
        yield return Speak("Python treats it as a float.");

        yield return TerminalRefresh();

        yield return AddSystemLine("FINAL MEMORY STATE");
        yield return AddSystemLine($"name = \"{playerName}\"");
        yield return AddSystemLine($"age = {playerAge}");
        yield return AddSystemLine("details_confirmed = True");
        yield return AddSystemLine("stability = 0.85");

        SetFace(proudFace);
        yield return Speak("You just learned Python basics.");
        yield return Speak("Now let’s practice.");

        // --- NEW: Lesson complete — disable hints and mark scene ---
        if (hintSystem != null)
            hintSystem.DisableHints();

        MarkSceneCompleted();

        if (exerciseScript != null)
            exerciseScript.StartExercise();
    }

    // ================= TERMINAL =================
    IEnumerator TerminalRefresh()
    {
        yield return AddSystemLine("Syncing memory...");
        yield return new WaitForSeconds(0.3f);
        terminalText.text = "";
        yield return AddSystemLine("Terminal ready.");
    }

    IEnumerator AddSystemLine(string line)
    {
        terminalText.text += (terminalText.text == "" ? "" : "\n") + line;
        yield return new WaitForSeconds(systemLineDelay);
    }

    void EnableInput()
    {
        inputEnabled = true;
        AppendLine(">");
        cursorRoutine = StartCoroutine(CursorBlink());
    }

    void DisableInput()
    {
        inputEnabled = false;
        if (cursorRoutine != null) StopCoroutine(cursorRoutine);
    }

    void AppendLine(string line)
    {
        terminalText.text += (terminalText.text == "" ? "" : "\n") + line;
    }

    void RefreshInputLine()
    {
        string[] lines = terminalText.text.Split('\n');
        lines[lines.Length - 1] = $"> {currentInput}{(cursorVisible ? "_" : "")}";
        terminalText.text = string.Join("\n", lines);
    }

    IEnumerator CursorBlink()
    {
        while (inputEnabled)
        {
            cursorVisible = !cursorVisible;
            RefreshInputLine();
            yield return new WaitForSeconds(cursorBlinkRate);
        }
    }

    // ================= DIALOGUE =================
    // Modified to wait for Enter key after message is fully displayed
    IEnumerator Speak(string msg)
    {
        dialogueText.text = "";
        waitingForAdvance = true;
        skipRequested = false;

        // Type out the message
        foreach (char c in msg)
        {
            if (skipRequested)
            {
                dialogueText.text = msg;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueSpeed);
        }

        // Wait for player to press Enter (or the advance key)
        // We keep waitingForAdvance true until key press
        while (waitingForAdvance)
        {
            // Check for advance input (handled in Update via HandleDialogueAdvance)
            yield return null;
        }

        // Small pause before next line
        yield return new WaitForSeconds(0.1f);
    }

    void HandleDialogueAdvance()
    {
        if (!waitingForAdvance) return;

        // If any advance input is detected, clear the wait flag
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(advanceKey))
        {
            waitingForAdvance = false;
            skipRequested = true;   // also skip typing if still in progress
        }
    }

    // ================= ID CARD =================
    void ShowIDCardName(string name)
    {
        idNameText.text = name;
        idAgeText.text = "--";
    }

    void UpdateIDCardAge(int age)
    {
        idAgeText.text = age.ToString();
    }

    // ================= UTILITIES =================
    void SetFace(Sprite face)
    {
        if (botFaceImage && face)
            botFaceImage.sprite = face;
    }

    void PlayTypingSound(char c)
    {
        if (!typingAudio) return;

        if (c == '\b' && typeBackspace)
            typingAudio.PlayOneShot(typeBackspace);
        else if (c == ' ' && typeSpace)
            typingAudio.PlayOneShot(typeSpace);
        else if (typeLetter)
            typingAudio.PlayOneShot(typeLetter);
    }

    // --- NEW: Mark the scene as completed ---
    void MarkSceneCompleted()
    {
        // Example: set a PlayerPrefs flag
        PlayerPrefs.SetInt("TerminalVariableLessonCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("TerminalVariableLesson marked as completed.");

        // Alternatively, you could call a GameManager method:
        // GameManager.Instance.CompleteLesson("TerminalVariable");
    }
}