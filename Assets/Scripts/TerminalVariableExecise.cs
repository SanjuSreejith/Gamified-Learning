using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TerminalVariableExercise : MonoBehaviour
{
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

    [Header("Terminal Auto Clear")]
    public int maxOutputLines = 10;
    public float clearDelay = 0.15f;

    // ================= INTERNAL =================
    string input = "";
    bool inputEnabled;
    bool cursorVisible = true;
    bool clearing;

    Coroutine cursorRoutine;

    int step = 1;
    int mistakes = 0;
    int totalMistakes = 0;

    const int TOTAL_TASKS = 4;

    string taskBuffer = "";
    string outputBuffer = "";

    bool waitingForAdvance;
    bool finished;

    // ================= START =================
    void Start()
    {
        terminalText.text = "";
        dialogueText.text = "";
        terminalText.richText = false;
        dialogueText.richText = false;
        SetFace(neutralFace);
    }

    public void StartExercise()
    {
        StopAllCoroutines();

        step = 1;
        mistakes = 0;
        totalMistakes = 0;
        input = "";
        finished = false;

        taskBuffer = "";
        outputBuffer = "";

        StartCoroutine(Flow());
    }

    // ================= FLOW =================
    IEnumerator Flow()
    {
        yield return Say("Alright… now it’s your turn.");
        yield return Say("Think carefully.");
        yield return Say("Let’s begin.");

        BuildTask();
        EnableInput();
    }

    // ================= UPDATE =================
    void Update()
    {
        HandleDialogueAdvance();

        if (!inputEnabled || clearing || finished) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b' && input.Length > 0)
                input = input[..^1];
            else if (c == '\n' || c == '\r')
                Submit();
            else if (!char.IsControl(c))
                input += c;
        }

        RefreshTerminal();
    }

    // ================= TASK =================
    void BuildTask()
    {
        taskBuffer = $"TASK {step}:\n";

        switch (step)
        {
            case 1:
                taskBuffer += "Store a name\nValue: \"Alex\"\nstring OR char array\n";
                break;
            case 2:
                taskBuffer += "Store age\nValue: 25\n";
                break;
            case 3:
                taskBuffer += "Store decision isReady\nValue: true\n";
                break;
            case 4:
                taskBuffer += "Store decimal energy\nValue: 0.5\n";
                break;
        }

        taskBuffer += "\n";
        outputBuffer = "";
    }

    // ================= SUBMIT =================
    void Submit()
    {
        AppendOutput($"> {input}");

        if (!input.TrimEnd().EndsWith(";"))
        {
            StartCoroutine(MissingSemicolon());
            input = "";
            return;
        }

        string clean = Clean(input);
        bool correct = false;

        if (step == 1)
        {
            correct =
                clean == "stringname=\"alex\"" ||
                (clean.StartsWith("charname[") && clean.Contains("=\"alex\""));
        }
        else if (step == 2)
            correct = clean == "intage=25";
        else if (step == 3)
            correct = clean == "boolisready=true";
        else if (step == 4)
            correct = clean == "floatenergy=0.5" || clean == "floatenergy=0.5f";

        input = "";

        if (correct)
            StartCoroutine(Correct());
        else
            StartCoroutine(Mistake());
    }

    // ================= CORRECT =================
    IEnumerator Correct()
    {
        DisableInput();
        mistakes = 0;

        SetFace(happyFace);
        AppendOutput("✓ Compiled Successfully");
        yield return Say("Correct.");

        step++;

        if (step > TOTAL_TASKS)
        {
            finished = true;
            yield return FinishWithEvaluation();
            yield break;
        }

        yield return ClearOutputAnimated();
        BuildTask();
        EnableInput();
    }

    // ================= MISTAKE =================
    IEnumerator Mistake()
    {
        mistakes++;
        totalMistakes++;

        SetFace(thinkingFace);
        AppendOutput("! Compile Error");

        if (mistakes >= 3)
        {
            SetFace(warningFace);
            yield return Say("Here’s a hint:");
            yield return Say(GetHint());
        }
        else
        {
            yield return Say("Check the structure.");
        }

        EnableInput();
    }

    IEnumerator MissingSemicolon()
    {
        totalMistakes++;
        SetFace(warningFace);

        AppendOutput("! Missing semicolon");
        yield return Say("In C-style languages, every statement ends with ';'");

        EnableInput();
    }

    string GetHint()
    {
        return step switch
        {
            1 => "char name[20] = \"Alex\";  OR  string name = \"Alex\";",
            2 => "int age = 25;",
            3 => "bool isReady = true;",
            4 => "float energy = 0.5;",
            _ => ""
        };
    }

    // ================= FINAL =================
    IEnumerator FinishWithEvaluation()
    {
        DisableInput();
        yield return ClearOutputAnimated();

        float accuracy = Mathf.Clamp01(1f - (float)totalMistakes / (TOTAL_TASKS * 3));
        int percent = Mathf.RoundToInt(accuracy * 100f);

        AppendOutput($"Accuracy: {percent}%\n");

        if (percent >= 90)
        {
            SetFace(proudFace);
            yield return Say("Excellent work.");
            yield return Say("You truly understood variables.");
        }
        else if (percent >= 70)
        {
            SetFace(happyFace);
            yield return Say("Good job.");
            yield return Say("You’re ready to move forward.");
        }
        else
        {
            SetFace(thinkingFace);
            yield return Say("You made mistakes.");
            yield return Say("But learning happened.");
        }

        yield return Say("You are ready to enter the game world.");

        AppendOutput("\n>>> ENTERING GAME WORLD <<<");
    }

    // ================= OUTPUT =================
    void AppendOutput(string line)
    {
        outputBuffer += line + "\n";
        AutoClearCheck();
        RefreshTerminal();
    }

    void AutoClearCheck()
    {
        if (clearing || finished) return;

        int lines = outputBuffer.Split('\n').Length;
        if (lines >= maxOutputLines)
            StartCoroutine(ClearOutputAnimated());
    }

    IEnumerator ClearOutputAnimated()
    {
        clearing = true;
        DisableInput();

        outputBuffer += "--- clearing terminal ---\n";
        RefreshTerminal();
        yield return new WaitForSeconds(clearDelay * 3f);

        outputBuffer = "";
        RefreshTerminal();

        clearing = false;
    }

    // ================= CURSOR =================
    void EnableInput()
    {
        if (finished) return;

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
        terminalText.text = taskBuffer + outputBuffer;

        if (!inputEnabled || finished) return;

        terminalText.text += $"> {input}{(cursorVisible ? "_" : "")}\n";
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
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
            waitingForAdvance = false;
    }

    // ================= HELPERS =================
    string Clean(string raw)
    {
        return raw.ToLower().Replace(" ", "").Replace(";", "");
    }

    void SetFace(Sprite face)
    {
        if (botFaceImage && face)
            botFaceImage.sprite = face;
    }
}
