using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class TowerController : MonoBehaviour
{
    [System.Serializable]
    public class UIPanels
    {
        [Header("Terminal UI")]
        public GameObject terminalPanel;
        public TextMeshProUGUI terminalText;

        [Header("Dialogue UI")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI dialogueText;
        public Image speakerImage;
        public Sprite abelPortrait;
        public Sprite kuttanPortrait;
    }

    [System.Serializable]
    public class TowerSettings
    {
        [Header("Tower")]
        public Tower tower;

        [Header("Spike Trap (Optional - Single Trap)")]
        public SpikeTrap spikeTrap; // Keep for backward compatibility

        [Header("Enemy Counter")]
        public EnemyCounter enemyCounter;

        [Header("Machine Sound")]
        public AudioSource machineAudio;
    }

    [System.Serializable]
    public class MachineValues
    {
        public float timerValue = 10f;
        public float healthValue = 100f;
        public const float TIMER_MAX = 10f;
        public const float HEALTH_MAX = 100f;
    }

    [Header("Configuration")]
    public UIPanels ui;
    public TowerSettings towerSettings;
    public MachineValues machineValues = new MachineValues();

    [Header("Post Lesson Settings")]
    public Collider2D colliderToDeactivateAfterClear;
    public bool deactivateColliderOnCompletion = true; // Enable/disable collider deactivation

    [Header("Cinematic Camera (Optional - Not for Button Towers)")]
    public CinemachineCamera cinematicCamera;
    public float cinematicDuration = 3f;
    public int cinematicPriority = 20;
    public bool enableCinematicForNonButtonTowers = true; // Master toggle

    // State Management
    private enum TowerState
    {
        Inactive,           // Not yet interacted with
        IntroPlayed,        // Intro dialogue done
        TerminalOpen,       // Terminal is open
        Configured,         // Terminal solved, ready to run
        Running,            // Machine is running
        Completed,          // Tower fully completed
        Failed              // Tower failed (too many mistakes)
    }

    private TowerState currentState = TowerState.Inactive;
    private TMPTypewriter typewriter;
    private string input = "";

    // Mistake tracking
    private int mistakeCount = 0;
    private const int MAX_MISTAKES = 3;
    private bool hasGivenHint;
    private List<string> wrongAttempts = new List<string>();

    // Button tower specific
    private bool isHoldingButton;
    private string buttonCondition = "";

    // Coroutine references
    private Coroutine machineLoopCoroutine;
    private Coroutine currentDialogueCoroutine;
    private Coroutine cinematicCoroutine;

    // Pause state
    private bool isPaused;
    private float previousTimeScale = 1f;

    // Player detection
    private bool playerInRange;

    // Track if machine effects are active
    private bool machineEffectsActive = false;

    // Track if lesson is cleared (collider deactivated)
    private bool lessonCleared = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        // Safety: ensure UI starts hidden
        if (ui.terminalPanel != null)
            ui.terminalPanel.SetActive(false);
        if (ui.dialoguePanel != null)
            ui.dialoguePanel.SetActive(false);

        // Setup audio
        if (towerSettings.machineAudio != null)
        {
            towerSettings.machineAudio.loop = true;
            towerSettings.machineAudio.playOnAwake = false;
        }
    }

    void Start()
    {
        if (ui.dialogueText != null)
            typewriter = ui.dialogueText.GetComponent<TMPTypewriter>();

        ValidateComponents();
        ResetTower();
    }

    void ValidateComponents()
    {
        if (ui.terminalPanel == null)
            Debug.LogError("Terminal Panel not assigned!");
        if (ui.dialoguePanel == null)
            Debug.LogError("Dialogue Panel not assigned!");
        if (towerSettings.tower == null)
            Debug.LogError("Tower not assigned!");

        // Check if we have either a direct spike trap or a machine group with traps
        if (towerSettings.spikeTrap == null &&
            (towerSettings.tower == null || towerSettings.tower.machine == null || towerSettings.tower.machine.traps.Length == 0))
        {
            Debug.LogWarning("No spike traps assigned to this tower!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        Debug.Log($"Player entered tower range. State: {currentState}, LessonCleared: {lessonCleared}");

        HandlePlayerEnter();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        Debug.Log("Player left tower range");

        HandlePlayerExit();
    }

    void Update()
    {
        if (IsTerminalState()) return;

        HandlePlayerInput();
    }

    #region State Management

    bool IsTerminalState()
    {
        return currentState == TowerState.Completed ||
               currentState == TowerState.Failed;
    }

    bool CanInteract()
    {
        return playerInRange && !IsTerminalState() && !lessonCleared;
    }

    void ChangeState(TowerState newState)
    {
        Debug.Log($"Tower state changed: {currentState} -> {newState}");
        currentState = newState;
    }

    #endregion

    #region Player Interaction

    void HandlePlayerEnter()
    {
        if (IsTerminalState() || lessonCleared) return;

        switch (currentState)
        {
            case TowerState.Inactive:
                ChangeState(TowerState.IntroPlayed);
                StartDialogue(GetIntroDialogue());
                break;

            case TowerState.Configured:
                if (IsButtonTower())
                {
                    StartDialogue(GetReminderDialogue());
                }
                break;

            case TowerState.Running:
                // Just remind that machine is running
                if (IsButtonTower() && !isHoldingButton)
                {
                    StartDialogue(GetRunningReminderDialogue());
                }
                break;

            default:
                // For other states, maybe play a brief reminder
                if (currentState != TowerState.IntroPlayed)
                {
                    StartDialogue(GetQuickReminderDialogue());
                }
                break;
        }
    }

    void HandlePlayerExit()
    {
        // Close UI when player leaves
        if (ui.terminalPanel.activeSelf)
        {
            CloseTerminal();
        }

        if (ui.dialoguePanel.activeSelf)
        {
            StopDialogue();
        }

        // Auto-release button for button tower
        if (IsButtonTower() && isHoldingButton)
        {
            isHoldingButton = false;
            Debug.Log("Player left - R released automatically");
        }

        TryResumeGame();
    }

    void HandlePlayerInput()
    {
        if (!CanInteract()) return;

        // Handle dialogue input FIRST - highest priority
        if (ui.dialoguePanel.activeSelf)
        {
            HandleDialogueInput();
            return;
        }

        if (ui.terminalPanel.activeSelf)
        {
            HandleTerminalTyping();
            return;
        }

        // Open terminal with E
        if (Input.GetKeyDown(KeyCode.E) && CanOpenTerminal())
        {
            OpenTerminal();
        }

        // Handle button tower input
        if (IsButtonTower() && (currentState == TowerState.Configured || currentState == TowerState.Running))
        {
            HandleButtonInput();
        }
    }

    bool CanOpenTerminal()
    {
        return currentState != TowerState.Completed &&
               currentState != TowerState.Failed &&
               currentState != TowerState.Configured && // Already configured
               !ui.dialoguePanel.activeSelf &&
               !lessonCleared;
    }

    #endregion

    #region Dialogue System

    void HandleDialogueInput()
    {
        if (!ui.dialoguePanel.activeSelf) return;

        // Check if Enter key is pressed
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Enter/E pressed during dialogue");

            // If typewriter is still typing, skip to end
            if (typewriter != null && typewriter.IsTyping())
            {
                typewriter.Skip();
                return;
            }

            // If not typing, advance to next dialogue or close panel
            AdvanceDialogueManually();
        }
    }

    void AdvanceDialogueManually()
    {
        // If we're in a dialogue coroutine, let it handle the advancement
        // The coroutine will detect the key press through its own loop
        // This method is just a backup
    }

    void StartDialogue(string[][] dialogue)
    {
        if (currentDialogueCoroutine != null)
            StopCoroutine(currentDialogueCoroutine);

        currentDialogueCoroutine = StartCoroutine(PlayDialogueSequence(dialogue));
    }

    void StopDialogue()
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }

        ui.dialoguePanel.SetActive(false);
        TryResumeGame();
    }

    IEnumerator PlayDialogueSequence(string[][] dialogues)
    {
        PauseGame();

        for (int i = 0; i < dialogues.Length; i++)
        {
            string[] dialogue = dialogues[i];
            if (dialogue.Length == 2)
            {
                // Show the dialogue
                ShowDialogue(dialogue[0], dialogue[1]);

                // Wait for player to press Enter/E to continue
                bool waitingForInput = true;
                while (waitingForInput)
                {
                    // Check for input to advance
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
                    {
                        // If typewriter is still typing, skip to end
                        if (typewriter != null && typewriter.IsTyping())
                        {
                            typewriter.Skip();
                        }
                        else
                        {
                            // If this is the last dialogue, close panel
                            if (i == dialogues.Length - 1)
                            {
                                waitingForInput = false;
                            }
                            else
                            {
                                waitingForInput = false;
                            }
                        }
                    }
                    yield return null;
                }

                // Small delay between dialogues
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        // End of dialogue sequence - hide panel
        ui.dialoguePanel.SetActive(false);
        currentDialogueCoroutine = null;
        TryResumeGame();
    }
    void ShowDialogue(string speaker, string text)
    {
        ui.dialoguePanel.SetActive(true);

        if (ui.speakerText != null)
            ui.speakerText.text = speaker;

        if (ui.speakerImage != null)
        {
            ui.speakerImage.sprite = speaker == "Abel" ? ui.abelPortrait : ui.kuttanPortrait;
        }

        // 📝 Add the line to the backlog (full text, not typed)
        if (DialogueBacklogManager.Instance != null)
            DialogueBacklogManager.Instance.AddLine(speaker, text);

        if (typewriter != null)
        {
            typewriter.Play(text); // starts letter‑by‑letter effect
        }
        else if (ui.dialogueText != null)
        {
            ui.dialogueText.text = text;
        }
    }
    #endregion

    #region Terminal System

    void OpenTerminal()
    {
        Debug.Log("Opening terminal");
        ChangeState(TowerState.TerminalOpen);

        input = "";
        ui.terminalPanel.SetActive(true);
        UpdateTerminalDisplay();
        PauseGame();
    }

    void CloseTerminal()
    {
        Debug.Log("Closing terminal");
        ui.terminalPanel.SetActive(false);

        if (currentState == TowerState.TerminalOpen)
        {
            ChangeState(TowerState.IntroPlayed);
        }

        TryResumeGame();
    }

    void HandleTerminalTyping()
    {
        bool inputChanged = false;

        foreach (char c in Input.inputString)
        {
            if (c == '\n' || c == '\r') // Enter
            {
                SubmitTerminalInput();
                return;
            }

            if (c == '\b') // Backspace
            {
                if (input.Length > 0)
                {
                    input = input.Remove(input.Length - 1);
                    inputChanged = true;
                }
            }
            else if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '>' || c == '<' || c == '=')
            {
                input += c;
                inputChanged = true;
            }
        }

        if (inputChanged)
        {
            UpdateTerminalDisplay();
        }
    }

    void SubmitTerminalInput()
    {
        string submittedInput = input.Trim();
        CloseTerminal();
        ValidateAndExecute(submittedInput);
    }

    void UpdateTerminalDisplay()
    {
        if (ui.terminalText == null || towerSettings.tower == null) return;

        int remainingAttempts = MAX_MISTAKES - mistakeCount;
        string displayText = "";

        if (towerSettings.tower.towerType == Tower.TowerType.Emergency)
        {
            displayText = $"> {(string.IsNullOrEmpty(input) ? "________" : input)}\n\n" +
                         "# Type <color=yellow>break</color> to stop the loop\n" +
                         $"<color=#FFA500>Attempts remaining: {remainingAttempts}</color>";
        }
        else
        {
            string conditionDisplay = string.IsNullOrEmpty(input) ? "__________" : input;
            string expected = GetExpectedCondition();

            displayText = $"while {conditionDisplay}:\n    run()\n\n" +
                         $"# Expected: <color=yellow>{expected}</color>\n" +
                         $"<color=#FFA500>Attempts remaining: {remainingAttempts}</color>";

            // Show current machine values
            if (towerSettings.tower.towerType == Tower.TowerType.Timer)
            {
                displayText += $"\n\n<color=#87CEEB>Current time: {machineValues.timerValue:F1}s</color>";
            }
            else if (towerSettings.tower.towerType == Tower.TowerType.Health)
            {
                displayText += $"\n\n<color=#87CEEB>Current health: {machineValues.healthValue:F1}</color>";
            }

            // Add hint after 2 mistakes
            if (mistakeCount >= 2 && !hasGivenHint)
            {
                displayText += $"\n\n<color=#87CEEB>Hint: Try typing exactly: {expected}</color>";
                hasGivenHint = true;
            }
        }

        ui.terminalText.text = displayText;
    }

    void ValidateAndExecute(string typedInput)
    {
        if (IsTerminalState() || lessonCleared) return;

        if (string.IsNullOrEmpty(typedInput))
        {
            HandleMistake("Input cannot be empty!");
            return;
        }

        bool isCorrect = false;
        string expected = GetExpectedCondition();

        if (towerSettings.tower.towerType == Tower.TowerType.Emergency)
        {
            isCorrect = typedInput.ToLower() == "break";
        }
        else if (IsButtonTower())
        {
            isCorrect = typedInput.ToLower().Replace(" ", "") == "device_signal" ||
                       typedInput.ToLower().Replace(" ", "") == "devicesignal";

            if (isCorrect)
            {
                buttonCondition = typedInput;
            }
        }
        else
        {
            isCorrect = typedInput == expected;
        }

        if (!isCorrect)
        {
            HandleMistake($"Incorrect: '{typedInput}'");
            return;
        }

        // Correct input!
        Debug.Log("Correct input!");
        HandleCorrectInput();
    }

    void HandleMistake(string errorMessage)
    {
        mistakeCount++;
        wrongAttempts.Add(errorMessage);

        if (mistakeCount >= MAX_MISTAKES)
        {
            StartCoroutine(HandleFailure());
        }
        else
        {
            StartCoroutine(ShowErrorAndRetry(errorMessage));
        }
    }

    void HandleCorrectInput()
    {
        bool isButtonTower = IsButtonTower();

        if (towerSettings.tower.towerType == Tower.TowerType.Emergency)
        {
            ChangeState(TowerState.Completed);
            ClearLesson(); // Deactivate collider immediately

            // Only play cinematic for non-button towers if enabled
            if (!isButtonTower && enableCinematicForNonButtonTowers)
                StartCinematicCamera();

            StartDialogue(GetSuccessDialogue());

            if (towerSettings.tower != null)
                towerSettings.tower.Activate();
        }
        else if (isButtonTower)
        {
            ChangeState(TowerState.Configured);
            StartDialogue(GetConfiguredDialogue());
            // Collider stays active until R hold completes
        }
        else
        {
            ChangeState(TowerState.Running);
            StartMachineLoop(input);
            ClearLesson(); // Deactivate collider immediately

            // Only play cinematic for non-button towers if enabled
            if (!isButtonTower && enableCinematicForNonButtonTowers)
                StartCinematicCamera();

            StartDialogue(GetSuccessDialogue());
        }
    }

    IEnumerator ShowErrorAndRetry(string message)
    {
        if (ui.terminalText != null && !IsTerminalState() && !lessonCleared)
        {
            ui.terminalText.text = $"<color=red>✗ {message}</color>\n\nPress E to try again...";
            yield return new WaitForSecondsRealtime(1.5f);

            if (playerInRange && !IsTerminalState() && mistakeCount < MAX_MISTAKES && !lessonCleared)
            {
                OpenTerminal();
            }
        }
    }

    IEnumerator HandleFailure()
    {
        ChangeState(TowerState.Failed);

        if (ui.terminalText != null)
        {
            ui.terminalText.text = "<color=red>✗ TOWER LOCKED\n\nToo many incorrect attempts.</color>";
            yield return new WaitForSecondsRealtime(1.5f);
        }

        CloseTerminal();
        StopMachineLoop();
        StartDialogue(GetFailureDialogue());

        // Disable collider
        GetComponent<Collider2D>().enabled = false;
    }

    #endregion

    #region Machine Loop

    void StartMachineLoop(string condition)
    {
        if (machineLoopCoroutine != null)
            StopCoroutine(machineLoopCoroutine);

        machineLoopCoroutine = StartCoroutine(RunMachineLoop(condition));
    }

    void StopMachineLoop()
    {
        if (machineLoopCoroutine != null)
        {
            StopCoroutine(machineLoopCoroutine);
            machineLoopCoroutine = null;
        }

        StopMachineEffects();
    }

    IEnumerator RunMachineLoop(string condition)
    {
        while (EvaluateCondition(condition))
        {
            // Activate effects if not already active
            if (!machineEffectsActive)
            {
                ActivateMachineEffects();
                machineEffectsActive = true;
            }

            if (towerSettings.tower != null && towerSettings.tower.machine != null)
            {
                towerSettings.tower.machine.ActivateMachine();
            }

            UpdateMachineValues();
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("While condition failed - stopping machine");
        StopMachineEffects();
    }

    bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return false;

        condition = condition.Trim();

        switch (towerSettings.tower.towerType)
        {
            case Tower.TowerType.Timer:
                return EvaluateComparison(condition, machineValues.timerValue, "time_remaining");

            case Tower.TowerType.Health:
                return EvaluateComparison(condition, machineValues.healthValue, "machine_health");

            case Tower.TowerType.Button:
                return isHoldingButton;

            case Tower.TowerType.Emergency:
                return false;
        }

        return false;
    }

    bool EvaluateComparison(string condition, float currentValue, string variableName)
    {
        if (condition.Contains(">"))
        {
            string[] parts = condition.Split('>');
            if (parts.Length == 2)
            {
                string variable = parts[0].Trim();
                float threshold;
                if (float.TryParse(parts[1].Trim(), out threshold))
                {
                    if (variable == variableName)
                        return currentValue > threshold;
                }
            }
        }
        else if (condition.Contains("<"))
        {
            string[] parts = condition.Split('<');
            if (parts.Length == 2)
            {
                string variable = parts[0].Trim();
                float threshold;
                if (float.TryParse(parts[1].Trim(), out threshold))
                {
                    if (variable == variableName)
                        return currentValue < threshold;
                }
            }
        }

        // Default behavior
        return currentValue > 0;
    }

    void UpdateMachineValues()
    {
        switch (towerSettings.tower.towerType)
        {
            case Tower.TowerType.Timer:
                machineValues.timerValue -= Time.deltaTime * 2f;
                if (machineValues.timerValue < 0) machineValues.timerValue = 0;
                Debug.Log($"Timer: {machineValues.timerValue:F1}s remaining");
                break;

            case Tower.TowerType.Health:
                machineValues.healthValue -= Time.deltaTime * 5f;
                if (machineValues.healthValue < 0) machineValues.healthValue = 0;
                Debug.Log($"Health: {machineValues.healthValue:F1} remaining");
                break;
        }
    }

    #endregion

    #region Button Tower - Fixed for reactivation

    void HandleButtonInput()
    {
        if (towerSettings.tower == null || towerSettings.tower.machine == null) return;

        // Allow button input in both Configured and Running states
        if (currentState != TowerState.Configured && currentState != TowerState.Running) return;

        // PRESS R - Start holding
        if (Input.GetKeyDown(KeyCode.R) && !isHoldingButton)
        {
            isHoldingButton = true;
            Debug.Log("Button pressed - Starting machine");

            // If we're in Configured state, start the machine
            if (currentState == TowerState.Configured)
            {
                ChangeState(TowerState.Running);
                StartMachineLoop(buttonCondition);
                StartCoroutine(DelayedCompleteTower());
            }
            // If we're in Running state, the machine is already running
            else if (currentState == TowerState.Running)
            {
                Debug.Log("Machine already running - continuing");
            }
        }

        // RELEASE R - Stop holding
        if (Input.GetKeyUp(KeyCode.R) && isHoldingButton)
        {
            isHoldingButton = false;
            Debug.Log("Button released - condition will become false");
            // Machine will stop when condition evaluates to false in the next loop iteration
        }
    }

    IEnumerator DelayedCompleteTower()
    {
        yield return new WaitForSeconds(0.5f);

        if (!IsTerminalState() && currentState == TowerState.Running)
        {
            ChangeState(TowerState.Completed);

            // Only now deactivate collider for button tower (after R hold completes)
            ClearLesson();

            if (towerSettings.tower != null)
                towerSettings.tower.Activate();
        }
    }

    #endregion

    #region Machine Effects Control

    void ActivateMachineEffects()
    {
        // Play sound
        PlayMachineSound();

        Debug.Log("Machine effects activated - traps will be activated by MachineGroup");
    }

    void DeactivateMachineEffects()
    {
        // Stop sound
        StopMachineSound();

        machineEffectsActive = false;
    }

    #endregion

    #region Audio

    void PlayMachineSound()
    {
        if (towerSettings.machineAudio != null && !towerSettings.machineAudio.isPlaying)
        {
            towerSettings.machineAudio.Play();
        }
    }

    void StopMachineSound()
    {
        if (towerSettings.machineAudio != null && towerSettings.machineAudio.isPlaying)
        {
            towerSettings.machineAudio.Stop();
        }
    }

    #endregion

    #region Effects Management

    void StopMachineEffects()
    {
        if (towerSettings.tower != null && towerSettings.tower.machine != null)
        {
            towerSettings.tower.machine.DeactivateMachine();
        }

        DeactivateMachineEffects();

        if (currentState == TowerState.Running)
        {
            ChangeState(TowerState.Configured);
        }
    }

    #endregion

    #region Dialogue Content

    string[][] GetIntroDialogue()
    {
        if (towerSettings.tower == null) return new string[0][];

        // Get trap info from the machine group
        string trapInfo = "";
        if (towerSettings.tower.machine != null && towerSettings.tower.machine.traps.Length > 0)
        {
            SpikeTrap firstTrap = towerSettings.tower.machine.traps[0];
            if (firstTrap != null)
            {
                trapInfo = $"\nDeals {firstTrap.damage} damage every {firstTrap.damageInterval}s";
                if (towerSettings.tower.machine.traps.Length > 1)
                {
                    trapInfo += $" (x{towerSettings.tower.machine.traps.Length} traps)";
                }
            }
        }
        else if (towerSettings.spikeTrap != null)
        {
            trapInfo = $"\nDeals {towerSettings.spikeTrap.damage} damage every {towerSettings.spikeTrap.damageInterval}s";
        }

        switch (towerSettings.tower.towerType)
        {
            case Tower.TowerType.Button:
                return new string[][] {
                    new string[] { "Abel", "This tower responds to both code and touch." },
                    new string[] { "Kuttan", "Press E to open the terminal." },
                    new string[] { "Abel", $"Type: {GetExpectedCondition()}{trapInfo}" },
                    new string[] { "Kuttan", "After that, hold R to activate the traps!" },
                    new string[] { "Abel", "The traps keep running even after you leave!" }
                };

            case Tower.TowerType.Timer:
                return new string[][] {
                    new string[] { "Kuttan", "Time itself is the condition now." },
                    new string[] { "Abel", $"Type: {GetExpectedCondition()} (current time: {machineValues.timerValue:F1}s){trapInfo}" },
                    new string[] { "Kuttan", "The traps activate while time remains!" }
                };

            case Tower.TowerType.Health:
                return new string[][] {
                    new string[] { "Abel", "This loop depends on its health." },
                    new string[] { "Kuttan", $"Type: {GetExpectedCondition()} (current health: {machineValues.healthValue:F1}){trapInfo}" },
                    new string[] { "Abel", "The traps fight while health remains!" }
                };

            case Tower.TowerType.Emergency:
                return new string[][] {
                    new string[] { "Abel", "This loop will never end on its own." },
                    new string[] { "Kuttan", $"Type exactly: {GetExpectedCondition()}" },
                    new string[] { "Abel", "This will deactivate the emergency traps!" }
                };

            default:
                return new string[0][];
        }
    }

    string[][] GetQuickReminderDialogue()
    {
        return new string[][] {
            new string[] { "Kuttan", "Press E to open the terminal." },
            new string[] { "Abel", $"Type: {GetExpectedCondition()}" }
        };
    }

    string[][] GetReminderDialogue()
    {
        return new string[][] {
            new string[] { "Kuttan", "This tower is ready to go!" },
            new string[] { "Abel", "Just hold R to start the machine." }
        };
    }

    string[][] GetRunningReminderDialogue()
    {
        return new string[][] {
            new string[] { "Abel", "The machine is running!" },
            new string[] { "Kuttan", "Hold R to keep it active." }
        };
    }

    string[][] GetConfiguredDialogue()
    {
        return new string[][] {
            new string[] { "Abel", "Perfect! The condition is set." },
            new string[] { "Kuttan", "Now hold R to activate!" },
            new string[] { "Abel", "It keeps running even after you leave!" }
        };
    }

    string[][] GetSuccessDialogue()
    {
        if (towerSettings.tower == null) return new string[0][];

        switch (towerSettings.tower.towerType)
        {
            case Tower.TowerType.Timer:
                return new string[][] {
                    new string[] { "Kuttan", "Perfect! The time condition is set." },
                    new string[] { "Abel", $"You have {machineValues.timerValue:F1}s of operation!" }
                };

            case Tower.TowerType.Health:
                return new string[][] {
                    new string[] { "Abel", "The health condition is correct." },
                    new string[] { "Kuttan", $"It has {machineValues.healthValue:F1} health to work with." }
                };

            case Tower.TowerType.Emergency:
                return new string[][] {
                    new string[] { "Kuttan", "You stopped the infinite loop!" },
                    new string[] { "Abel", "Emergency protocol successful." }
                };

            default:
                return new string[0][];
        }
    }

    string[][] GetFailureDialogue()
    {
        if (towerSettings.tower == null) return new string[0][];

        string attempts = string.Join(", ", wrongAttempts);

        switch (towerSettings.tower.towerType)
        {
            case Tower.TowerType.Button:
                return new string[][] {
                    new string[] { "Kuttan", $"You tried: {attempts}" },
                    new string[] { "Abel", "You needed to type: device_signal" }
                };

            case Tower.TowerType.Timer:
                return new string[][] {
                    new string[] { "Abel", $"You tried: {attempts}" },
                    new string[] { "Kuttan", "The correct condition was: time_remaining > 0" },
                    new string[] { "Abel", $"Time was at {machineValues.timerValue:F1}s when it failed." }
                };

            case Tower.TowerType.Health:
                return new string[][] {
                    new string[] { "Kuttan", $"Your attempts: {attempts}" },
                    new string[] { "Abel", "You needed: machine_health > 0" },
                    new string[] { "Kuttan", $"Health was at {machineValues.healthValue:F1} when it failed." }
                };

            case Tower.TowerType.Emergency:
                return new string[][] {
                    new string[] { "Abel", $"You typed: {attempts}" },
                    new string[] { "Kuttan", "But only 'break' could stop it." }
                };

            default:
                return new string[0][];
        }
    }

    #endregion

    #region Helper Methods

    bool IsButtonTower()
    {
        return towerSettings.tower != null &&
               towerSettings.tower.towerType == Tower.TowerType.Button;
    }

    string GetExpectedCondition()
    {
        if (towerSettings.tower == null) return "";

        switch (towerSettings.tower.towerType)
        {
            case Tower.TowerType.Timer: return "time_remaining > 0";
            case Tower.TowerType.Health: return "machine_health > 0";
            case Tower.TowerType.Button: return "device_signal";
            case Tower.TowerType.Emergency: return "break";
        }
        return "";
    }

    #endregion

    #region Post Lesson Features

    void ClearLesson()
    {
        if (lessonCleared) return;

        lessonCleared = true;
        DeactivateLessonCollider();
    }

    void DeactivateLessonCollider()
    {
        if (!deactivateColliderOnCompletion) return;

        if (colliderToDeactivateAfterClear != null)
        {
            colliderToDeactivateAfterClear.enabled = false;
            Debug.Log("[TowerController] Lesson collider deactivated");
        }
    }

    void StartCinematicCamera()
    {
        if (cinematicCamera == null || !enableCinematicForNonButtonTowers) return;

        if (cinematicCoroutine != null)
            StopCoroutine(cinematicCoroutine);

        cinematicCoroutine = StartCoroutine(PlayCinematicCamera());
    }

    IEnumerator PlayCinematicCamera()
    {
        if (cinematicCamera == null) yield break;

        int originalPriority = cinematicCamera.Priority;
        cinematicCamera.Priority = cinematicPriority;

        Debug.Log("[TowerController] Cinematic camera activated");

        yield return new WaitForSecondsRealtime(cinematicDuration);

        cinematicCamera.Priority = originalPriority;
        Debug.Log("[TowerController] Cinematic camera reset");

        cinematicCoroutine = null;
    }

    #endregion

    #region Pause System

    void PauseGame()
    {
        if (!isPaused)
        {
            isPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    void ResumeGame()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = previousTimeScale;
        }
    }

    void TryResumeGame()
    {
        if (!ui.terminalPanel.activeSelf && !ui.dialoguePanel.activeSelf)
        {
            ResumeGame();
        }
    }

    #endregion

    #region Public Methods

    public void ResetTower()
    {
        // Reset state
        mistakeCount = 0;
        hasGivenHint = false;
        wrongAttempts.Clear();
        isHoldingButton = false;
        buttonCondition = "";
        input = "";
        machineEffectsActive = false;
        lessonCleared = false;

        // Reset machine values
        machineValues.timerValue = MachineValues.TIMER_MAX;
        machineValues.healthValue = MachineValues.HEALTH_MAX;

        // Reset tower
        if (towerSettings.tower != null)
            towerSettings.tower.ResetTower();

        // Stop all effects
        StopMachineLoop();
        StopMachineSound();

        // Don't directly deactivate spike traps - let MachineGroup handle it
        if (towerSettings.tower != null && towerSettings.tower.machine != null)
        {
            towerSettings.tower.machine.DeactivateMachine();
        }

        // Reset colliders
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        // Re-enable lesson collider if it was disabled
        if (colliderToDeactivateAfterClear != null)
            colliderToDeactivateAfterClear.enabled = true;

        // Hide UI
        if (ui.terminalPanel != null)
            ui.terminalPanel.SetActive(false);
        if (ui.dialoguePanel != null)
            ui.dialoguePanel.SetActive(false);

        // Reset state
        ChangeState(TowerState.Inactive);

        // Resume game if paused
        TryResumeGame();
    }

    #endregion

    #region Debug Visualization

    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D box)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
        }

        // Draw status indicator
        Color stateColor = currentState switch
        {
            TowerState.Completed => Color.green,
            TowerState.Failed => Color.red,
            TowerState.Running => Color.cyan,
            TowerState.Configured => Color.yellow,
            _ => Color.gray
        };

        Gizmos.color = stateColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    #endregion
}