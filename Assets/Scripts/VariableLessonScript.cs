using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AdvancedTerminalVariableLesson : MonoBehaviour
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
    public bool autoSkipDialogue = true;

    string currentInput = "";
    bool inputEnabled;
    bool cursorVisible = true;

    Coroutine cursorRoutine;

    string playerName;
    int playerAge;

    int step = 0;
    bool skipRequested;
    bool dialogueFinished;

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
            if (c == '\b')
            {
                if (currentInput.Length > 0)
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                SubmitInput();
            }
            else
            {
                if (step == 1 && (char.IsLetterOrDigit(c) || c == '_'))
                    currentInput += c;

                else if (step == 2 && char.IsDigit(c))
                    currentInput += c;
            }
        }

        RefreshInputLine();
    }

    // ================= TERMINAL BOOT =================
    IEnumerator TerminalBoot()
    {
        yield return AddSystemLine(">>> MEMORY OS v0.1 <<<");
        yield return AddSystemLine("Booting core modules...");
        yield return AddSystemLine("Loading language engine...");
        yield return AddSystemLine("System status: STABLE");
        yield return AddSystemLine("-----------------------");

        SetFace(thinkingFace);
        yield return StartDialogue("Oh… someone’s here?");
        yield return StartDialogue("Hi. I’m glad you found this place.");
        yield return StartDialogue("Before we fix anything, I should know who I’m talking to.");

        SetFace(idleFace);
        yield return StartDialogue("Type your name below. I’ll wait.");

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
                AppendLine("! SYSTEM: Invalid numeric input");
                currentInput = "";
                return;
            }

            DisableInput();
            StartCoroutine(HandleAge());
        }

        currentInput = "";
    }

    // ================= NAME =================
    IEnumerator HandleName()
    {
        SetFace(happyFace);
        yield return StartDialogue($"Nice to meet you, {playerName}.");
        yield return StartDialogue("I’ll store that carefully.");

        SetFace(thinkingFace);
        yield return AddSystemLine("Allocating memory block...");
        yield return AddSystemLine("Type detected: string");
        yield return AddSystemLine($"string name = \"{playerName}\"");

        yield return StartDialogue("A variable is just a labeled memory box.");
        yield return StartDialogue("The label is the variable name.");
        yield return StartDialogue("The content inside is the value.");

        yield return TerminalRefresh();

        SetFace(idleFace);
        yield return StartDialogue("Alright. Name saved.");
        yield return StartDialogue("Now tell me your age.");
        yield return StartDialogue("Only numbers this time.");

        EnableInput();
        step = 2;
    }

    // ================= AGE =================
    IEnumerator HandleAge()
    {
        SetFace(thinkingFace);

        if (playerAge < 0)
        {
            SetFace(warningFace);
            yield return StartDialogue("Hmm… that doesn’t look right.");
            yield return StartDialogue("Negative age would break time itself.");
            yield return AddSystemLine("ERROR: Invalid age range");
            EnableInput();
            yield break;
        }

        if (playerAge > 150)
        {
            SetFace(warningFace);
            yield return StartDialogue("That age exceeds human limits.");
            yield return StartDialogue("Are you a legend… or something else?");
            yield return AddSystemLine("WARNING: Age value unrealistic");
            EnableInput();
            yield break;
        }

        yield return AddSystemLine("Validating numeric value...");
        yield return AddSystemLine("Type detected: int");
        yield return AddSystemLine($"int age = {playerAge}");

        if (playerAge <= 12)
            yield return StartDialogue("That’s very young. Curiosity starts early.");

        else if (playerAge <= 19)
            yield return StartDialogue("Perfect age to understand how systems work.");

        else if (playerAge <= 59)
            yield return StartDialogue("Experience and logic go well together.");

        else
            yield return StartDialogue("That’s a lot of wisdom.");

        yield return StartDialogue("Numbers use the keyword 'int'.");
        yield return StartDialogue("Keywords tell me how to treat data.");

        yield return TerminalRefresh();

        yield return AddSystemLine("FINAL MEMORY STATE");
        yield return AddSystemLine("------------------");
        yield return AddSystemLine($"string name = \"{playerName}\"");
        yield return AddSystemLine($"int age = {playerAge}");

        SetFace(proudFace);
        yield return StartDialogue("Well done.");
        yield return StartDialogue("You didn’t just enter data.");
        yield return StartDialogue("You taught me how to remember you.");

        DisableInput();
    }

    // ================= TERMINAL =================
    IEnumerator TerminalRefresh()
    {
        yield return AddSystemLine("Syncing memory...");
        yield return new WaitForSeconds(0.4f);
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
        string cursor = cursorVisible ? "_" : "";
        lines[lines.Length - 1] = $"> {currentInput}{cursor}";
        terminalText.text = string.Join("\n", lines);
    }

    IEnumerator CursorBlink()
    {
        while (true)
        {
            cursorVisible = !cursorVisible;
            yield return new WaitForSeconds(cursorBlinkRate);
        }
    }

    // ================= DIALOGUE =================
    IEnumerator StartDialogue(string message)
    {
        dialogueFinished = false;
        skipRequested = false;
        dialogueText.text = "";

        foreach (char c in message)
        {
            if (autoSkipDialogue && skipRequested)
            {
                dialogueText.text = message;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueSpeed);
        }

        dialogueFinished = true;

        if (!autoSkipDialogue)
            yield return new WaitUntil(() => skipRequested);
        else
            yield return new WaitForSeconds(0.4f);
    }

    void HandleDialogueAdvance()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (autoSkipDialogue && !dialogueFinished)
                skipRequested = true;
            else if (!autoSkipDialogue && dialogueFinished)
                skipRequested = true;
        }
    }

    void SetFace(Sprite face)
    {
        if (botFaceImage && face)
            botFaceImage.sprite = face;
    }
}
