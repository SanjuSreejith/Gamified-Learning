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

    [Header("Completion Save")]
    public bool markSceneCompleted = true;

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

    string[] lockHints =
    {
        "Look at the platforms carefully.",
        "Each platform group represents a number.",
        "First platform set = first digit.",
        "Second platform set = second digit."
    };

    string[] pythonHints =
    {
        "input() reads what the user types.",
        "input() returns text.",
        "Convert text to number using int().",
        "Example: password = int(input())"
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

    readonly string[] botLockHints =
    {
        "Hmm… the password isn't written anywhere.",
        "Maybe the platforms around us mean something.",
        "Count the first platform group carefully.",
        "Then check the second one."
    };

    readonly string[] successDialogue =
    {
        "Correct.",
        "The lock accepted the number.",
        "The door is opening."
    };

    void Start()
    {
        if (boardPanel) boardPanel.SetActive(false);
        if (terminalPanel) terminalPanel.SetActive(false);
        if (lockPanel) lockPanel.SetActive(false);
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

        if (boardPanel) boardPanel.SetActive(false);
        if (terminalPanel) terminalPanel.SetActive(false);
        if (lockPanel) lockPanel.SetActive(false);

        dialogueActive = false;

        SetPaused(false);

        if (pythonConfigured)
            state = State.WaitingForTerminal;
        else
            state = State.Idle;
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
            if (!playerInside) return;

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

    int dialogueIndex;
    string[] activeDialogue;

    void StartDialogue(string[] dialogue)
    {
        activeDialogue = dialogue;
        dialogueIndex = 0;
        dialogueActive = true;

        if (boardPanel) boardPanel.SetActive(true);

        boardText.text = activeDialogue[dialogueIndex];

        AddToBacklog(activeDialogue[dialogueIndex]);

        StartCoroutine(DialogueRoutine());
    }

    IEnumerator DialogueRoutine()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));

        while (dialogueIndex < activeDialogue.Length - 1)
        {
            dialogueIndex++;

            boardText.text = activeDialogue[dialogueIndex];

            AddToBacklog(activeDialogue[dialogueIndex]);

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
        }

        dialogueActive = false;

        if (boardPanel) boardPanel.SetActive(false);

        if (state == State.Teaching)
            state = State.WaitingForTerminal;
    }

    void AddToBacklog(string text)
    {
        if (DialogueBacklogManager.Instance != null)
            DialogueBacklogManager.Instance.AddLine("Kuttan", text);
    }

    void SetPaused(bool pause)
    {
        if (playerMovementScript)
            playerMovementScript.enabled = !pause;
    }

    void OpenPythonTerminal()
    {
        if (terminalPanel) terminalPanel.SetActive(true);

        pythonInput = "";
        terminalText.text = "> ";
        state = State.TypingPython;

        SetPaused(true);

        DialogueBacklogManager.Instance?.AddLine("System", "Python terminal opened.");
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
        if (terminalPanel) terminalPanel.SetActive(false);

        SetPaused(false);

        if (RemoveSpaces(pythonInput) == RemoveSpaces(CORRECT_CODE))
        {
            pythonConfigured = true;

            DialogueBacklogManager.Instance?.AddLine("System", "Python code accepted.");

            PlaySound(correctSound);
            StartCoroutine(OpenLockAfterDialogue());
        }
        else
        {
            DialogueBacklogManager.Instance?.AddLine("System", "Python code rejected.");

            PlaySound(wrongSound);
            StartDialogue(wrongDialogue);
            state = State.WaitingForTerminal;
        }
    }

    IEnumerator OpenLockAfterDialogue()
    {
        StartDialogue(lockDialogue);

        yield return new WaitUntil(() => !dialogueActive);

        if (hintSystem)
        {
            hintSystem.SetHints(lockHints);
            hintSystem.EnableHints();
        }

        StartDialogue(botLockHints);

        yield return new WaitUntil(() => !dialogueActive);

        OpenLockTerminal();
    }

    void OpenLockTerminal()
    {
        if (lockPanel) lockPanel.SetActive(true);

        lockInput = "";
        lockText.text = "Enter Password";
        state = State.TypingLock;

        SetPaused(true);

        DialogueBacklogManager.Instance?.AddLine("System", "Door lock terminal opened.");
    }

    void CloseLockTerminal()
    {
        if (lockPanel) lockPanel.SetActive(false);

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
            DialogueBacklogManager.Instance?.AddLine("System", "Password accepted.");

            if (lockPanel) lockPanel.SetActive(false);

            SetPaused(false);

            StartCoroutine(SuccessSequence());
        }
        else
        {
            DialogueBacklogManager.Instance?.AddLine("System", "Incorrect password.");

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

        DialogueBacklogManager.Instance?.AddLine("System", "Door opened.");

        yield return new WaitForSeconds(1f);

        MarkSceneCompleted();

        SceneManager.LoadScene(nextSceneName);
    }

    void MarkSceneCompleted()
    {
        if (!markSceneCompleted) return;

        string sceneName = SceneManager.GetActiveScene().name;
        string key = "SceneCompleted_" + sceneName;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
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