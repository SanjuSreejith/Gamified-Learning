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

    // ================= TUTORIAL LOCK =================
    [Header("Tutorial Lock")]
    public bool tutorialActive = true;

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

    void Update()
    {
        if (tutorialActive) return;

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

    void HandleDistance()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= interactDistance)
        {
            if (currentState == GameState.Idle)
            {
                boardPanel.SetActive(true);

                if (!introCompleted)
                    StartDialogue(introDialogue, GameState.IntroDialogue);
                else
                    boardText.text = "Press 1 to try again.";
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (!introCompleted)
                    StartDialogue(teachingDialogue, GameState.TeachingDialogue);
                else
                    OpenInputTerminal();
            }
        }
        else
        {
            if (currentState == GameState.Idle)
                boardPanel.SetActive(false);
        }
    }

    void StartDialogue(string[] dialogue, GameState state)
    {
        boardPanel.SetActive(true);

        activeDialogue = dialogue;
        dialogueIndex = 0;
        currentState = state;
        boardText.text = activeDialogue[dialogueIndex];

        backlogManager?.AddLine("Kuttan", activeDialogue[dialogueIndex]);

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
                botHintSystem?.DisableHints();
                OpenInputTerminal();
            }
            else if (currentState == GameState.SuccessDialogue)
            {
                botHintSystem?.DisableHints();
                StartCoroutine(FadeAndChangeScene());
            }
        }
    }

    void OpenInputTerminal()
    {
        currentInput = "";
        inputTerminalPanel.SetActive(true);
        inputText.text = "> ";
        currentState = GameState.InputTerminal;

        Time.timeScale = 0f;

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

    void SubmitInput()
    {
        inputTerminalPanel.SetActive(false);
        Time.timeScale = 1f;

        outputTerminalText.text += "> " + currentInput + "\n";
        attemptCount++;

        backlogManager?.AddLine("Kuttan", currentInput);

        List<string> errors = ValidatePythonPrint(currentInput);

        if (errors.Count == 0)
            HandleSuccess();
        else
            ShowErrors(errors);
    }

    List<string> ValidatePythonPrint(string raw)
    {
        List<string> errors = new List<string>();
        string s = raw.Trim();

        if (!s.StartsWith("print"))
            errors.Add("Use the `print()` function.");

        if (!s.Contains("(") || !s.Contains(")"))
            errors.Add("print must use parentheses.");

        if (!s.Contains("\""))
            errors.Add("Text must be inside quotes.");

        return errors;
    }

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
    }

    void HandleSuccess()
    {
        outputTerminalText.text += "Welcome\n\n";
        StartDialogue(successDialogue, GameState.SuccessDialogue);
    }

    void PlayTypingSound(char c)
    {
        if (char.IsLetter(c))
            audioSource.PlayOneShot(typeLetter);
        else if (c == ' ')
            audioSource.PlayOneShot(typeSpace);
        else
            audioSource.PlayOneShot(typeSymbol);
    }

    IEnumerator FadeAndChangeScene()
    {
        currentState = GameState.Transition;
        fadeCanvas.blocksRaycasts = true;

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