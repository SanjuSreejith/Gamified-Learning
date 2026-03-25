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
    bool demoPlayed = false;  // NEW: Track if demo has been played
    bool isExecuting = false;
    int errorCount = 0;

    // ================= DIALOGUE VARIATIONS FOR ERRORS =================
    string[] errorDialogue1 = new string[]
    {
        "Hmm... the door AI didn't respond to that.",
        "It seems to only react to greetings.",
        "Try sending a welcome message.",
        "Press Enter to try again..."
    };

    string[] errorDialogue2 = new string[]
    {
        "Still not working...",
        "The door AI is expecting a specific greeting.",
        "Maybe try 'Hello', 'Welcome', or 'Greetings'?",
        "Press Enter to try again..."
    };

    string[] errorDialogue3 = new string[]
    {
        "Let me help you...",
        "The AI is programmed to respond to 'welcome'.",
        "Try typing exactly: welcome",
        "Press Enter to try again..."
    };

    string[] errorDialogue4 = new string[]
    {
        "One more hint:",
        "The message needs to be a greeting.",
        "Examples: 'Welcome', 'Hello', 'Greetings'",
        "Type one of these and press Enter...",
        "Press Enter to continue..."
    };

    string[] errorDialogueFinal = new string[]
    {
        "The correct message is 'welcome'.",
        "Type it exactly like this: welcome",
        "Then press Enter to send.",
        "Press Enter to try..."
    };

    void Start()
    {
        boardPanel.SetActive(false);
        inputTerminalPanel.SetActive(false);

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0;
            fadeCanvas.blocksRaycasts = false;
        }

        ResetTerminal();

        // Add to backlog when scene starts
        AddToBacklog("System", "Door terminal system initialized");

        // Check if demo was already played
        demoPlayed = PlayerPrefs.GetInt("DemoPlayed_" + SceneManager.GetActiveScene().name, 0) == 1;
    }

    void Update()
    {
        if (tutorialActive) return;

        HandleDistance();

        if (currentState == GameState.Input && !isExecuting)
        {
            HandleTyping();
            HandleCursorBlink();
        }

        if ((currentState == GameState.Intro || currentState == GameState.Feedback) && !isExecuting)
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

        // Add to backlog
        AddToBacklog("Door System", text);
    }

    // ================= INTRO =================
    void StartIntro()
    {
        currentState = GameState.Intro;
        introPlayed = true;

        if (!demoPlayed)
        {
            // Full intro with demo
            currentDialogue = new string[]
            {
                "This door is controlled by an AI system...",
                "It listens to messages sent using Python.",
                "print() sends a message into the system.",
                "Only the correct message will trigger it.",
                "Watch this example...",
                "Press Enter..."
            };
        }
        else
        {
            // Short intro without demo
            currentDialogue = new string[]
            {
                "Welcome back!",
                "Remember to use print() to send messages.",
                "Type the correct greeting to open the door.",
                "Press Enter to continue..."
            };
        }

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
            if (!demoPlayed)
            {
                // Play demo only if it hasn't been played
                StartCoroutine(PlayDemo());
            }
            else
            {
                // Skip demo and go straight to input
                OpenInputTerminal();
            }
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
        AddToBacklog("Terminal", "Executing: print(\"Hello\")");

        yield return new WaitForSeconds(1f);

        outputTerminalText.text += "Output: Hello\n";
        AddToBacklog("Terminal", "Output: Hello");

        yield return new WaitForSeconds(1.5f);

        inputTerminalPanel.SetActive(false);

        ShowText("See? print() sends a message.");

        yield return new WaitForSeconds(2f);

        ShowText("Now you try sending the correct message.");

        yield return new WaitForSeconds(1.5f);

        // Mark demo as played
        demoPlayed = true;
        PlayerPrefs.SetInt("DemoPlayed_" + SceneManager.GetActiveScene().name, 1);
        PlayerPrefs.Save();

        OpenInputTerminal();
    }

    // ================= INPUT =================
    void OpenInputTerminal()
    {
        currentState = GameState.Input;
        inputTerminalPanel.SetActive(true);

        playerInput = "";
        isTyping = true;
        isExecuting = false;

        UpdateInputDisplay();

        ShowText("Type a message to the door AI\nPress Enter to send");

        AddToBacklog("System", "Input terminal opened - waiting for message");
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
                if (playerInput.Length > 0)
                {
                    isTyping = false;
                    StartCoroutine(ExecuteCode());
                }
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
            if (currentState == GameState.Input)
                UpdateInputDisplay();
        }
    }

    void UpdateInputDisplay()
    {
        string cursor = (cursorVisible && isTyping) ? "|" : "";
        inputText.text = "> print(\"" + playerInput + cursor + "\")";
    }

    // ================= EXECUTION =================
    IEnumerator ExecuteCode()
    {
        isExecuting = true;
        currentState = GameState.Transition;

        inputTerminalPanel.SetActive(false);

        PlaySound(executeSound);

        ResetTerminal();
        outputTerminalText.text += "> Executing...\n";
        AddToBacklog("Terminal", "Executing: print(\"" + playerInput + "\")");

        yield return new WaitForSeconds(1.2f);

        string cleanText = playerInput.Trim().ToLower();

        outputTerminalText.text += "Output: " + playerInput + "\n";
        AddToBacklog("Terminal", "Output: " + playerInput);

        // Check for valid greetings
        bool isValidGreeting = cleanText.Contains("welcome") ||
                               cleanText == "hello" ||
                               cleanText == "hi" ||
                               cleanText == "greetings";

        if (isValidGreeting && cleanText.Contains("welcome"))
        {
            outputTerminalText.text += "Door AI: Message recognized ✓\n";
            outputTerminalText.text += "Door AI: Opening access...\n\n";
            AddToBacklog("Door AI", "Message recognized - Access granted");

            StartCoroutine(HandleSuccess());
        }
        else if (isValidGreeting)
        {
            outputTerminalText.text += "Door AI: Partial match...\n";
            outputTerminalText.text += "Door AI: Need exact message: 'welcome'\n\n";
            AddToBacklog("Door AI", "Partial match - Need exact message: 'welcome'");

            yield return new WaitForSeconds(1.5f);
            HandleError(cleanText, true);
        }
        else
        {
            outputTerminalText.text += "Door AI: No valid response\n\n";
            AddToBacklog("Door AI", "No valid response detected");

            yield return new WaitForSeconds(1.5f);
            HandleError(cleanText, false);
        }
    }

    // ================= ERROR =================
    void HandleError(string input, bool wasPartialMatch)
    {
        PlaySound(errorSound);

        errorCount++;

        // Select dialogue based on error count
        if (errorCount == 1)
        {
            currentDialogue = errorDialogue1;
        }
        else if (errorCount == 2)
        {
            currentDialogue = errorDialogue2;
        }
        else if (errorCount == 3)
        {
            currentDialogue = errorDialogue3;
        }
        else if (errorCount == 4)
        {
            currentDialogue = errorDialogue4;
        }
        else
        {
            currentDialogue = errorDialogueFinal;
        }

        currentState = GameState.Feedback;
        dialogueIndex = 0;

        // Add to backlog
        AddToBacklog("Hint System", "Attempt " + errorCount + " failed: '" + input + "'");

        ShowText(currentDialogue[dialogueIndex]);

        isExecuting = false;
    }

    // ================= SUCCESS =================
    IEnumerator HandleSuccess()
    {
        currentState = GameState.Success;

        AddToBacklog("System", "Success! Access granted to door");

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

        if (fadeCanvas != null)
        {
            fadeCanvas.blocksRaycasts = true;

            float t = 0;
            while (t < fadeDuration)
            {
                fadeCanvas.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Mark scene as completed
        MarkSceneCompleted();

        SceneManager.LoadScene(nextSceneName);
    }

    // ================= SCENE COMPLETION =================
    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("SceneCompleted_" + sceneName, 1);
        PlayerPrefs.Save();

        AddToBacklog("System", "Scene completed: " + sceneName);
    }

    // ================= DIALOGUE BACKLOG =================
    void AddToBacklog(string speaker, string message)
    {
        if (DialogueBacklogManager.Instance != null)
        {
            DialogueBacklogManager.Instance.AddLine(speaker, message);
        }
        else
        {
            Debug.Log("[Backlog] " + speaker + ": " + message);
        }
    }

    // ================= AUDIO =================
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void ResetTerminal()
    {
        if (outputTerminalText != null)
        {
            outputTerminalText.text =
                "PYTHON TERMINAL\n" +
                "----------------\n\n";
        }
    }

    // ================= PUBLIC METHODS =================
    public void ResetErrorCount()
    {
        errorCount = 0;
        AddToBacklog("System", "Error count reset");
    }

    public void ResetDemoFlag()
    {
        demoPlayed = false;
        PlayerPrefs.DeleteKey("DemoPlayed_" + SceneManager.GetActiveScene().name);
        AddToBacklog("System", "Demo flag reset");
    }

    public bool IsInUIState()
    {
        return currentState == GameState.Input ||
               currentState == GameState.Intro ||
               currentState == GameState.Feedback ||
               (boardPanel != null && boardPanel.activeSelf) ||
               (inputTerminalPanel != null && inputTerminalPanel.activeSelf);
    }
}