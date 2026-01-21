using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TerminalVariableExercise : MonoBehaviour
{
    // ================= UI =================
    [Header("UI")]
    public TextMeshProUGUI terminalText;
    public TextMeshProUGUI dialogueText;
    public Image botFaceImage;

    [Header("Bot Faces")]
    public Sprite neutralFace;
    public Sprite thinkingFace;
    public Sprite happyFace;
    public Sprite proudFace;
    public Sprite warningFace;

    [Header("Dialogue")]
    public bool autoSkipDialogue = true;
    public float dialogueSpeed = 0.03f;
    public float autoSkipDelay = 0.4f;

    [Header("Cursor")]
    public float cursorBlinkRate = 0.5f;

    [Header("Terminal Management")]
    public int maxLinesBeforeClear = 5;
    private int currentOutputLines = 0;

    [Header("Typing Audio")]
    public AudioSource typingAudio;
    public AudioClip typeLetter;
    public AudioClip typeSpace;
    public AudioClip typeBackspace;

    [Header("Feedback Audio")]
    public AudioSource feedbackAudio;
    public AudioClip correctSound;
    public AudioClip errorSound;

    [Header("Scene Transition")]
    public string nextSceneName = "GameScene";
    public float sceneChangeDelay = 2f;

    // ================= INTERNAL =================
    string input = "";
    bool inputEnabled;
    bool cursorVisible = true;
    bool finished;

    Coroutine cursorRoutine;

    int step = 1;
    int totalMistakes = 0;

    const int TOTAL_TASKS = 4;

    string taskBuffer = "";
    string outputBuffer = "";

    bool waitingForAdvance;
    bool isClearingTerminal = false;

    // ================= START =================
    void Start()
    {
        terminalText.text = "";
        dialogueText.text = "";
        SetFace(neutralFace);
    }

    public void StartExercise()
    {
        StopAllCoroutines();

        step = 1;
        totalMistakes = 0;
        input = "";
        finished = false;
        taskBuffer = "";
        outputBuffer = "";
        currentOutputLines = 0;

        StartCoroutine(Flow());
    }

    // ================= FLOW =================
    IEnumerator Flow()
    {
        yield return Say("Now you will write Python.");
        yield return Say("No types. No semicolons.");
        yield return Say("Just clean logic.");

        BuildTask();
        EnableInput();
    }

    // ================= UPDATE =================
    void Update()
    {
        HandleDialogueAdvance();

        if (!inputEnabled || finished || isClearingTerminal) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b' && input.Length > 0)
            {
                input = input[..^1];
                PlayTypingSound(c);
            }
            else if (c == '\n' || c == '\r')
            {
                Submit();
            }
            else if (!char.IsControl(c))
            {
                input += c;
                PlayTypingSound(c);
            }
        }

        RefreshTerminal();
    }

    // ================= TASK =================
    void BuildTask()
    {
        taskBuffer = $"TASK {step}/{TOTAL_TASKS} (Python):\n";

        switch (step)
        {
            case 1:
                taskBuffer += "Store a name\nValue: \"Alex\"\nVariable: name\n";
                break;

            case 2:
                taskBuffer += "Store age\nValue: 25\nVariable: age\n";
                break;

            case 3:
                taskBuffer += "Store readiness\nValue: True\nVariable: is_ready\n";
                break;

            case 4:
                taskBuffer += "Store energy level\nValue: 0.5\nVariable: energy_level\n";
                break;
        }

        taskBuffer += "\n";
        // Don't clear outputBuffer here - keep previous output
        RefreshTerminal();
    }

    // ================= SUBMIT =================
    void Submit()
    {
        string currentInput = input;
        AppendOutput($"> {currentInput}");

        CompilerResult result = ValidatePython(step, currentInput);
        input = "";

        if (result.success)
        {
            StartCoroutine(Correct());
        }
        else
        {
            totalMistakes++;
            StartCoroutine(ExplainMistake(result));
        }
    }

    // ================= COMPILER RESULT =================
    struct CompilerResult
    {
        public bool success;
        public string error;
        public string reference;
    }

    // ================= VALIDATION =================
    CompilerResult ValidatePython(int step, string raw)
    {
        string s = raw.Trim();

        // Check for semicolons first (Python doesn't use them)
        if (s.Contains(";"))
            return Error("Python does not use semicolons.", GetReferenceCode(step));

        // Basic format check - must contain assignment
        if (!s.Contains("="))
            return Error("Assignment requires '='.", GetReferenceCode(step));

        // Split into left (variable) and right (value)
        string[] parts = s.Split('=');
        if (parts.Length != 2)
            return Error("Invalid assignment format.", GetReferenceCode(step));

        string variable = parts[0].Trim();
        string value = parts[1].Trim();

        switch (step)
        {
            case 1:
                if (variable != "name")
                    return Error("Variable must be named `name`.", GetReferenceCode(step));

                // Check if value is a string (in quotes)
                if (value.Length < 2 || !((value.StartsWith("\"") && value.EndsWith("\"")) ||
                                         (value.StartsWith("'") && value.EndsWith("'"))))
                    return Error("String values must be in quotes.", GetReferenceCode(step));

                // Check if the value inside quotes is "Alex"
                string innerValue = value.Substring(1, value.Length - 2);
                if (innerValue != "Alex")
                    return Error($"Value should be \"Alex\", not \"{innerValue}\".", GetReferenceCode(step));
                break;

            case 2:
                if (variable != "age")
                    return Error("Variable must be named `age`.", GetReferenceCode(step));

                if (!int.TryParse(value, out int ageValue))
                    return Error("Age must be a whole number.", GetReferenceCode(step));

                if (ageValue != 25)
                    return Error($"Value should be 25, not {ageValue}.", GetReferenceCode(step));
                break;

            case 3:
                if (variable != "is_ready")
                    return Error("Variable must be named `is_ready`.", GetReferenceCode(step));

                if (!(value == "True" || value == "False"))
                    return Error("Boolean values are either True or False.", GetReferenceCode(step));

                if (value != "True")
                    return Error($"Value should be True, not {value}.", GetReferenceCode(step));
                break;

            case 4:
                if (variable != "energy_level")
                    return Error("Variable must be named `energy_level`.", GetReferenceCode(step));

                if (!float.TryParse(value, out float energyValue))
                    return Error("Energy must be a decimal number.", GetReferenceCode(step));

                if (Mathf.Abs(energyValue - 0.5f) > 0.001f)
                    return Error($"Value should be 0.5, not {value}.", GetReferenceCode(step));
                break;
        }

        return new CompilerResult { success = true };
    }

    string GetReferenceCode(int stepNum)
    {
        switch (stepNum)
        {
            case 1: return "name = \"Alex\"";
            case 2: return "age = 25";
            case 3: return "is_ready = True";
            case 4: return "energy_level = 0.5";
            default: return "";
        }
    }

    CompilerResult Error(string msg, string reference)
    {
        return new CompilerResult
        {
            success = false,
            error = msg,
            reference = reference
        };
    }

    // ================= FEEDBACK =================
    IEnumerator ExplainMistake(CompilerResult r)
    {
        DisableInput();
        SetFace(thinkingFace);

        if (feedbackAudio && errorSound)
            feedbackAudio.PlayOneShot(errorSound);

        AppendOutput("Error");
        yield return Say(r.error);

        if (!string.IsNullOrEmpty(r.reference))
        {
            yield return Say("Try this:");
            AppendOutput(" " + r.reference);
        }

        // Check if we should clear the terminal
        if (currentOutputLines >= maxLinesBeforeClear)
        {
            yield return ClearTerminalOutput();
        }

        EnableInput();
    }

    IEnumerator Correct()
    {
        DisableInput();

        if (feedbackAudio && correctSound)
            feedbackAudio.PlayOneShot(correctSound);

        SetFace(happyFace);
        AppendOutput(" Correct");
        yield return Say("Well done.");

        // Check if we should clear the terminal before next task
        if (currentOutputLines >= maxLinesBeforeClear)
        {
            yield return ClearTerminalOutput();
        }
        else
        {
            AppendOutput("---"); // Separator line
        }

        step++;

        if (step > TOTAL_TASKS)
        {
            finished = true;
            yield return Finish();
            yield break;
        }

        yield return new WaitForSeconds(0.5f); // Brief pause
        BuildTask();
        EnableInput();
    }

    IEnumerator ClearTerminalOutput()
    {
        isClearingTerminal = true;
        DisableInput();

        AppendOutput("...clearing...");
        yield return new WaitForSeconds(0.3f);

        // Clear output buffer but keep task buffer
        outputBuffer = "";
        currentOutputLines = 0;
        RefreshTerminal();

        yield return new WaitForSeconds(0.2f);
        isClearingTerminal = false;
    }

    // ================= FINISH =================
    IEnumerator Finish()
    {
        SetFace(proudFace);
        yield return Say("Excellent! You understand Python variables.");

        float accuracy = Mathf.Clamp01(1f - (float)totalMistakes / (TOTAL_TASKS * 2f));
        int percent = Mathf.RoundToInt(accuracy * 100f);

        AppendOutput($"\n--- FINISHED ---");
        AppendOutput($"Tasks: {TOTAL_TASKS}/{TOTAL_TASKS}");
        AppendOutput($"Mistakes: {totalMistakes}");
        AppendOutput($"Accuracy: {percent}%");

        yield return Say($"Accuracy: {percent}%");

        if (percent >= 90)
            yield return Say("Perfect! You're ready.");
        else if (percent >= 70)
            yield return Say("Good job. Keep practicing.");
        else
            yield return Say("You're getting there. Review the basics.");

        yield return Say("The system trusts you now.");
        AppendOutput("\n>>> ENTERING GAME WORLD <<<");

        yield return new WaitForSeconds(sceneChangeDelay);
        SceneManager.LoadScene(nextSceneName);
    }

    // ================= TERMINAL =================
    void AppendOutput(string line)
    {
        outputBuffer += line + "\n";
        currentOutputLines++;
        RefreshTerminal();
    }

    void EnableInput()
    {
        inputEnabled = true;
        cursorVisible = true;

        if (cursorRoutine != null)
            StopCoroutine(cursorRoutine);

        cursorRoutine = StartCoroutine(CursorBlink());
    }

    void DisableInput()
    {
        inputEnabled = false;
        if (cursorRoutine != null)
            StopCoroutine(cursorRoutine);
    }

    IEnumerator CursorBlink()
    {
        while (inputEnabled)
        {
            cursorVisible = !cursorVisible;
            RefreshTerminal();
            yield return new WaitForSeconds(cursorBlinkRate);
        }
    }

    void RefreshTerminal()
    {
        string fullText = taskBuffer + outputBuffer;

        if (inputEnabled && !finished && !isClearingTerminal)
        {
            fullText += $"> {input}{(cursorVisible ? "_" : "")}\n";
        }

        terminalText.text = fullText;
    }

    // ================= DIALOGUE =================
    IEnumerator Say(string msg)
    {
        waitingForAdvance = true;
        dialogueText.text = "";

        foreach (char c in msg)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueSpeed);
        }

        if (autoSkipDialogue)
            yield return new WaitForSeconds(autoSkipDelay);
        else
            yield return new WaitUntil(() => !waitingForAdvance);

        waitingForAdvance = false;
    }

    void HandleDialogueAdvance()
    {
        if (!waitingForAdvance || autoSkipDialogue) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            waitingForAdvance = false;
    }

    // ================= HELPERS =================
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

    void SetFace(Sprite face)
    {
        if (botFaceImage && face)
            botFaceImage.sprite = face;
    }
}