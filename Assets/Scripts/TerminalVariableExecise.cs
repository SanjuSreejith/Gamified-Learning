using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Globalization;

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
    int mistakesThisStep = 0;

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
        mistakesThisStep = 0;
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
        yield return Say("Press ENTER to submit.");

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
        RefreshTerminal();
    }

    // ================= SUBMIT =================
    void Submit()
    {
        string currentInput = input;
        AppendOutput($"> {currentInput}");
        input = "";

        CompilerResult result = ValidatePython(step, currentInput);

        if (result.success)
        {
            mistakesThisStep = 0;
            StartCoroutine(Correct());
        }
        else
        {
            totalMistakes++;
            mistakesThisStep++;
            StartCoroutine(ExplainMistake(result));
        }
    }

    // ================= COMPILER =================
    struct CompilerResult
    {
        public bool success;
        public string error;
        public string reference;
    }

    CompilerResult ValidatePython(int step, string raw)
    {
        string s = raw.Trim();

        if (s.Contains(";"))
            return Error("Python does not use semicolons.", GetReferenceCode(step));

        int eqIndex = s.IndexOf('=');
        if (eqIndex == -1)
            return Error("Assignment requires '='.", GetReferenceCode(step));

        string variable = s.Substring(0, eqIndex).Trim();
        string value = s.Substring(eqIndex + 1).Trim();

        if (string.IsNullOrEmpty(variable) || string.IsNullOrEmpty(value))
            return Error("Invalid assignment format.", GetReferenceCode(step));

        switch (step)
        {
            case 1:
                if (variable != "name")
                    return Error("Variable must be `name`.", GetReferenceCode(step));
                if (!(value.StartsWith("\"") && value.EndsWith("\"")))
                    return Error("Strings must be in quotes.", GetReferenceCode(step));
                if (value[1..^1] != "Alex")
                    return Error("Value must be \"Alex\".", GetReferenceCode(step));
                break;

            case 2:
                if (variable != "age")
                    return Error("Variable must be `age`.", GetReferenceCode(step));
                if (!int.TryParse(value, out int age) || age != 25)
                    return Error("Age must be 25.", GetReferenceCode(step));
                break;

            case 3:
                if (variable != "is_ready")
                    return Error("Variable must be `is_ready`.", GetReferenceCode(step));
                if (value != "True")
                    return Error("Boolean must be True.", GetReferenceCode(step));
                break;

            case 4:
                if (variable != "energy_level")
                    return Error("Variable must be `energy_level`.", GetReferenceCode(step));
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) || Mathf.Abs(f - 0.5f) > 0.001f)
                    return Error("Energy must be 0.5.", GetReferenceCode(step));
                break;
        }

        return new CompilerResult { success = true };
    }

    // ================= DIALOGUE BRANCHING =================
    IEnumerator ExplainMistake(CompilerResult r)
    {
        DisableInput();
        SetFace(mistakesThisStep >= 2 ? warningFace : thinkingFace);

        if (feedbackAudio && errorSound)
            feedbackAudio.PlayOneShot(errorSound);

        AppendOutput("Error");
        yield return Say(r.error);

        if (mistakesThisStep == 1)
            yield return Say("Slow down. Read the task carefully.");
        else if (mistakesThisStep == 2)
            yield return Say("You're repeating the same mistake.");
        else
            yield return Say("Focus. Precision matters here.");

        AppendOutput(" " + r.reference);

        if (currentOutputLines >= maxLinesBeforeClear)
            yield return ClearTerminalOutput();

        EnableInput();
    }

    IEnumerator Correct()
    {
        DisableInput();

        if (feedbackAudio && correctSound)
            feedbackAudio.PlayOneShot(correctSound);

        SetFace(happyFace);
        AppendOutput(" Correct");

        if (totalMistakes == 0)
            yield return Say("Perfect execution.");
        else if (totalMistakes < 3)
            yield return Say("Good. You're learning.");
        else
            yield return Say("You got it. Keep sharpening.");

        if (currentOutputLines >= maxLinesBeforeClear)
            yield return ClearTerminalOutput();
        else
            AppendOutput("---");

        step++;

        if (step > TOTAL_TASKS)
        {
            finished = true;
            yield return Finish();
            yield break;
        }

        yield return new WaitForSeconds(0.4f);
        BuildTask();
        EnableInput();
    }

    IEnumerator ClearTerminalOutput()
    {
        isClearingTerminal = true;
        DisableInput();

        AppendOutput("...clearing...");
        yield return new WaitForSeconds(0.3f);

        outputBuffer = "";
        currentOutputLines = 0;
        taskBuffer = "";
        BuildTask();

        yield return new WaitForSeconds(0.2f);
        isClearingTerminal = false;
    }

    // ================= FINISH =================
    IEnumerator Finish()
    {
        SetFace(proudFace);

        int accuracy = Mathf.RoundToInt(Mathf.Clamp01(1f - totalMistakes / 8f) * 100f);
        yield return Say($"Accuracy: {accuracy}%");

        if (accuracy >= 90)
            yield return Say("You think like a programmer.");
        else if (accuracy >= 70)
            yield return Say("Solid foundation. Keep practicing.");
        else
            yield return Say("You survived. Improvement awaits.");

        yield return Say("The system trusts you now.");
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
        cursorRoutine = StartCoroutine(CursorBlink());
    }

    void DisableInput()
    {
        inputEnabled = false;
        if (cursorRoutine != null) StopCoroutine(cursorRoutine);
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
        terminalText.text = taskBuffer + outputBuffer +
            (inputEnabled ? $"> {input}{(cursorVisible ? "_" : "")}\n" : "");
    }

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
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            waitingForAdvance = false;
    }

    void PlayTypingSound(char c)
    {
        if (!typingAudio) return;
        if (c == '\b' && typeBackspace) typingAudio.PlayOneShot(typeBackspace);
        else if (c == ' ' && typeSpace) typingAudio.PlayOneShot(typeSpace);
        else if (typeLetter) typingAudio.PlayOneShot(typeLetter);
    }

    void SetFace(Sprite face)
    {
        if (botFaceImage && face) botFaceImage.sprite = face;
    }

    CompilerResult Error(string msg, string reference)
    {
        return new CompilerResult { success = false, error = msg, reference = reference };
    }

    string GetReferenceCode(int s)
    {
        return s switch
        {
            1 => "name = \"Alex\"",
            2 => "age = 25",
            3 => "is_ready = True",
            4 => "energy_level = 0.5",
            _ => ""
        };
    }
}
