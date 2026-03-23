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
    public GameObject escPanel; // 👈 NEW

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

    const string CORRECT_CODE = "password = int(input())";
    const string LOCK_PASSWORD = "59";

    readonly string[] teachingDialogue =
    {
        "Hey… this door isn’t a normal lock.",
        "It’s controlled by a Python system.",
        "To open it, we need to take input from the user.",
        "But there’s a catch…",
        "input() gives text, not numbers.",
        "So we convert it using int().",
        "Like this: password = int(input())",
        "Press E to try it yourself."
    };

    readonly string[] wrongDialogue =
    {
        "Hmm… that’s not quite right.",
        "Remember — input() gives text.",
        "We must convert it using int().",
        "Try again."
    };

    readonly string[] lockDialogue =
    {
        "Nice. The system accepted your code.",
        "Now the lock can read numbers properly.",
        "Let’s enter the password."
    };

    readonly string[] successDialogue =
    {
        "Perfect.",
        "The system recognized the input.",
        "Door unlocking..."
    };

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

        StartDialogue(teachingDialogue);
    }

    void Update()
    {
        if (dialogueActive) return;

        if (state == State.TypingLock && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLockTerminal();
            return;
        }

        if (state == State.WaitingForTerminal)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && !pythonConfigured)
                OpenPythonTerminal();

            if (Input.GetKeyDown(KeyCode.E) && pythonConfigured)
                OpenLockTerminal();
        }

        if (state == State.TypingPython)
            HandlePythonTyping();

        if (state == State.TypingLock)
            HandleLockTyping();
    }

    // ================= DIALOGUE =================

    int dialogueIndex;
    string[] activeDialogue;

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
        if (typewriter != null)
            typewriter.Play(activeDialogue[dialogueIndex]);
        else
            boardText.text = activeDialogue[dialogueIndex];

        DialogueBacklogManager.Instance?.AddLine("Kuttan", activeDialogue[dialogueIndex]);

        StartCoroutine(WaitForNext());
    }

    IEnumerator WaitForNext()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));

        if (typewriter != null && typewriter.IsTyping())
        {
            typewriter.Skip();
            StartCoroutine(WaitForNext());
            yield break;
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
                state = State.WaitingForTerminal;
        }
    }

    // ================= PYTHON =================

    void OpenPythonTerminal()
    {
        terminalPanel.SetActive(true);

        pythonInput = "";
        terminalText.text = "> ";

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

        if (RemoveSpaces(pythonInput) == RemoveSpaces(CORRECT_CODE))
        {
            pythonConfigured = true;
            PlaySound(correctSound);
            StartCoroutine(OpenLockSequence());
        }
        else
        {
            PlaySound(wrongSound);
            StartDialogue(wrongDialogue);
            state = State.WaitingForTerminal;
        }
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
        if (escPanel) escPanel.SetActive(true); // 👈 SHOW ESC PANEL

        lockInput = "";
        lockText.text = "Enter Password";

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
                lockInput += c;
        }

        lockText.text = lockInput.Length > 0 ? lockInput + "_" : "Enter Password";
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
            lockText.text = "Wrong Password";
        }
    }

    IEnumerator SuccessSequence()
    {
        StartDialogue(successDialogue);
        yield return new WaitUntil(() => !dialogueActive);

        PlaySound(doorOpenSound);

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(nextSceneName);
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

    string RemoveSpaces(string s)
    {
        return s.Replace(" ", "");
    }
}