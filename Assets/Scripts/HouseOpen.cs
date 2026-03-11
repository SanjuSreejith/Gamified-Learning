using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorPythonInputLesson_Trigger : MonoBehaviour
{
    [Header("Bot Dialogue UI")]
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;

    [Header("Python Terminal")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    [Header("Lock Terminal")]
    public GameObject lockPanel;
    public TextMeshProUGUI lockText;

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

    const string CORRECT_CODE = "password = int(input())";
    const string LOCK_PASSWORD = "59";

    string[] pythonHints =
    {
        "input() reads what the user types.",
        "input() returns text.",
        "Convert text to number using int().",
        "Example: password = int(input())"
    };

    string[] lockHints =
    {
        "The password is a number.",
        "The correct number was mentioned earlier.",
        "Try entering 59."
    };

    readonly string[] teachingDialogue =
    {
        "Hey… this door is locked.",
        "Looks like a Python based lock.",
        "We need to take input from the user.",
        "input() always returns text.",
        "Numbers must be converted.",
        "We use int() for that.",
        "Example: password = int(input())",
        "Press 1 to open the Python terminal."
    };

    readonly string[] wrongDialogue =
    {
        "That didn’t work.",
        "input() returns text.",
        "Use int() to convert it.",
        "Press 1 and try again."
    };

    readonly string[] lockDialogue =
    {
        "Good. The program worked.",
        "Because of int(), the lock accepts numbers.",
        "Enter the password."
    };

    readonly string[] successDialogue =
    {
        "Correct.",
        "The lock accepted the number.",
        "The door is opening."
    };

    void Start()
    {
        boardPanel.SetActive(false);
        terminalPanel.SetActive(false);
        lockPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        if (state != State.Idle) return;

        state = State.Teaching;

        if (hintSystem)
        {
            hintSystem.SetHints(pythonHints);
            hintSystem.EnableHints();
        }

        StartDialogue(teachingDialogue);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;

        StopAllCoroutines();

        boardPanel.SetActive(false);
        terminalPanel.SetActive(false);
        lockPanel.SetActive(false);

        if (hintSystem)
            hintSystem.DisableHints();

        state = State.Idle;
        dialogueActive = false;

        SetPaused(false);
    }

    void Update()
    {
        if (dialogueActive) return;

        if (state == State.WaitingForTerminal)
        {
            if (playerInside && Input.GetKeyDown(KeyCode.Alpha1))
                OpenPythonTerminal();
        }

        if (state == State.TypingPython)
            HandlePythonTyping();

        if (state == State.TypingLock)
            HandleLockTyping();
    }

    int dialogueIndex;
    string[] activeDialogue;

    void StartDialogue(string[] dialogue)
    {
        activeDialogue = dialogue;
        dialogueIndex = 0;
        dialogueActive = true;

        boardPanel.SetActive(true);
        boardText.text = activeDialogue[dialogueIndex];

        StartCoroutine(DialogueRoutine());
    }

    IEnumerator DialogueRoutine()
    {
        yield return new WaitUntil(() =>
            Input.GetKeyDown(KeyCode.Return));

        while (dialogueIndex < activeDialogue.Length - 1)
        {
            dialogueIndex++;
            boardText.text = activeDialogue[dialogueIndex];

            yield return new WaitUntil(() =>
                Input.GetKeyDown(KeyCode.Return));
        }

        dialogueActive = false;
        boardPanel.SetActive(false);

        if (state == State.Teaching)
            state = State.WaitingForTerminal;
    }

    void SetPaused(bool pause)
    {
        if (playerMovementScript)
            playerMovementScript.enabled = !pause;
    }

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
            PlaySound(correctSound);

            if (hintSystem)
                hintSystem.SetHints(lockHints);

            StartCoroutine(OpenLockAfterDialogue());
        }
        else
        {
            PlaySound(wrongSound);
            StartDialogue(wrongDialogue);
            state = State.WaitingForTerminal;
        }
    }

    IEnumerator OpenLockAfterDialogue()
    {
        StartDialogue(lockDialogue);
        yield return new WaitUntil(() => !dialogueActive);
        OpenLockTerminal();
    }

    void OpenLockTerminal()
    {
        lockPanel.SetActive(true);
        lockInput = "";
        lockText.text = "Enter Password";
        state = State.TypingLock;

        SetPaused(true);
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
            SetPaused(false);

            StartCoroutine(SuccessSequence());
        }
        else
        {
            lockInput = "";
            lockText.text = "Enter Password";
        }
    }

    IEnumerator SuccessSequence()
    {
        StartDialogue(successDialogue);
        yield return new WaitUntil(() => !dialogueActive);

        if (hintSystem)
            hintSystem.DisableHints();

        PlaySound(doorOpenSound);

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(nextSceneName);
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