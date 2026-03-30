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

    [Header("Completion Save")]
    public bool markSceneCompleted = true;
    int pythonMistakes = 0;

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

    const int MAX_LOCK_LENGTH = 4;
    const int MAX_DISPLAY_LENGTH = 12;
    string dynamicPlaceholder = "Enter Password";

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

    readonly string[] botLockHints =
    {
        "Hmm… the password isn't written anywhere.",
        "Maybe the platforms around us mean something.",
        "Count the first platform group carefully.",
        "Then check the second one."
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

        // CRITICAL: Ensure lock panel starts disabled
        if (lockPanel != null)
        {
            lockPanel.SetActive(false);
            Debug.Log("[DoorTrigger] Lock panel initialized as disabled");
        }
        else
        {
            Debug.LogError("[DoorTrigger] LOCK PANEL IS NOT ASSIGNED in Inspector!");
        }

        if (escPanel) escPanel.SetActive(false);

        Debug.Log("[DoorTrigger] System initialized. State: " + state);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        Debug.Log("[DoorTrigger] Player entered trigger");

        if (state != State.Idle)
        {
            Debug.Log("[DoorTrigger] State not idle: " + state);
            return;
        }

        state = State.Teaching;
        Debug.Log("[DoorTrigger] State changed to: Teaching");

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
        Debug.Log("[DoorTrigger] Player exited trigger");

        StopAllCoroutines();

        if (boardPanel) boardPanel.SetActive(false);
        if (terminalPanel) terminalPanel.SetActive(false);
        if (lockPanel) lockPanel.SetActive(false);
        if (escPanel) escPanel.SetActive(false);

        dialogueActive = false;

        SetPaused(false);

        if (pythonConfigured)
            state = State.WaitingForTerminal;
        else
            state = State.Idle;
    }

    void Update()
    {
        if (!playerInside) return;
        if (dialogueActive) return;

        // Handle ESC key for closing lock panel
        if (state == State.TypingLock && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[DoorTrigger] ESC pressed - closing lock panel");
            CloseLockTerminal();
            return;
        }

        if (state == State.WaitingForTerminal)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[DoorTrigger] E pressed. PythonConfigured: " + pythonConfigured);

                if (!pythonConfigured)
                {
                    Debug.Log("[DoorTrigger] Opening Python terminal");
                    OpenPythonTerminal();
                }
                else
                {
                    Debug.Log("[DoorTrigger] Opening Lock terminal");
                    OpenLockTerminal();
                }
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

    void StartDialogue(string[] dialogue)
    {
        activeDialogue = dialogue;
        dialogueIndex = 0;
        dialogueActive = true;

        if (boardPanel) boardPanel.SetActive(true);
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
            if (boardPanel) boardPanel.SetActive(false);

            if (state == State.Teaching)
            {
                state = State.WaitingForTerminal;
                Debug.Log("[DoorTrigger] Teaching complete. State: WaitingForTerminal");
            }
        }
    }

    // ================= PYTHON =================

    void OpenPythonTerminal()
    {
        if (terminalPanel)
        {
            terminalPanel.SetActive(true);
            Debug.Log("[DoorTrigger] Python terminal opened");
        }

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

        terminalText.text = GetPythonDisplayText();
    }
    void SubmitPython()
    {
        if (terminalPanel) terminalPanel.SetActive(false);
        SetPaused(false);

        Debug.Log("[DoorTrigger] Python submitted: " + pythonInput);

        if (RemoveSpaces(pythonInput) == RemoveSpaces(CORRECT_CODE))
        {
            pythonConfigured = true;
            pythonMistakes = 0; // reset

            Debug.Log("[DoorTrigger] ✅ Python code accepted!");

            DialogueBacklogManager.Instance?.AddLine("System", "Python code accepted.");
            PlaySound(correctSound);
            StartCoroutine(OpenLockSequence());
        }
        else
        {
            pythonMistakes++; // 🔥 track mistakes

            Debug.Log("[DoorTrigger] ❌ Python code rejected");
            DialogueBacklogManager.Instance?.AddLine("System", "Python code rejected.");
            PlaySound(wrongSound);

            StartDialogue(wrongDialogue);
            state = State.WaitingForTerminal;
        }
    }

    string RemoveSpaces(string s)
    {
        return s.Replace(" ", "").ToLower();
    }

    // ================= LOCK =================

    IEnumerator OpenLockSequence()
    {
        Debug.Log("[DoorTrigger] Starting lock sequence");

        StartDialogue(lockDialogue);
        yield return new WaitUntil(() => !dialogueActive);

        if (hintSystem)
        {
            hintSystem.SetHints(lockHints);
            hintSystem.EnableHints();
        }

        StartDialogue(botLockHints);
        yield return new WaitUntil(() => !dialogueActive);

        // Small delay to ensure UI is ready
        yield return new WaitForSeconds(0.1f);

        OpenLockTerminal();
    }

    void OpenLockTerminal()
    {
        Debug.Log("=========================================");
        Debug.Log("[DoorTrigger] ⚡ OPENING LOCK TERMINAL ⚡");

        // CRITICAL CHECK
        if (lockPanel == null)
        {
            Debug.LogError("[DoorTrigger] ❌❌❌ LOCK PANEL IS NULL! Please assign in Inspector! ❌❌❌");
            return;
        }

        // Check if lock panel is in a Canvas
        Canvas canvas = lockPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DoorTrigger] ❌ Lock Panel is not under a Canvas! UI won't be visible!");
        }
        else
        {
            Debug.Log("[DoorTrigger] Lock Panel Canvas: " + canvas.name);
            Debug.Log("[DoorTrigger] Canvas Render Mode: " + canvas.renderMode);
        }

        // Enable lock panel
        lockPanel.SetActive(true);
        Debug.Log("[DoorTrigger] Lock Panel SetActive(true) - Active: " + lockPanel.activeSelf);
        Debug.Log("[DoorTrigger] Lock Panel Active in Hierarchy: " + lockPanel.activeInHierarchy);

        // Check if parent objects are enabled
        if (!lockPanel.activeInHierarchy)
        {
            Debug.LogWarning("[DoorTrigger] Lock Panel not active in hierarchy! Checking parents...");
            Transform parent = lockPanel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogWarning("[DoorTrigger] Parent '" + parent.name + "' is disabled. Enabling...");
                    parent.gameObject.SetActive(true);
                }
                parent = parent.parent;
            }

            // Try again
            lockPanel.SetActive(true);
            Debug.Log("[DoorTrigger] After fix - Active in Hierarchy: " + lockPanel.activeInHierarchy);
        }

        if (escPanel)
        {
            escPanel.SetActive(true);
            Debug.Log("[DoorTrigger] ESC Panel activated");
        }

        lockInput = "";
        lockText.text = dynamicPlaceholder;
        Debug.Log("[DoorTrigger] Lock text set to: " + lockText.text);

        state = State.TypingLock;
        SetPaused(true);

        Debug.Log("[DoorTrigger] State changed to: TypingLock");
        Debug.Log("=========================================");

        // Force focus on lock panel input
        StartCoroutine(ForceLockPanelFocus());
    }

    IEnumerator ForceLockPanelFocus()
    {
        yield return new WaitForEndOfFrame();

        if (lockPanel != null && lockPanel.activeInHierarchy)
        {
            // Try to find and focus input field
            TMP_InputField inputField = lockPanel.GetComponentInChildren<TMP_InputField>();
            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
                Debug.Log("[DoorTrigger] Input field focused");
            }
            else
            {
                Debug.Log("[DoorTrigger] No input field found, using text-based input");
            }
        }
    }
    string GetPythonDisplayText()
    {
        string display = "> " + pythonInput + "_\n";

        // 🔥 Progressive hint system
        if (pythonMistakes == 0)
        {
            display += "\n# use a variable";
            display += "\n# read input from user";
        }
        else if (pythonMistakes == 1)
        {
            display += "\n# use a variable (like password)";
            display += "\n# input() reads user input";
        }
        else if (pythonMistakes == 2)
        {
            display += "\n# input() gives text";
            display += "\n# convert it to number";
        }
        else
        {
            display += "\n# use int() to convert input";
            display += "\n# assign it to a variable";
        }

        return display;
    }
    void CloseLockTerminal()
    {
        Debug.Log("[DoorTrigger] Closing lock terminal");

        if (lockPanel) lockPanel.SetActive(false);
        if (escPanel) escPanel.SetActive(false);

        SetPaused(false);
        state = State.WaitingForTerminal;

        Debug.Log("[DoorTrigger] State changed to: WaitingForTerminal");
    }

    void HandleLockTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && lockInput.Length > 0)
            {
                lockInput = lockInput.Remove(lockInput.Length - 1);
                Debug.Log("[DoorTrigger] Backspace - Input: " + lockInput);
            }
            else if (c == '\n' || c == '\r')
            {
                SubmitLock();
                return;
            }
            else if (char.IsDigit(c))
            {
                if (lockInput.Length < MAX_LOCK_LENGTH)
                {
                    lockInput += c;
                    Debug.Log("[DoorTrigger] Digit entered: " + c + " - Current: " + lockInput);
                }
            }
        }

        if (lockInput.Length > 0)
            lockText.text = lockInput + "_";
        else
            lockText.text = dynamicPlaceholder;
    }

    void SubmitLock()
    {
        Debug.Log("[DoorTrigger] Lock submitted: " + lockInput);

        if (lockInput == LOCK_PASSWORD)
        {
            Debug.Log("[DoorTrigger] ✅ Correct password!");
            DialogueBacklogManager.Instance?.AddLine("System", "Password accepted.");

            if (lockPanel) lockPanel.SetActive(false);
            if (escPanel) escPanel.SetActive(false);

            SetPaused(false);
            StartCoroutine(SuccessSequence());
        }
        else
        {
            Debug.Log("[DoorTrigger] ❌ Incorrect password. Expected: " + LOCK_PASSWORD);
            DialogueBacklogManager.Instance?.AddLine("System", "Incorrect password.");

            lockInput = "";
            lockText.text = "❌ Wrong Password";
            StartCoroutine(ClearWrongMessage());
        }
    }

    IEnumerator ClearWrongMessage()
    {
        yield return new WaitForSeconds(1f);
        if (state == State.TypingLock)
        {
            lockText.text = dynamicPlaceholder;
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

        Debug.Log("[DoorTrigger] Scene marked as completed: " + sceneName);
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

    public bool IsInUIState()
    {
        return state == State.TypingPython ||
               state == State.TypingLock ||
               dialogueActive ||
               (boardPanel != null && boardPanel.activeSelf) ||
               (terminalPanel != null && terminalPanel.activeSelf) ||
               (lockPanel != null && lockPanel.activeSelf);
    }
}