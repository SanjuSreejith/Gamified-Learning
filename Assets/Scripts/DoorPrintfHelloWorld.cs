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

    // ================= PYTHON DIALOGUES =================
    string[] introDialogue =
    {
        "This door seems locked.",
        "It reacts to Python programs.",
        "Python prints text using print().",
        "Let’s try a basic program.",
        "Print: Hello World",
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
    }

    void HandleDialogueAdvance()
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetMouseButtonDown(0)) return;

        dialogueIndex++;

        if (dialogueIndex < activeDialogue.Length)
        {
            boardText.text = activeDialogue[dialogueIndex];
        }
        else
        {
            if (currentState == GameState.IntroDialogue)
                introCompleted = true;

            if (currentState == GameState.TeachingDialogue ||
                currentState == GameState.FeedbackDialogue)
                OpenInputTerminal();
            else if (currentState == GameState.SuccessDialogue)
                StartCoroutine(FadeAndChangeScene());
        }
    }

    // ================= INPUT TERMINAL =================
    void OpenInputTerminal()
    {
        currentInput = "";
        inputTerminalPanel.SetActive(true);
        inputText.text = "> ";
        currentState = GameState.InputTerminal;
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
        outputTerminalText.text += "> " + currentInput + "\n";
        attemptCount++;

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
            if (inside != "Hello World")
                errors.Add("The text must be exactly: Hello World.");
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
    }

    // ================= SUCCESS =================
    void HandleSuccess()
    {
        outputTerminalText.text += "Hello World\n\n";
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

    // ================= FADE =================
    IEnumerator FadeAndChangeScene()
    {
        currentState = GameState.Transition;
        fadeCanvas.blocksRaycasts = true;

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
