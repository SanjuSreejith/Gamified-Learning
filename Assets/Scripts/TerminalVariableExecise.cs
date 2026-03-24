using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Globalization;

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
    public float dialogueSpeed = 0.03f;
    public KeyCode advanceKey = KeyCode.Return;

    [Header("Cursor")]
    public float cursorBlinkRate = 0.5f;

    [Header("Audio")]
    public AudioSource typingAudio;
    public AudioClip typeLetter;
    public AudioClip typeSpace;
    public AudioClip typeBackspace;

    public AudioSource feedbackAudio;
    public AudioClip correctSound;
    public AudioClip errorSound;

    [Header("Scene")]
    public string nextSceneName = "GameScene";
    public float sceneChangeDelay = 2f;

    [Header("Hint System")]
    public BotHintSystem hintSystem;

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
    private const string PREF_KEY = "TypeSpeed";

    // ================= START =================
    void Start()
    {   dialogueSpeed = PlayerPrefs.GetFloat(PREF_KEY);
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
        outputBuffer = "";

        hintSystem?.EnableHints();
        UpdateHintForStep();

        StartCoroutine(Flow());
    }

    // ================= FLOW =================
    IEnumerator Flow()
    {
        yield return Say("Now it's your turn.");
        yield return Say("Think before you type.");
        yield return Say("Use F1 if you're stuck.");

        BuildTask();
        EnableInput();
    }

    // ================= UPDATE =================
    void Update()
    {
        HandleDialogueAdvance();

        if (!inputEnabled || finished) return;

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
        taskBuffer = $"[ TASK {step}/{TOTAL_TASKS} ]\n";
        switch (step)
        {
            case 1:
                taskBuffer += "Create a variable called 'name'\n";
                taskBuffer += "and set it to \"Arya\"\n";
                break;

            case 2:
                taskBuffer += "Create a variable called 'age'\n";
                taskBuffer += "and set it to 25\n";
                break;

            case 3:
                taskBuffer += "Create a variable called 'is_ready'\n";
                taskBuffer += "and set it to True\n";
                break;

            case 4:
                taskBuffer += "Create a variable called 'energy_level'\n";
                taskBuffer += "and set it to 10.5\n";
                break;
        }

        taskBuffer += "\n";

        UpdateHintForStep();
        RefreshTerminal();
    }

    // ================= SMART HINT SYSTEM =================
    void UpdateHintForStep()
    {
        if (hintSystem == null) return;

        string hint = step switch
        {
            1 => "Use a variable and assign a string value",
            2 => "Assign a number (no quotes)",
            3 => "Boolean values are True/False",
            4 => "Decimal numbers use dot (.)",
            _ => ""
        };

        hintSystem.SetHints(new string[] { hint });
    }

    void UpdateHintAfterMistake()
    {
        if (hintSystem == null) return;

        string hint = step switch
        {
            1 => "Format: variable = \"text\"",
            2 => "Format: variable = number",
            3 => "Format: variable = True",
            4 => "Format: variable = decimal",
            _ => ""
        };

        hintSystem.SetHints(new string[] { hint });
    }

    // ================= SUBMIT =================
    void Submit()
    {
        string currentInput = input;

        // 🔥 CLEAR TERMINAL EACH ATTEMPT
        outputBuffer = "";

        AppendOutput("> Executing...");
        input = "";

        var result = ValidatePython(step, currentInput);

        if (result.success)
        {
            mistakesThisStep = 0;
            StartCoroutine(Correct());
        }
        else
        {
            totalMistakes++;
            mistakesThisStep++;

            UpdateHintAfterMistake(); // 🔥 dynamic hint

            StartCoroutine(ExplainMistake(result));
        }
    }

    struct CompilerResult
    {
        public bool success;
        public string error;
    }

    CompilerResult ValidatePython(int step, string raw)
    {
        string s = raw.Trim();

        if (!s.Contains("="))
            return Error("Missing '='");

        var parts = s.Split('=');
        string variable = parts[0].Trim();
        string value = parts[1].Trim();

        switch (step)
        {
            case 1:
                if (variable != "name") return Error("Wrong variable");
                if (value != "\"Arya\"") return Error("Wrong value");
                break;

            case 2:
                if (variable != "age") return Error("Wrong variable");
                if (value != "25") return Error("Wrong number");
                break;

            case 3:
                if (variable != "is_ready") return Error("Wrong variable");
                if (value != "True") return Error("Wrong boolean");
                break;

            case 4:
                if (variable != "energy_level") return Error("Wrong variable");
                if (value != "10.5") return Error("Wrong decimal");
                break;
        }

        return new CompilerResult { success = true };
    }

    // ================= FEEDBACK =================
    IEnumerator ExplainMistake(CompilerResult r)
    {
        DisableInput();

        SetFace(warningFace);
        feedbackAudio?.PlayOneShot(errorSound);

        AppendOutput("✖ Failed");

        yield return Say(r.error);

        if (mistakesThisStep == 1)
            yield return Say("Look carefully.");
        else if (mistakesThisStep == 2)
            yield return Say("You're close.");
        else
            yield return Say("Press F1 for help.");

        EnableInput();
    }

    IEnumerator Correct()
    {
        DisableInput();

        feedbackAudio?.PlayOneShot(correctSound);
        SetFace(happyFace);

        AppendOutput("Success");

        yield return Say("Nice.");

        step++;

        if (step > TOTAL_TASKS)
        {
            finished = true;
            yield return Finish();
            yield break;
        }

        yield return new WaitForSeconds(0.3f);

        BuildTask();
        EnableInput();
    }

    IEnumerator Finish()
    {
        SetFace(proudFace);

        int accuracy = Mathf.RoundToInt(Mathf.Clamp01(1f - totalMistakes / 8f) * 100f);

        AppendOutput($"Accuracy: {accuracy}%");

        yield return Say("You understand variables.");

        MarkSceneCompleted();

        hintSystem?.DisableHints();

        yield return new WaitForSeconds(sceneChangeDelay);
        SceneManager.LoadScene(nextSceneName);
    }

    // ================= SAVE =================
    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        PlayerPrefs.SetInt("SceneCompleted_" + sceneName, 1);

        PlayerPrefs.Save();
    }

    // ================= TERMINAL =================
    void AppendOutput(string line)
    {
        outputBuffer += line + "\n";
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

        yield return new WaitUntil(() => !waitingForAdvance);
    }

    void HandleDialogueAdvance()
    {
        if (!waitingForAdvance) return;

        if (Input.GetKeyDown(advanceKey))
            waitingForAdvance = false;
    }

    void PlayTypingSound(char c)
    {
        if (!typingAudio) return;
        if (c == '\b') typingAudio.PlayOneShot(typeBackspace);
        else if (c == ' ') typingAudio.PlayOneShot(typeSpace);
        else typingAudio.PlayOneShot(typeLetter);
    }

    void SetFace(Sprite face)
    {
        if (botFaceImage && face)
            botFaceImage.sprite = face;
    }

    CompilerResult Error(string msg)
    {
        return new CompilerResult { success = false, error = msg };
    }
}