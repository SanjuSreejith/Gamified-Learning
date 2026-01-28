using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorPythonInputLesson_Trigger : MonoBehaviour
{
    /* ================= BOT UI ================= */
    [Header("Bot UI")]
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;

    /* ================= PYTHON TERMINAL ================= */
    [Header("Python Terminal")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    /* ================= LOCK TERMINAL ================= */
    [Header("Lock Terminal")]
    public GameObject lockPanel;
    public TextMeshProUGUI lockText;

    /* ================= AUDIO ================= */
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip doorOpenSound;

    /* ================= SCENE ================= */
    public string nextSceneName;

    /* ================= STATE ================= */
    enum State
    {
        Idle,
        Teaching,
        WaitingForTerminal,
        TypingPython,
        TypingLock,
        Transition
    }

    State state = State.Idle;

    bool playerInside;
    bool dialogueActive;

    string pythonInput = "";
    string lockInput = "";

    const string CORRECT_CODE = "password = int(input())";
    const string LOCK_PASSWORD = "59";

    /* ================= DIALOGUES ================= */

    string[] teachingDialogue =
    {
        "Hey… this door is locked.",
        "Looks like a Python based lock.",
        "We need to take input from the user.",
        "In Python, input() always gives TEXT.",
        "Even if you type a number, it is still text.",
        "So we convert it to a number.",
        "This is called TYPE CASTING.",
        "We use int() for that.",
        "Example:",
        "password = int(input())",
        "Press 1 to open the Python terminal."
    };

    string[] wrongDialogue =
    {
        "That didn’t work.",
        "input() alone gives text.",
        "Numbers must be converted using int().",
        "Check spelling and brackets.",
        "Press 1 and try again."
    };

    string[] lockDialogue =
    {
        "Good. The program worked.",
        "Because of int(), the lock accepts numbers.",
        "Now enter the password."
    };

    string[] successDialogue =
    {
        "Yes… that’s correct.",
        "The lock accepted the number.",
        "Before you go inside—",
        "Turn off your headlight.",
        "It could give you away.",
        "I’ll wait outside.",
        "Be careful."
    };

    /* ================= START ================= */
    void Start()
    {
        boardPanel.SetActive(false);
        terminalPanel.SetActive(false);
        lockPanel.SetActive(false);

        terminalText.text =
            "PYTHON TERMINAL\n" +
            "---------------\n\n> ";

        state = State.Idle;
    }

    /* ================= TRIGGER ================= */
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;

        if (state == State.Idle)
        {
            state = State.Teaching;
            StartDialogue(teachingDialogue);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
    }

    /* ================= UPDATE ================= */
    void Update()
    {
        if (dialogueActive) return;

        if (state == State.WaitingForTerminal && playerInside)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                OpenPythonTerminal();
        }
        else if (state == State.TypingPython)
        {
            HandlePythonTyping();
        }
        else if (state == State.TypingLock)
        {
            HandleLockTyping();
        }
    }

    /* ================= DIALOGUE ================= */
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
        // wait for any previous Enter to be released
        yield return new WaitUntil(() => !Input.GetKey(KeyCode.Return));

        while (dialogueIndex < activeDialogue.Length)
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));

            dialogueIndex++;

            if (dialogueIndex < activeDialogue.Length)
            {
                boardText.text = activeDialogue[dialogueIndex];
                yield return new WaitUntil(() => !Input.GetKey(KeyCode.Return));
            }
        }

        dialogueActive = false;

        if (state == State.Teaching)
            state = State.WaitingForTerminal;
    }

    /* ================= PYTHON TERMINAL ================= */
    void OpenPythonTerminal()
    {
        boardPanel.SetActive(false);
        terminalPanel.SetActive(true);
        pythonInput = "";
        state = State.TypingPython;
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

            terminalText.text = "> " + pythonInput + "_";
        }
    }

    void SubmitPython()
    {
        terminalPanel.SetActive(false);

        if (Clean(pythonInput) == Clean(CORRECT_CODE))
        {
            if (audioSource && correctSound)
                audioSource.PlayOneShot(correctSound);

            StartDialogue(lockDialogue);
            OpenLockTerminal();
        }
        else
        {
            if (audioSource && wrongSound)
                audioSource.PlayOneShot(wrongSound);

            StartDialogue(wrongDialogue);
            state = State.WaitingForTerminal;
        }
    }

    /* ================= LOCK TERMINAL ================= */
    void OpenLockTerminal()
    {
        lockPanel.SetActive(true);
        lockInput = "";
        lockText.text = "> _";
        state = State.TypingLock;
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

            lockText.text = "> " + lockInput + "_";
        }
    }

    void SubmitLock()
    {
        if (lockInput == LOCK_PASSWORD)
        {
            lockPanel.SetActive(false);
            StartCoroutine(SuccessSequence());
        }
        else
        {
            lockInput = "";
            lockText.text = "> _";
        }
    }

    /* ================= SUCCESS ================= */
    IEnumerator SuccessSequence()
    {
        StartDialogue(successDialogue);

        yield return new WaitUntil(() => !dialogueActive);

        if (audioSource && doorOpenSound)
            audioSource.PlayOneShot(doorOpenSound);

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(nextSceneName);
    }

    /* ================= HELPERS ================= */
    string Clean(string s)
    {
        return s.Replace(" ", "").ToLower();
    }
}
