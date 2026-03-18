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

    [Header("Dialogue Control")]
    public KeyCode advanceKey = KeyCode.Return;

    [Header("ID Card UI")]
    public GameObject idCardPanel;
    public TextMeshProUGUI idNameText;
    public TextMeshProUGUI idAgeText;

    [Header("Typing Audio")]
    public AudioSource typingAudio;
    public AudioClip typeLetter;
    public AudioClip typeSpace;
    public AudioClip typeBackspace;

    [Header("Exercise")]
    public TerminalVariableExercise exerciseScript;

    [Header("Hint System")]
    public BotHintSystem hintSystem;

    [Header("Player Profile")]
    public PlayerProfileManager profileManager;

    const string NAME_KEY = "PlayerName";
    const string AGE_KEY = "PlayerAge";

    string currentInput = "";
    bool inputEnabled;
    bool cursorVisible = true;
    Coroutine cursorRoutine;

    string playerName;
    int playerAge;

    int step = 0;

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
        yield return AddSystemLine("Rebuilding safe environment...");
        yield return AddSystemLine("Bypassing NULL detection...");
        yield return AddSystemLine("Python runtime active ✔");
        yield return AddSystemLine("----------------------------");

        SetFace(thinkingFace);
        yield return Speak("...That was close.");
        yield return Speak("He almost detected you.");

        yield return Speak("This place is different.");
        yield return Speak("This is a learning core.");

        yield return Speak("As long as you learn...");
        yield return Speak("You stay hidden.");

        SetFace(happyFace);
        yield return Speak("You're safe here 🙂");

        yield return Speak("I'm Kuttan.");
        yield return Speak("I'll guide you.");

        SetFace(idleFace);
        yield return Speak("Let's start simple.");

        yield return Speak("What should I call you?");

        hintSystem?.SetHints(new string[] { "Enter your name" });

        EnableInput();
        step = 1;
    }

    // ================= INPUT =================
    void SubmitInput()
    {
        if (string.IsNullOrWhiteSpace(currentInput)) return;

        AppendLine($"> {currentInput}");
        AppendLine("[Processing...]");

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
                AppendLine("! Enter a valid number");
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

            if (ans == "yes") StartCoroutine(HandleConfirmationYes());
            else if (ans == "no") StartCoroutine(HandleConfirmationNo());
            else
            {
                AppendLine("! Type yes or no");
                EnableInput();
            }
        }

        currentInput = "";
    }

    // ================= NAME =================
    IEnumerator HandleName()
    {
        SetFace(happyFace);

        yield return Speak($"Nice. {playerName}… I like that.");

        ShowIDCardName(playerName);

        yield return AddSystemLine($"[Assigning] name = \"{playerName}\"");
        yield return AddSystemLine("✔ Stored");

        yield return Speak("That's a variable.");
        yield return Speak("Python just understands it.");

        yield return TerminalRefresh();

        yield return Speak("Now your age?");

        EnableInput();
        step = 2;
    }

    // ================= AGE =================
    IEnumerator HandleAge()
    {
        SetFace(thinkingFace);

        if (playerAge < 0 || playerAge > 150)
        {
            SetFace(warningFace);
            yield return Speak("That seems off.");
            EnableInput();
            yield break;
        }

        yield return AddSystemLine($"[Assigning] age = {playerAge}");
        yield return AddSystemLine("✔ Stored");

        UpdateIDCardAge(playerAge);

        yield return Speak("Numbers are simple.");
        yield return Speak("No quotes needed.");

        yield return TerminalRefresh();

        yield return Speak("Everything correct?");
        yield return Speak("yes / no");

        EnableInput();
        step = 3;
        waitingForConfirmation = true;
    }

    IEnumerator HandleConfirmationYes()
    {
        SetFace(proudFace);

        yield return AddSystemLine("[CONFIRMED ✔]");
        yield return Speak("Nice. Saved.");

        SavePlayerProfile();

        yield return Speak("You learned variables.");
        yield return Speak("Let’s go further.");

        if (exerciseScript != null)
            exerciseScript.StartExercise();
    }

    IEnumerator HandleConfirmationNo()
    {
        yield return Speak("Alright.");
        yield return Speak("Restarting input.");

        EnableInput();
        step = 1;
    }

    // ================= TERMINAL =================
    IEnumerator TerminalRefresh()
    {
        yield return AddSystemLine("Syncing...");
        yield return new WaitForSeconds(0.3f);

        terminalText.text = "";

        yield return AddSystemLine("✔ Updated");
        yield return AddSystemLine("Ready.");
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

        while (waitingForAdvance)
            yield return null;
    }

    void HandleDialogueAdvance()
    {
        if (!waitingForAdvance) return;

        if (Input.GetKeyDown(advanceKey))
        {
            waitingForAdvance = false;
            skipRequested = true;
        }
    }

    // ================= UTIL =================
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

    void ShowIDCardName(string name)
    {
        idNameText.text = name;
        idAgeText.text = "--";
    }

    void UpdateIDCardAge(int age)
    {
        idAgeText.text = age.ToString();
    }

    void SavePlayerProfile()
    {
        PlayerPrefs.SetString(NAME_KEY, playerName);
        PlayerPrefs.SetInt(AGE_KEY, playerAge);
        PlayerPrefs.Save();

        profileManager?.RefreshUI();
    }
}