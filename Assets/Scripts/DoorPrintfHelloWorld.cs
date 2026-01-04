using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorPrintf_TerminalSystem : MonoBehaviour
{
    // ================= PLAYER =================
    public Transform player;
    public float interactDistance = 2.5f;

    // ================= BOARD =================
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;

    // ================= INPUT TERMINAL (UI) =================
    public GameObject inputTerminalPanel;
    public TextMeshProUGUI inputText;

    // ================= OUTPUT TERMINAL (3D) =================
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
        WrongDialogue,
        SuccessDialogue,
        Transition
    }

    GameState currentState = GameState.Idle;

    // ================= INTERNAL =================
    int dialogueIndex;
    int attemptCount;
    string currentInput = "";

    bool introCompleted = false;

    const string correctAnswer = "printf(\"Hello World\");";

    // ================= DIALOGUES =================

    string[] introDialogue =
 {
    "Hmm… this door looks locked.",
    "It reacts to something…",
    "I think it reacts to C programs.",
    "Maybe printing something will unlock it.",
    "In C, we use printf().",
    "Try printing: Hello World",
    "Press 1 to try."
};


    string[] teachingDialogue =
    {
        "In C, we use printf().",
        "printf prints text to the screen.",
        "Try printing Hello World."
    };

    string[] retryShortHints =
    {
        "Let’s try again.",
        "Give it another try.",
        "Focus on spelling.",
        "Press 1 when ready."
    };

    string[] wrongDialoguePool =
    {
        "Hmm… that didn’t work.",
        "Something feels off.",
        "That syntax isn’t right.",
        "The program didn’t run."
    };

    string[] wrongExplanation =
    {
        "C is very strict.",
        "Even small mistakes matter.",
        "Computers follow exact rules."
    };

    string[] successPerfect =
    {
        "Excellent!",
        "First try. Well done.",
        "That’s perfect C syntax.",
        "The door accepted your program.",
        "The door is now opened.",
        "Let’s go .."
    };

    string[] successGood =
    {
        "Good job!",
        "You figured it out.",
        "printf printed the text correctly.",
        "The door accepted your program.",
        "The door is now opened.",
        "Let’s go .."
    };

    string[] successOkay =
    {
        "You did it.",
        "Practice makes you better.",
        "printf printed the text correctly.",
        "The door accepted your program.",
        "The door is now opened.",
        "Let’s go .."
    };

    string[] activeDialogue;

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
            currentState == GameState.WrongDialogue ||
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
                boardText.text = retryShortHints[Random.Range(0, retryShortHints.Length)];
        }

        if (dist > interactDistance && currentState == GameState.Idle)
        {
            boardPanel.SetActive(false);
        }

        if (dist <= interactDistance && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!introCompleted)
                StartDialogue(teachingDialogue, GameState.TeachingDialogue);
            else
                OpenInputTerminal();
        }
    }

    // ================= DIALOGUE =================
    void StartDialogue(string[] dialogue, GameState nextState)
    {
        activeDialogue = dialogue;
        dialogueIndex = 0;
        currentState = nextState;
        boardText.text = activeDialogue[dialogueIndex];
    }

    void HandleDialogueAdvance()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            dialogueIndex++;

            if (dialogueIndex < activeDialogue.Length)
            {
                boardText.text = activeDialogue[dialogueIndex];
            }
            else
            {
                if (currentState == GameState.IntroDialogue)
                    introCompleted = true;

                if (currentState == GameState.TeachingDialogue)
                    OpenInputTerminal();
                else if (currentState == GameState.WrongDialogue)
                    currentState = GameState.Idle;
                else if (currentState == GameState.SuccessDialogue)
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
    }

    void HandleTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (currentInput.Length > 0)
                {
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    inputText.text = inputText.text.Substring(0, inputText.text.Length - 1);
                }
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
        outputTerminalText.text += "> " + currentInput + "\n";
        attemptCount++;

        if (currentInput.Trim() == correctAnswer)
            HandleSuccess();
        else
            HandleWrong();
    }

    void HandleWrong()
    {
        outputTerminalText.text += "Error\n\n";

        string[] combined =
        {
            wrongDialoguePool[Random.Range(0, wrongDialoguePool.Length)],
            wrongExplanation[Random.Range(0, wrongExplanation.Length)],
            "Press 1 and try again."
        };

        StartDialogue(combined, GameState.WrongDialogue);
    }

    // ================= SUCCESS =================
    void HandleSuccess()
    {
        outputTerminalText.text += "Hello World\n\n";

        if (attemptCount == 1)
            StartDialogue(successPerfect, GameState.SuccessDialogue);
        else if (attemptCount == 2)
            StartDialogue(successGood, GameState.SuccessDialogue);
        else
            StartDialogue(successOkay, GameState.SuccessDialogue);
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
