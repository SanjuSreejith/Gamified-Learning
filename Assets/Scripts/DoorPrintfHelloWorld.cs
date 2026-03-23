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
    public AudioClip executeSound;
    public AudioClip errorSound;
    public AudioClip doorOpenSound;

    // ================= SCENE =================
    public string nextSceneName;

    // ================= TUTORIAL =================
    public bool tutorialActive = true;

    // ================= STATE =================
    enum GameState
    {
        Idle,
        Intro,
        Demo,
        Input,
        Feedback,
        Success,
        Transition
    }

    GameState currentState = GameState.Idle;

    // ================= INPUT =================
    string playerInput = "";
    bool isTyping = false;

    // ================= CURSOR =================
    bool cursorVisible = true;
    float cursorTimer = 0f;
    float cursorBlinkSpeed = 0.5f;

    // ================= DIALOGUE =================
    string[] currentDialogue;
    int dialogueIndex = 0;

    // ================= INTERNAL =================
    bool introPlayed = false;

    void Start()
    {
        boardPanel.SetActive(false);
        inputTerminalPanel.SetActive(false);

        fadeCanvas.alpha = 0;
        fadeCanvas.blocksRaycasts = false;

        ResetTerminal();
    }

    void Update()
    {
        if (tutorialActive) return;

        HandleDistance();

        if (currentState == GameState.Input)
        {
            HandleTyping();
            HandleCursorBlink();
        }

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
            "This door is controlled by an AI system...",
            "It listens to messages sent using Python.",
            "print() sends a message into the system.",
            "Only the correct message will trigger it.",
            "Watch this example...",
            "Press Enter..."
        };

        dialogueIndex = 0;
        ShowText(currentDialogue[dialogueIndex]);
    }

    void HandleDialogueAdvance()
    {
        if (!Input.GetKeyDown(KeyCode.Return)) return;

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
            StartCoroutine(PlayDemo());
        }
    }

    // ================= DEMO =================
    IEnumerator PlayDemo()
    {
        currentState = GameState.Demo;

        inputTerminalPanel.SetActive(true);

        playerInput = "";
        UpdateInputDisplay();

        yield return new WaitForSeconds(0.5f);

        string demoText = "Hello";

        foreach (char c in demoText)
        {
            playerInput += c;
            UpdateInputDisplay();
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);

        ResetTerminal();
        outputTerminalText.text += "> Executing...\n";

        yield return new WaitForSeconds(1f);

        outputTerminalText.text += "Output: Hello\n";

        yield return new WaitForSeconds(1.5f);

        inputTerminalPanel.SetActive(false);

        ShowText("See? print() sends a message.");

        yield return new WaitForSeconds(2f);

        ShowText("Now you try sending the correct message.");

        yield return new WaitForSeconds(1.5f);

        OpenInputTerminal();
    }

    // ================= INPUT =================
    void OpenInputTerminal()
    {
        currentState = GameState.Input;
        inputTerminalPanel.SetActive(true);

        playerInput = "";
        isTyping = true;

        UpdateInputDisplay();

        ShowText("Type a message to the door AI\nPress Enter to send");
    }

    void HandleTyping()
    {
        if (!isTyping) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b' && playerInput.Length > 0)
            {
                playerInput = playerInput.Remove(playerInput.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                isTyping = false;
                StartCoroutine(ExecuteCode());
                return;
            }
            else if (!char.IsControl(c))
            {
                playerInput += c;
            }
        }

        UpdateInputDisplay();
    }

    void HandleCursorBlink()
    {
        cursorTimer += Time.deltaTime;

        if (cursorTimer >= cursorBlinkSpeed)
        {
            cursorVisible = !cursorVisible;
            cursorTimer = 0f;
            UpdateInputDisplay();
        }
    }

    void UpdateInputDisplay()
    {
        string cursor = cursorVisible ? "|" : "";
        inputText.text = "> print(\"" + playerInput + cursor + "\")";
    }

    // ================= EXECUTION =================
    IEnumerator ExecuteCode()
    {
        currentState = GameState.Transition;

        inputTerminalPanel.SetActive(false);

        PlaySound(executeSound);

        ResetTerminal();
        outputTerminalText.text += "> Executing...\n";

        yield return new WaitForSeconds(1.2f);

        string cleanText = playerInput.Trim();

        outputTerminalText.text += "Output: " + cleanText + "\n";

        if (cleanText.ToLower().Contains("welcome"))
        {
            outputTerminalText.text += "Door AI: Message recognized\n";
            outputTerminalText.text += "Door AI: Opening access\n\n";

            StartCoroutine(HandleSuccess());
        }
        else
        {
            outputTerminalText.text += "Door AI: No valid response\n\n";
            HandleError(cleanText);
        }
    }

    // ================= ERROR =================
    void HandleError(string input)
    {
        PlaySound(errorSound);
        currentState = GameState.Feedback;

        currentDialogue = new string[]
        {
            "The door AI did not accept that...",
           
          "This door responds to greetings.",
"It opens only when it receives a proper welcome message.",
            "Press Enter..."
        };

        dialogueIndex = 0;
        ShowText(currentDialogue[dialogueIndex]);
    }

    // ================= SUCCESS =================
    IEnumerator HandleSuccess()
    {
        currentState = GameState.Success;

        ShowText("Access granted...");
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