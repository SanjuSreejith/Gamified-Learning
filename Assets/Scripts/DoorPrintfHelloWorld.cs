using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorPrintf_TerminalSystem : MonoBehaviour
{
    // ================= PLAYER =================
    public Transform player;
    public float interactDistance = 2.5f;

    // ================= UI =================
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;
    public TMPTypewriter typewriter;

    public GameObject inputTerminalPanel;
    public TextMeshProUGUI inputText;

    public TextMeshPro outputTerminalText;

    // ================= FADE =================
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1.2f;

    // ================= AUDIO =================
    public AudioSource audioSource;
    public AudioClip selectSound;
    public AudioClip executeSound;
    public AudioClip errorSound;
    public AudioClip doorOpenSound;

    // ================= SCENE =================
    public string nextSceneName;

    // ================= TUTORIAL =================
    [Header("Tutorial Lock")]
    public bool tutorialActive = true;

    // ================= STATE =================
    enum GameState
    {
        Idle,
        Intro,
        Input,
        Feedback,
        Success,
        Transition
    }

    GameState currentState = GameState.Idle;

    // ================= INPUT =================
    string[] wordOptions = { "\"Hello\"", "\"Welcome\"", "\"Open\"" };
    int currentOptionIndex = 0;

    // ================= DIALOGUE =================
    string[] currentDialogue;
    int dialogueIndex = 0;

    // ================= INTERNAL =================
    bool introPlayed = false;
    bool blockNextInputFrame = false;

    void Start()
    {
        boardPanel.SetActive(false);
        inputTerminalPanel.SetActive(false);

        fadeCanvas.alpha = 0;
        fadeCanvas.blocksRaycasts = false;

        outputTerminalText.text =
            "PYTHON TERMINAL\n" +
            "----------------\n\n";
    }

    void Update()
    {
        if (tutorialActive) return;

        HandleDistance();

        if (currentState == GameState.Input)
            HandleInput();

        if (currentState == GameState.Intro || currentState == GameState.Feedback)
            HandleDialogueAdvance();
    }

    // ================= DISTANCE =================
    void HandleDistance()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= interactDistance)
        {
            if (currentState == GameState.Idle)
            {
                boardPanel.SetActive(true);

                if (!introPlayed)
                    StartIntro();
                else
                    ShowText("Press E to interact");
            }

            if (Input.GetKeyDown(KeyCode.E) && currentState == GameState.Idle)
            {
                OpenInputTerminal();
            }
        }
        else
        {
            if (currentState == GameState.Idle)
                boardPanel.SetActive(false);
        }
    }

    // ================= TEXT =================
    void ShowText(string text)
    {
        if (typewriter != null)
            typewriter.Play(text);
        else
            boardText.text = text;
    }

    // ================= INTRO =================
    void StartIntro()
    {
        currentState = GameState.Intro;
        introPlayed = true;

        currentDialogue = new string[]
        {
            "This door reacts to messages...",
            "But it understands Python.",
            "Try sending something.",
            "Press Enter..."
        };

        dialogueIndex = 0;
        ShowText(currentDialogue[dialogueIndex]);
    }

    void HandleDialogueAdvance()
    {
        if (!Input.GetKeyDown(KeyCode.Return)) return;

        // Skip typing if still animating
        if (typewriter != null && typewriter.IsTyping())
        {
            typewriter.Skip();
            return;
        }

        dialogueIndex++;

        if (dialogueIndex < currentDialogue.Length)
        {
            ShowText(currentDialogue[dialogueIndex]);
        }
        else
        {
            OpenInputTerminal();
        }
    }

    // ================= INPUT =================
    void OpenInputTerminal()
    {
        currentState = GameState.Input;
        inputTerminalPanel.SetActive(true);

        currentOptionIndex = 0;
        UpdateInputDisplay();

        ShowText("A / D to change\nEnter to execute");

        blockNextInputFrame = true;
    }

    void HandleInput()
    {
        if (blockNextInputFrame)
        {
            blockNextInputFrame = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            currentOptionIndex--;
            if (currentOptionIndex < 0)
                currentOptionIndex = wordOptions.Length - 1;

            PlaySound(selectSound);
            UpdateInputDisplay();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            currentOptionIndex++;
            if (currentOptionIndex >= wordOptions.Length)
                currentOptionIndex = 0;

            PlaySound(selectSound);
            UpdateInputDisplay();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(ExecuteCode());
        }
    }

    void UpdateInputDisplay()
    {
        inputText.text = "> print(" + wordOptions[currentOptionIndex] + ")";
    }

    // ================= EXECUTION =================
    IEnumerator ExecuteCode()
    {
        currentState = GameState.Transition;

        inputTerminalPanel.SetActive(false);

        PlaySound(executeSound);

        // 🔥 Clear old output first
        ResetTerminal();

        // Show fresh execution
        outputTerminalText.text += "> Executing...\n";
        yield return new WaitForSeconds(1.2f);

        string selected = wordOptions[currentOptionIndex];
        string cleanText = selected.Replace("\"", "");

        // 🔥 Always show output
        outputTerminalText.text += "Output: " + cleanText + "\n";

        if (cleanText == "Welcome")
        {
            outputTerminalText.text += "Door: Accepted\n\n";
            StartCoroutine(HandleSuccess());
        }
        else
        {
            outputTerminalText.text += "Door: No response\n\n";
            HandleError(cleanText);
        }
    }

    // ================= ERROR =================
    void HandleError(string input)
    {
        PlaySound(errorSound);

        currentState = GameState.Feedback;

        if (input == "Hello")
        {
            currentDialogue = new string[]
            {
                "It printed 'Hello'...",
                "But door ignored it.",
                "Maybe wrong message.",
                "Try again."
            };
        }
        else if (input == "Open")
        {
            currentDialogue = new string[]
            {
                "Command sent...",
                "But nothing happened.",
                "It expects a message.",
                "Think again."
            };
        }
        else
        {
            currentDialogue = new string[]
            {
                "No response...",
                "That didn't work.",
                "Try something else.",
                "Press Enter..."
            };
        }

        dialogueIndex = 0;
        ShowText(currentDialogue[dialogueIndex]);
    }

    // ================= SUCCESS =================
    IEnumerator HandleSuccess()
    {
        currentState = GameState.Success;

        ShowText("Accepted...");
        yield return new WaitForSeconds(1f);

        ShowText("Door unlocking...");
        yield return new WaitForSeconds(1f);

        PlaySound(doorOpenSound);

        yield return new WaitForSeconds(1f);

        StartCoroutine(FadeAndChangeScene());
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

        SceneManager.LoadScene(nextSceneName);
    }

    // ================= AUDIO =================
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
    void ResetTerminal()
    {
        outputTerminalText.text =
            "PYTHON TERMINAL\n" +
            "----------------\n\n";
    }
}