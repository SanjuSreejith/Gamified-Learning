using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class DoorPrintf_TerminalSystem : MonoBehaviour
{
    // ================= PLAYER =================
    public Transform player;
    public float interactDistance = 2.5f;

    // ================= BOARD =================
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;

    // ================= INPUT TERMINAL =================
    public GameObject inputTerminalPanel;
    public TextMeshProUGUI inputText;

    // ================= OUTPUT TERMINAL =================
    public TextMeshPro outputTerminalText;

    // ================= FADE =================
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1.2f;

    // ================= AUDIO =================
    public AudioSource audioSource;
    public AudioClip typeLetter;
    public AudioClip typeSymbol;
    public AudioClip typeSpace;
    public AudioClip doorOpenSound;

    // ================= SCENE =================
    public string nextSceneName;

    // ================= HINT SYSTEM =================
    public BotHintSystem botHintSystem;

    // ================= DIALOGUE BACKLOG =================
    public DialogueBacklogManager backlogManager;

    // ================= STATE =================
    enum GameState
    {
        Idle,
        IntroDialogue,
        TeachingDialogue,
        InputTerminal,
        FeedbackDialogue,
        SuccessDialogue,
        Transition
    }

    GameState currentState = GameState.Idle;

    // ================= INTERNAL =================
    int dialogueIndex;
    int attemptCount;
    string currentInput = "";
    bool introCompleted;
    string[] activeDialogue;

    // ================= HINTS =================
    string[] teachingHints = {
        "Python uses print()",
        "Text must be inside double quotes",
        "No semicolons at the end"
    };

    string[] inputTerminalHints = {
        "Type: print(\"Welcome\")",
        "Remember double quotes",
        "Press Enter to submit"
    };

    string[] feedbackHints = {
        "Check your syntax",
        "Use print with parentheses",
        "Text must be exactly 'Welcome'"
    };

    string[] successHints = {
        "Correct! The door will open",
        "You can now proceed"
    };

    // ================= PYTHON DIALOGUES =================
    string[] introDialogue =
    {
        "This door seems locked.",
        "It reacts to Python programs.",
        "Python prints text using print().",
        "Let’s try a basic program.",
        "Print:Welcome",
        "Press 1 to begin."
    };

    string[] teachingDialogue =
    {
        "Python uses the print() function.",
        "Text must be inside double quotes.",
        "Python does NOT use semicolons."
    };

    string[] successDialogue =
    {
        "Perfect.",
        "Your Python code executed successfully.",
        "print() displayed the text.",
        "The door accepted your program.",
        "The door is opening.",
        "Proceed."
    };

    // ================= START =================
    void Start()
    {
        boardPanel.SetActive(false);
        inputTerminalPanel.SetActive(false);

        fadeCanvas.alpha = 0;
        fadeCanvas.blocksRaycasts = false;

        outputTerminalText.text =
            "PYTHON OUTPUT TERMINAL\n" +
            "----------------------\n\n";

        if (botHintSystem == null)
            botHintSystem = FindObjectOfType<BotHintSystem>();

        if (backlogManager == null)
            backlogManager = FindObjectOfType<DialogueBacklogManager>();
    }

    // ================= UPDATE =================
    void Update()
    {
        HandleDistance();

        if (currentState == GameState.InputTerminal)
            HandleTyping();

        if (currentState == GameState.IntroDialogue ||
            currentState == GameState.TeachingDialogue ||
            currentState == GameState.FeedbackDialogue ||
            currentState == GameState.SuccessDialogue)
        {
            HandleDialogueAdvance();
        }
    }

    // ================= DISTANCE =================
    void HandleDistance()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= interactDistance && currentState == GameState.Idle)
        {
            boardPanel.SetActive(true);

            if (!introCompleted)
                StartDialogue(introDialogue, GameState.IntroDialogue);
            else
                boardText.text = "Press 1 to try again.";
        }

        if (dist > interactDistance && currentState == GameState.Idle)
            boardPanel.SetActive(false);

        if (dist <= interactDistance && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!introCompleted)
                StartDialogue(teachingDialogue, GameState.TeachingDialogue);
            else
                OpenInputTerminal();
        }
    }

    // ================= DIALOGUE =================
    void StartDialogue(string[] dialogue, GameState state)
    {
        activeDialogue = dialogue;
        dialogueIndex = 0;
        currentState = state;
        boardText.text = activeDialogue[dialogueIndex];

        // Add first line to backlog (speaker: Kuttan)
        backlogManager?.AddLine("Kuttan", activeDialogue[dialogueIndex]);

        // Set hints for teaching dialogue
        if (state == GameState.TeachingDialogue && botHintSystem != null)
        {
            botHintSystem.SetHints(teachingHints);
            botHintSystem.EnableHints();
        }
    }

    void HandleDialogueAdvance()
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetMouseButtonDown(0)) return;

        dialogueIndex++;

        if (dialogueIndex < activeDialogue.Length)
        {
            boardText.text = activeDialogue[dialogueIndex];
            backlogManager?.AddLine("Kuttan", activeDialogue[dialogueIndex]);
        }
        else
        {
            if (currentState == GameState.IntroDialogue)
                introCompleted = true;

            if (currentState == GameState.TeachingDialogue ||
                currentState == GameState.FeedbackDialogue)
            {
                // Disable hints before opening input terminal
                botHintSystem?.DisableHints();
                OpenInputTerminal();
            }
            else if (currentState == GameState.SuccessDialogue)
            {
                // Disable hints before fading
                botHintSystem?.DisableHints();
                StartCoroutine(FadeAndChangeScene());
            }
        }
    }

    // ================= INPUT TERMINAL =================
    void OpenInputTerminal()
    {
        currentInput = "";
        inputTerminalPanel.SetActive(true);
        inputText.text = "> ";
        currentState = GameState.InputTerminal;

        // Pause the game
        Time.timeScale = 0f;

        // Set hints for input terminal
        if (botHintSystem != null)
        {
            botHintSystem.SetHints(inputTerminalHints);
            botHintSystem.EnableHints();
        }
    }

    void HandleTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && currentInput.Length > 0)
            {
                currentInput = currentInput[..^1];
                inputText.text = inputText.text[..^1];
            }
            else if (c == '\n' || c == '\r')
            {
                SubmitInput();
            }
            else
            {
                currentInput += c;
                inputText.text += c;
                PlayTypingSound(c);
            }
        }
    }

    // ================= SUBMIT =================
    void SubmitInput()
    {
        inputTerminalPanel.SetActive(false);

        // Unpause the game
        Time.timeScale = 1f;

        outputTerminalText.text += "> " + currentInput + "\n";
        attemptCount++;

        // Add player input to backlog (speaker: Kuttan)
        backlogManager?.AddLine("Kuttan", currentInput);

        List<string> errors = ValidatePythonPrint(currentInput);

        if (errors.Count == 0)
            HandleSuccess();
        else
            ShowErrors(errors);
    }

    // ================= PYTHON VALIDATION =================
    List<string> ValidatePythonPrint(string raw)
    {
        List<string> errors = new List<string>();
        string s = raw.Trim();

        // Semicolon check
        if (s.EndsWith(";"))
            errors.Add("Python does not use semicolons.");

        // print() existence & case
        if (s.StartsWith("Print") || s.StartsWith("PRINT"))
            errors.Add("Python is case-sensitive. Use `print`, not `Print`.");

        if (!s.StartsWith("print"))
            errors.Add("Use the `print()` function.");

        // Parentheses
        int open = s.IndexOf('(');
        int close = s.LastIndexOf(')');
        if (open == -1 || close == -1 || close < open)
            errors.Add("print must use parentheses `()`.");

        // Quotes
        int quoteCount = 0;
        foreach (char c in s)
            if (c == '"') quoteCount++;

        if (quoteCount == 0)
            errors.Add("Text must be inside double quotes.");
        else if (quoteCount == 1)
            errors.Add("Missing one double quote `\"`.");

        // Content
        if (quoteCount >= 2)
        {
            int q1 = s.IndexOf('"');
            int q2 = s.LastIndexOf('"');
            string inside = s.Substring(q1 + 1, q2 - q1 - 1);
            if (inside != "Welcome")
                errors.Add("The text must be exactly: Welcome!");
        }

        return errors;
    }

    // ================= FEEDBACK =================
    void ShowErrors(List<string> errors)
    {
        outputTerminalText.text += "Runtime Errors:\n\n";

        List<string> dialogue = new List<string>
        {
            "The Python program failed.",
            "Issues detected:"
        };

        foreach (string err in errors)
            dialogue.Add("• " + err);

        dialogue.Add("Fix them and try again.");

        activeDialogue = dialogue.ToArray();
        dialogueIndex = 0;
        currentState = GameState.FeedbackDialogue;
        boardText.text = activeDialogue[dialogueIndex];

        // Add first error line to backlog
        backlogManager?.AddLine("Kuttan", activeDialogue[dialogueIndex]);

        // Set hints for feedback
        if (botHintSystem != null)
        {
            botHintSystem.SetHints(feedbackHints);
            botHintSystem.EnableHints();
        }
    }

    // ================= SUCCESS =================
    void HandleSuccess()
    {
        outputTerminalText.text += "Welcome\n\n";

        // Set hints for success
        if (botHintSystem != null)
        {
            botHintSystem.SetHints(successHints);
            botHintSystem.EnableHints();
        }

        StartDialogue(successDialogue, GameState.SuccessDialogue);
    }

    // ================= SOUND =================
    void PlayTypingSound(char c)
    {
        if (char.IsLetter(c))
            audioSource.PlayOneShot(typeLetter);
        else if (c == ' ')
            audioSource.PlayOneShot(typeSpace);
        else
            audioSource.PlayOneShot(typeSymbol);
    }

    // ================= SCENE COMPLETION =================
    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        PlayerPrefs.Save();
        Debug.Log("Scene marked as completed: " + sceneName);
    }

    // ================= FADE =================
    IEnumerator FadeAndChangeScene()
    {
        currentState = GameState.Transition;
        fadeCanvas.blocksRaycasts = true;

        // Mark this scene as completed before leaving
        MarkSceneCompleted();

        // Ensure time is unpaused (in case something went wrong)
        Time.timeScale = 1f;

        float t = 0;
        while (t < fadeDuration)
        {
            fadeCanvas.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        audioSource.PlayOneShot(doorOpenSound);
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(nextSceneName);
    }
}