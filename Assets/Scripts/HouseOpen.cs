using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorPythonInputLesson_Trigger : MonoBehaviour
{
    [Header("Bot Dialogue UI")]
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;
    public TMPTypewriter typewriter;

    [Header("Python Terminal")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    [Header("Lock Terminal")]
    public GameObject lockPanel;
    public TextMeshProUGUI lockText;

    [Header("ESC Hint Panel")]
    public GameObject escPanel;

    [Header("Hint System")]
    public BotHintSystem hintSystem;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip doorOpenSound;

    [Header("Scene Transition")]
    public string nextSceneName;

    [Header("Player Control")]
    public MonoBehaviour playerMovementScript;

    enum State
    {
        Idle,
        Teaching,
        WaitingForTerminal,
        TypingPython,
        TypingLock
    }

    State state = State.Idle;

    bool playerInside;
    bool dialogueActive;

    string pythonInput = "";
    string lockInput = "";

    bool pythonConfigured = false;

    const string LOCK_PASSWORD = "59";

    const int MAX_LOCK_LENGTH = 4;
    const int MAX_DISPLAY_LENGTH = 12;
    string dynamicPlaceholder = "Enter Code";

    // ================= HINTS =================

    string[] teachingHints = {
        "Press Enter to continue",
        "Follow the explanation carefully"
    };

    string[] waitingHints = {
        "Press E to interact",
        "Open the terminal"
    };

    string[] pythonHints = {
        "Use: password = int(input())",
        "input() gives text",
        "Convert it using int()"
    };

    string[] lockHints = {
        "Enter the password",
        "Use numbers only"
    };

    // ================= UNITY =================

    void Start()
    {
        boardPanel.SetActive(false);
        terminalPanel.SetActive(false);
        lockPanel.SetActive(false);

        if (escPanel) escPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        if (state != State.Idle) return;

        state = State.Teaching;

        hintSystem.SetHints(teachingHints);
        hintSystem.EnableHints();

        StartDialogue(teachingDialogue);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        hintSystem.DisableHints();
    }

    void Update()
    {
        if (!playerInside) return;
        if (dialogueActive) return;

        if (state == State.TypingLock && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLockTerminal();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (state == State.WaitingForTerminal)
            {
                if (!pythonConfigured)
                    OpenPythonTerminal();
                else
                    OpenLockTerminal();
            }
        }

        if (state == State.TypingPython)
            HandlePythonTyping();

        if (state == State.TypingLock)
            HandleLockTyping();
    }

    // ================= DIALOGUE =================

    int dialogueIndex;
    string[] activeDialogue;

    readonly string[] teachingDialogue =
    {
        "Hey… this door isn’t a normal lock.",
        "It’s controlled by a Python system.",
        "To open it, we need input from user.",
        "But input() gives text.",
        "So we use int(input()).",
        "Like: password = int(input())",
        "Press E to try."
    };

    readonly string[] wrongDialogue =
    {
        "Not correct.",
        "Use int(input())",
        "Try again."
    };

    readonly string[] lockDialogue =
    {
        "Nice. System accepted.",
        "Now enter password."
    };

    readonly string[] successDialogue =
    {
        "Perfect.",
        "Door unlocking..."
    };

    void StartDialogue(string[] dialogue)
    {
        activeDialogue = dialogue;
        dialogueIndex = 0;
        dialogueActive = true;

        boardPanel.SetActive(true);
        ShowLine();
    }

    void ShowLine()
    {
        StopAllCoroutines();

        if (typewriter != null)
            typewriter.Play(activeDialogue[dialogueIndex]);
        else
            boardText.text = activeDialogue[dialogueIndex];

        StartCoroutine(WaitForNext());
    }

    IEnumerator WaitForNext()
    {
        while (true)
        {
            yield return null;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (typewriter != null && typewriter.IsTyping())
                    typewriter.Skip();
                else
                    break;
            }
        }

        dialogueIndex++;

        if (dialogueIndex < activeDialogue.Length)
        {
            ShowLine();
        }
        else
        {
            dialogueActive = false;
            boardPanel.SetActive(false);

            if (state == State.Teaching)
            {
                state = State.WaitingForTerminal;
                hintSystem.SetHints(waitingHints);
            }
        }
    }

    // ================= PYTHON =================

    void OpenPythonTerminal()
    {
        terminalPanel.SetActive(true);

        pythonInput = "";
        terminalText.text = "> ";

        hintSystem.SetHints(pythonHints);

        state = State.TypingPython;
        SetPaused(true);
    }

    void HandlePythonTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && pythonInput.Length > 0)
                pythonInput = pythonInput.Remove(pythonInput.Length - 1);

            else if (c == '\n' || c == '\r')
            {
                SubmitPython();
                return;
            }
            else if (!char.IsControl(c))
                pythonInput += c;
        }

        terminalText.text = "> " + pythonInput + "_";
    }

    void SubmitPython()
    {
        terminalPanel.SetActive(false);
        SetPaused(false);

        if (IsValidPythonInput(pythonInput))
        {
            pythonConfigured = true;
            dynamicPlaceholder = ExtractInputPlaceholder(pythonInput);

            PlaySound(correctSound);
            hintSystem.SetHints(lockHints);

            StartCoroutine(OpenLockSequence());
        }
        else
        {
            PlaySound(wrongSound);
            StartDialogue(wrongDialogue);
            state = State.WaitingForTerminal;
        }
    }

    bool IsValidPythonInput(string input)
    {
        string s = input.Replace(" ", "").ToLower();
        return s.Contains("password=int(input(");
    }

    // ================= LOCK =================

    IEnumerator OpenLockSequence()
    {
        StartDialogue(lockDialogue);
        yield return new WaitUntil(() => !dialogueActive);
        OpenLockTerminal();
    }

    void OpenLockTerminal()
    {
        lockPanel.SetActive(true);
        if (escPanel) escPanel.SetActive(true);

        lockInput = "";
        lockText.text = LimitText(dynamicPlaceholder);

        state = State.TypingLock;
        SetPaused(true);
    }

    void CloseLockTerminal()
    {
        lockPanel.SetActive(false);
        if (escPanel) escPanel.SetActive(false);

        SetPaused(false);
        state = State.WaitingForTerminal;
    }

    void HandleLockTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && lockInput.Length > 0)
                lockInput = lockInput.Remove(lockInput.Length - 1);

            else if (c == '\n' || c == '\r')
            {
                SubmitLock();
                return;
            }
            else if (char.IsDigit(c))
            {
                if (lockInput.Length < MAX_LOCK_LENGTH)
                    lockInput += c;
            }
        }

        lockText.text = lockInput.Length > 0
            ? LimitText(lockInput + "_")
            : LimitText(dynamicPlaceholder);
    }

    void SubmitLock()
    {
        if (lockInput == LOCK_PASSWORD)
        {
            lockPanel.SetActive(false);
            if (escPanel) escPanel.SetActive(false);

            SetPaused(false);
            StartCoroutine(SuccessSequence());
        }
        else
        {
            lockInput = "";
            lockText.text = LimitText("❌ Wrong");
            StartCoroutine(ClearLockAfterDelay());
        }
    }

    IEnumerator ClearLockAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        lockInput = "";
        lockText.text = LimitText(dynamicPlaceholder);
    }

    IEnumerator SuccessSequence()
    {
        StartDialogue(successDialogue);
        yield return new WaitUntil(() => !dialogueActive);

        PlaySound(doorOpenSound);
        yield return new WaitForSeconds(1f);

        // ✅ MARK SCENE COMPLETED
        MarkSceneCompleted();

        SceneManager.LoadScene(nextSceneName);
    }

    // ================= SAVE =================

    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("SceneCompleted_" + sceneName, 1);
        PlayerPrefs.Save();
    }

    // ================= UTILS =================

    void SetPaused(bool pause)
    {
        if (playerMovementScript)
            playerMovementScript.enabled = !pause;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }

    string LimitText(string text)
    {
        return text.Length > MAX_DISPLAY_LENGTH
            ? text.Substring(0, MAX_DISPLAY_LENGTH)
            : text;
    }

    string ExtractInputPlaceholder(string input)
    {
        int start = input.IndexOf("input(");
        if (start == -1) return "Enter Code";

        int firstQuote = input.IndexOf('"', start);
        int secondQuote = input.IndexOf('"', firstQuote + 1);

        if (firstQuote != -1 && secondQuote != -1)
        {
            string extracted = input.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
            if (!string.IsNullOrEmpty(extracted))
                return extracted;
        }

        return "Enter Code";
    }
}