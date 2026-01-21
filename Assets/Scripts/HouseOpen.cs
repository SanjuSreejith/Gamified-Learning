using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorScanfLesson_Trigger : MonoBehaviour
{
    // ================= BOT UI =================
    [Header("Bot UI")]
    public GameObject boardPanel;
    public TextMeshProUGUI boardText;

    // ================= TERMINAL UI =================
    [Header("Terminal UI")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    // ================= AUDIO =================
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typeLetter;
    public AudioClip typeSpace;
    public AudioClip typeSymbol;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip doorOpenSound;

    // ================= SCENE =================
    [Header("Scene")]
    public string nextSceneName;

    // ================= STATE =================
    enum State
    {
        Idle,
        Teaching,
        WaitingForTerminal,
        Typing,
        Transition
    }

    State state = State.Idle;

    bool playerInside;
    bool dialogueActive;
    bool terminalOpen;

    string input = "";
    int attempts;
    int dialogueIndex;
    string[] activeDialogue;

    const string CORRECT_CODE = "scanf(\"%d\", &password);";

    // ================= DIALOGUES =================
    string[] teachingDialogue =
    {
        "This door is locked.",
        "It needs a password.",
        "I think the password is 349.",
        "But typing numbers alone won't work.",
        "In C, we use scanf to take input.",
        "scanf reads input from the user.",
        "It stores the value inside a variable.",
        "For numbers, we use %d.",
        "Example:",
        "scanf(\"%d\", &password);",
        "Press 1 to open the terminal."
    };

    string[] wrongDialogue =
    {
        "That didn't work.",
        "scanf syntax must be exact.",
        "Check %d and &password.",
        "Press 1 and try again."
    };

    string[] successPerfect =
    {
        "Excellent!",
        "You used scanf correctly.",
        "The password was read properly.",
        "The door is opening."
    };

    string[] successGood =
    {
        "Good job!",
        "You figured out scanf.",
        "The door is opening."
    };

    string[] successOkay =
    {
        "You did it.",
        "Practice makes you better.",
        "The door is opening."
    };

    // ================= START =================
    void Start()
    {
        if (boardPanel != null) boardPanel.SetActive(false);
        if (terminalPanel != null) terminalPanel.SetActive(false);

        if (terminalText != null)
            terminalText.text = "C INPUT TERMINAL\n----------------\n\n";

        Debug.Log("DoorScanfLesson_Trigger initialized. State: " + state);
    }

    // ================= TRIGGER (2D) =================
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger entered by: " + other.name + " with tag: " + other.tag);

        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        Debug.Log("Player entered trigger. Current state: " + state);

        if (state == State.Idle)
        {
            state = State.Teaching;
            Debug.Log("Starting teaching dialogue...");
            StartDialogue(teachingDialogue);
        }
        else if (state == State.WaitingForTerminal)
        {
            // Show the terminal prompt again
            Debug.Log("Player returned, showing terminal prompt");
            if (boardPanel != null)
            {
                boardPanel.SetActive(true);
                boardText.text = "Press 1 to open the terminal.";
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        Debug.Log("Player left trigger. State: " + state);

        // Only hide board if not in dialogue and terminal is closed
        if (!dialogueActive && !terminalOpen && state != State.Transition)
        {
            if (boardPanel != null)
                boardPanel.SetActive(false);
        }
    }

    // ================= UPDATE =================
    void Update()
    {
        // Handle dialogue advancement first
        if (dialogueActive)
        {
            HandleDialogueAdvance();
        }

        // Only handle terminal opening if we're waiting and player is inside
        else if (state == State.WaitingForTerminal && playerInside)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("1 key pressed to open terminal");
                OpenTerminal();
            }
        }

        // Handle typing in terminal
        else if (state == State.Typing && terminalOpen)
        {
            HandleTyping();
        }
    }

    // ================= DIALOGUE =================
    void StartDialogue(string[] dialogue)
    {
        if (dialogue == null || dialogue.Length == 0)
        {
            Debug.LogError("Dialogue array is null or empty!");
            return;
        }

        activeDialogue = dialogue;
        dialogueIndex = 0;
        dialogueActive = true;

        if (boardPanel == null)
        {
            Debug.LogError("BoardPanel is not assigned!");
            return;
        }

        boardPanel.SetActive(true);
        boardText.text = activeDialogue[dialogueIndex];
        Debug.Log("Started dialogue: " + activeDialogue[dialogueIndex]);
    }

    void HandleDialogueAdvance()
    {
        if (!dialogueActive) return;

        // Check for advance input
        bool advanceInput = Input.GetKeyDown(KeyCode.Return) ||
                           Input.GetKeyDown(KeyCode.KeypadEnter) ||
                           Input.GetMouseButtonDown(0);

        if (advanceInput)
        {
            Debug.Log("Dialogue advance input detected");
            dialogueIndex++;

            if (dialogueIndex < activeDialogue.Length)
            {
                boardText.text = activeDialogue[dialogueIndex];
                Debug.Log("Advanced to dialogue line " + dialogueIndex + ": " + activeDialogue[dialogueIndex]);
            }
            else
            {
                Debug.Log("Dialogue completed");
                dialogueActive = false;

                // Determine next state based on current state
                if (state == State.Teaching)
                {
                    state = State.WaitingForTerminal;
                    Debug.Log("Changed state to WaitingForTerminal");

                    if (boardPanel != null)
                    {
                        boardPanel.SetActive(true);
                        boardText.text = "Press 1 to open the terminal.";
                    }
                }
                else if (state == State.Transition)
                {
                    Debug.Log("Transition state - dialogue ended, loading next scene");
                    // Dialogue ended after success, proceed to next scene
                    if (!string.IsNullOrEmpty(nextSceneName))
                        SceneManager.LoadScene(nextSceneName);
                }
                else
                {
                    // For wrong dialogue, go back to waiting for terminal
                    state = State.WaitingForTerminal;
                    Debug.Log("Wrong dialogue completed, back to WaitingForTerminal");
                    if (boardPanel != null)
                        boardPanel.SetActive(false);
                }
            }
        }
    }

    // ================= TERMINAL =================
    void OpenTerminal()
    {
        Debug.Log("Opening terminal...");

        if (boardPanel != null)
            boardPanel.SetActive(false);

        terminalOpen = true;
        input = "";

        if (terminalPanel == null)
        {
            Debug.LogError("TerminalPanel is not assigned!");
            return;
        }

        terminalPanel.SetActive(true);

        // Set up terminal display
        terminalText.text = "C INPUT TERMINAL\n" +
                           "----------------\n\n" +
                           "Enter the scanf command:\n" +
                           "> ";

        state = State.Typing;
        Debug.Log("Terminal opened. State changed to Typing");
    }

    void HandleTyping()
    {
        string inputThisFrame = Input.inputString;

        if (!string.IsNullOrEmpty(inputThisFrame))
            Debug.Log("Input received: " + System.Text.Encoding.ASCII.GetBytes(inputThisFrame)[0] + " (char: " + inputThisFrame[0] + ")");

        foreach (char c in inputThisFrame)
        {
            if (c == '\b') // Backspace
            {
                if (input.Length > 0)
                {
                    input = input.Remove(input.Length - 1);
                    RefreshLine();
                }
            }
            else if (c == '\n' || c == '\r') // Enter
            {
                Debug.Log("Enter pressed, submitting: " + input);
                Submit();
                break; // Break after submit to avoid processing more characters
            }
            else if (!char.IsControl(c)) // Regular character
            {
                input += c;
                PlayTypingSound(c);
                RefreshLine();
            }
        }
    }

    void RefreshLine()
    {
        string[] lines = terminalText.text.Split('\n');

        // Find the line starting with "> "
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("> "))
            {
                lines[i] = "> " + input + "_"; // Add cursor
                terminalText.text = string.Join("\n", lines);
                return;
            }
        }
    }

    // ================= SUBMIT =================
    void Submit()
    {
        Debug.Log("Submitting input: " + input);
        terminalOpen = false;

        if (terminalPanel != null)
            terminalPanel.SetActive(false);

        // Remove cursor from display
        string displayText = terminalText.text.Replace("_", "");
        terminalText.text = displayText + "\n\nYou entered: " + input + "\n";

        attempts++;
        Debug.Log("Attempt " + attempts + ": " + input);

        if (Clean(input) == Clean(CORRECT_CODE))
        {
            Debug.Log("CORRECT! Starting success sequence...");
            StartCoroutine(Success());
        }
        else
        {
            Debug.Log("WRONG. Expected: " + CORRECT_CODE + " Got: " + input);
            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);

            state = State.WaitingForTerminal;
            StartDialogue(wrongDialogue);
        }

        input = "";
    }

    // ================= SUCCESS =================
    IEnumerator Success()
    {
        Debug.Log("Success coroutine started");

        if (audioSource != null && correctSound != null)
            audioSource.PlayOneShot(correctSound);

        terminalText.text += "\nInput read successfully!\n";
        terminalText.text += "Password = 349\n";
        terminalText.text += "Door unlocked!\n\n";

        // Determine which success dialogue to use
        if (attempts == 1)
        {
            Debug.Log("Perfect success (1 attempt)");
            StartDialogue(successPerfect);
        }
        else if (attempts == 2)
        {
            Debug.Log("Good success (2 attempts)");
            StartDialogue(successGood);
        }
        else
        {
            Debug.Log("Okay success (" + attempts + " attempts)");
            StartDialogue(successOkay);
        }

        yield return new WaitUntil(() => !dialogueActive);
        Debug.Log("Success dialogue completed");

        if (audioSource != null && doorOpenSound != null)
            audioSource.PlayOneShot(doorOpenSound);

        state = State.Transition;
        Debug.Log("Changed state to Transition");
        yield return new WaitForSeconds(1f);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("Loading next scene: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next scene name is not set!");
        }
    }

    // ================= SOUND =================
    void PlayTypingSound(char c)
    {
        if (!audioSource) return;

        if (char.IsLetter(c))
        {
            if (typeLetter != null) audioSource.PlayOneShot(typeLetter);
        }
        else if (c == ' ')
        {
            if (typeSpace != null) audioSource.PlayOneShot(typeSpace);
        }
        else
        {
            if (typeSymbol != null) audioSource.PlayOneShot(typeSymbol);
        }
    }

    // ================= HELPERS =================
    string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace(" ", "").Replace("\t", "").ToLower();
    }

    // ================= DEBUG =================
    //void OnGUI()
    //{
    //    // Debug display of current state
    //    GUIStyle style = new GUIStyle(GUI.skin.label);
    //    style.fontSize = 20;
    //    style.normal.textColor = Color.white;

    //    GUI.Label(new Rect(10, 10, 500, 30), $"State: {state}", style);
    //    GUI.Label(new Rect(10, 40, 500, 30), $"Dialogue Active: {dialogueActive}", style);
    //    GUI.Label(new Rect(10, 70, 500, 30), $"Terminal Open: {terminalOpen}", style);
    //    GUI.Label(new Rect(10, 100, 500, 30), $"Player Inside: {playerInside}", style);
    //    GUI.Label(new Rect(10, 130, 500, 30), $"Attempts: {attempts}", style);

    //    if (state == State.WaitingForTerminal && playerInside)
    //    {
    //        GUI.Label(new Rect(10, 160, 500, 30), "PRESS 1 TO OPEN TERMINAL", style);
    //    }
    //}
}