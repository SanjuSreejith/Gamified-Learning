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
    public bool autoAdvanceDialogue = true;
    public KeyCode advanceKey = KeyCode.Return;

    string currentInput = "";
    bool inputEnabled;
    bool cursorVisible = true;
    Coroutine cursorRoutine;

    string playerName;
    int playerAge;

    int step = 0;

    // Dialogue flow
    bool waitingForAdvance;
    bool skipRequested;

    // Boolean flow
    bool waitingForConfirmation;
    bool waitingForCorrectionChoice;

    public TerminalVariableExercise exerciseScript;

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
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
            else if (c == '\n' || c == '\r')
                SubmitInput();
            else
            {
                if (step == 1 && (char.IsLetterOrDigit(c) || c == '_'))
                    currentInput += c;
                else if (step == 2 && char.IsDigit(c))
                    currentInput += c;
                else if (step == 3 && char.IsLetter(c))
                    currentInput += c;
            }
        }

        RefreshInputLine();
    }

    // ================= BOOT =================
    // ================= BOOT =================
    IEnumerator TerminalBoot()
    {
        yield return AddSystemLine(">>> MEMORY OS v0.1 <<<");
        yield return AddSystemLine("Booting core modules...");
        yield return AddSystemLine("C language layer active");
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

        // 🔑 EMOTIONAL TRANSITION
        yield return Speak("…Okay.");
        yield return Speak("You’re safe now.");

        // ORIGINAL FRIENDLY INTRO
        yield return Speak("Oh… hey.");
        yield return Speak("I don’t get visitors often.");
        yield return Speak("But I’m glad you’re here.");
        yield return Speak("Let’s start simple.");

        SetFace(idleFace);
        yield return Speak("What’s your name?");
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
                AppendLine("! SYSTEM: I need a whole number 🙂");
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
                waitingForConfirmation = true;
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
                waitingForCorrectionChoice = true;
            }
        }

        currentInput = "";
    }

    // ================= STRING =================
    IEnumerator HandleName()
    {
        SetFace(happyFace);
        yield return Speak($"Nice to meet you, {playerName}!");
        yield return Speak("Names matter. Let’s store it.");

        SetFace(thinkingFace);
        yield return Speak("In pure C, text is stored as characters.");
        yield return AddSystemLine($"char name[] = \"{playerName}\";");

        yield return Speak("That works… but it’s heavy for beginners.");
        yield return Speak("So we use a learning shortcut.");

        yield return AddSystemLine($"string name = \"{playerName}\";");
        yield return Speak("Notice the semicolon?");
        yield return Speak("It marks the end of an instruction.");

        yield return TerminalRefresh();

        SetFace(idleFace);
        yield return Speak("Now tell me your age.");
        EnableInput();
        step = 2;
    }

    // ================= INT + BOOL =================
    IEnumerator HandleAge()
    {
        SetFace(thinkingFace);

        if (playerAge < 0 || playerAge > 150)
        {
            SetFace(warningFace);
            yield return Speak("That doesn’t seem realistic 😅");
            EnableInput();
            yield break;
        }

        yield return AddSystemLine($"int age = {playerAge};");
        yield return Speak("Whole numbers use the int type.");

        yield return TerminalRefresh();

        yield return Speak("Before I lock this in…");
        yield return Speak("Are you sure these details are correct?");
        yield return Speak("Type yes or no.");

        EnableInput();
        step = 3;
        waitingForConfirmation = true;
    }

    IEnumerator HandleConfirmationYes()
    {
        waitingForConfirmation = false;
        SetFace(happyFace);

        yield return AddSystemLine("bool detailsConfirmed = true;");
        yield return Speak("Got it ");
        yield return Speak("true means continue.");

        StartCoroutine(ContinueWithFloat());
    }

    IEnumerator HandleConfirmationNo()
    {
        waitingForConfirmation = false;
        SetFace(thinkingFace);

        yield return AddSystemLine("bool detailsConfirmed = false;");
        yield return Speak("Good choice.");
        yield return Speak("Questioning data is smart.");

        yield return Speak("What would you like to change?");
        yield return Speak("Type: name or age");

        waitingForCorrectionChoice = true;
        EnableInput();
    }

    IEnumerator ReenterName()
    {
        yield return Speak("Alright, let’s fix your name.");
        EnableInput();
        step = 1;
    }

    IEnumerator ReenterAge()
    {
        yield return Speak("Okay, let’s fix your age.");
        EnableInput();
        step = 2;
    }

    // ================= FLOAT =================
    IEnumerator ContinueWithFloat()
    {
        SetFace(thinkingFace);
        yield return Speak("This world isn’t perfectly stable.");

        yield return AddSystemLine("float stability = 0.85;");
        yield return Speak("Decimals use the float type.");
        yield return Speak("Used for health, speed, energy.");

        yield return TerminalRefresh();

        yield return AddSystemLine("FINAL MEMORY STATE");
        yield return AddSystemLine($"char name[] = \"{playerName}\";");
        yield return AddSystemLine($"int age = {playerAge};");
        yield return AddSystemLine("bool detailsConfirmed = true;");
        yield return AddSystemLine("float stability = 0.85;");

        SetFace(proudFace);
        yield return Speak("That was real programming.");
        yield return Speak("Now let’s practice.");

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
    IEnumerator Speak(string msg)
    {
        dialogueText.text = "";
        waitingForAdvance = true;
        skipRequested = false;

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

        if (autoAdvanceDialogue)
        {
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            yield return new WaitUntil(() => skipRequested);
        }

        waitingForAdvance = false;
    }

    void HandleDialogueAdvance()
    {
        if (!waitingForAdvance) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(advanceKey))
            skipRequested = true;
    }

    void SetFace(Sprite face)
    {
        if (botFaceImage && face)
            botFaceImage.sprite = face;
    }
}
