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

    // ================= DIALOGUES =================
    string[] introDialogue =
    {
        "Hmm… this door looks locked.",
        "It reacts to C programs.",
        "Maybe printing something will unlock it.",
        "In C, we use printf().",
        "Try printing: Hello World",
        "Press 1 to try."
    };

    string[] teachingDialogue =
    {
        "printf prints text to the screen.",
        "Text must be inside double quotes.",
        "Statements end with a semicolon."
    };

    string[] successDialogue =
    {
        "Excellent.",
        "Your program compiled successfully.",
        "printf printed the text.",
        "The door accepted your program.",
        "The door is now opened.",
        "Let’s go."
    };

    // ================= START =================
    void Start()
    {
        boardPanel.SetActive(false);
        inputTerminalPanel.SetActive(false);

        fadeCanvas.alpha = 0;
        fadeCanvas.blocksRaycasts = false;

        outputTerminalText.text =
            "C OUTPUT TERMINAL\n" +
            "-----------------\n\n";
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

        List<string> errors = ValidatePrintf(currentInput);

        if (errors.Count == 0)
            HandleSuccess();
        else
            ShowErrors(errors);
    }

    // ================= VALIDATION (OPTIMIZED + MULTI ERROR) =================
    List<string> ValidatePrintf(string raw)
    {
        List<string> errors = new List<string>();
        string s = raw.Trim();

        // Semicolon
        if (!s.EndsWith(";"))
            errors.Add("Missing semicolon `;` at the end.");

        // printf existence & case
        if (s.StartsWith("Printf") || s.StartsWith("PRINTF"))
            errors.Add("C is case-sensitive. Use `printf`, not `Printf`.");

        if (!s.ToLower().Contains("printf"))
            errors.Add("Use the `printf` function to print text.");

        // Parentheses
        int open = s.IndexOf('(');
        int close = s.LastIndexOf(')');
        if (open == -1 || close == -1 || close < open)
            errors.Add("`printf` must use parentheses `()`.");

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
        outputTerminalText.text += "Compile Errors:\n\n";

        List<string> dialogue = new List<string>
        {
            "The program didn’t run.",
            "Here’s what needs fixing:"
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
