using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class AdvancedBridgeTerminalController : MonoBehaviour
{
    /* ================= TERMINAL UI ================= */
    [Header("Terminal UI")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    [Header("Hint System")]
    public BotHintSystem hintSystem;                     // <-- NEW

    /* ================= BRIDGE ================= */
    [Header("Bridge")]
    public BridgeBreak2Controller2D bridgeController;

    /* ================= DIALOGUE UI ================= */
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    TMPTypewriter typewriter;
    public Image speakerImage;
    public Sprite abelPortrait;
    public Sprite kuttanPortrait;

    /* ================= SCENE CONTROL ================= */
    [Header("Scene Control")]
    public NPCSmartFollower2D[] friendlyNPCs;
    public EnemyAI2D_Smart[] enemies;
    public Transform npcHoldPoint;
    public float enemySlowMultiplier = 0.25f;

    /* ================= FADE ================= */
    [Header("Fade")]
    public CanvasGroup fadePanel;
    public float fadeSpeed = 2f;

    /* ================= BEHAVIOR TOGGLES ================= */   // <-- NEW
    [Header("Behavior")]
    public bool teleportNPCsToHoldPoint = true;   // false = rush without fade
    public float rushSpeedMultiplier = 2f;        // (requires NPC support)
    public bool pauseGameDuringDialogue = true;   // freeze time when dialogue/terminal is active

    /* ================= STATE ================= */
    bool active;
    bool editing;
    bool waitingForDialogueClose;
    bool lessonCompleted;   // 🔒 IMPORTANT LOCK

    string conditionInput = "";

    enum TeachState
    {
        None,
        Teaching,
        ReadyToEdit
    }

    TeachState teachState = TeachState.None;
    int teachIndex = 0;

    /* ================= INIT ================= */

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        terminalPanel.SetActive(false);
        //dialoguePanel.SetActive(false);

        if (fadePanel)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
        typewriter = dialogueText.GetComponent<TMPTypewriter>();

        // Optional: set up default hints for terminal phase
        if (hintSystem != null)
        {
            hintSystem.SetHints(new string[]
            {
                "people_count is the number of people on the bridge.",
                "You can use ==, >, <, >=, <=, != to compare.",
                "Examples: people_count == 3, people_count > 5.",
                "The condition must be a valid Python expression."
            });
        }
    }

    /* ================= TRIGGER ================= */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active) return;
        if (!other.CompareTag("Player")) return;

        active = true;
        GetComponent<Collider2D>().enabled = false;

        StartCoroutine(PrepareScene());
    }

    /* ================= PREPARATION (FADE/TELEPORT OR RUSH) ================= */

    IEnumerator PrepareScene()
    {
        if (teleportNPCsToHoldPoint)
        {
            yield return StartCoroutine(FadeAndTeleport());
        }
        else
        {
            yield return StartCoroutine(RushToHoldPoint());
        }

        // After NPCs are in place, start teaching
        teachState = TeachState.Teaching;
        teachIndex = 0;
        ShowTeachingDialogue();
    }

    IEnumerator FadeAndTeleport()
    {
        if (fadePanel)
        {
            fadePanel.gameObject.SetActive(true);
            while (fadePanel.alpha < 1f)
            {
                fadePanel.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
        }

        // Teleport friendly NPCs
        foreach (var npc in friendlyNPCs)
            if (npc) npc.TeleportToHoldPoint(npcHoldPoint);

        // Slow enemies
        foreach (var enemy in enemies)
            if (enemy) enemy.SetSlow(true, enemySlowMultiplier);

        yield return new WaitForSeconds(0.15f);

        if (fadePanel)
        {
            while (fadePanel.alpha > 0f)
            {
                fadePanel.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
            fadePanel.gameObject.SetActive(false);
        }
    }

    IEnumerator RushToHoldPoint()
    {
        // Slow enemies immediately
        foreach (var enemy in enemies)
            if (enemy) enemy.SetSlow(true, enemySlowMultiplier);

        // Tell each NPC to move to the hold point
        foreach (var npc in friendlyNPCs)
        {
            if (npc == null) continue;
            npc.MoveToHoldPoint(npcHoldPoint);
            // (Optional: if NPC script supports speed multiplier, set it here)
        }

        // Wait until all NPCs have arrived
        while (!AreNPCsAtHoldPoint())
        {
            yield return null; // check every frame
        }

        // Brief pause for visual coherence
        yield return new WaitForSeconds(0.2f);
    }

    bool AreNPCsAtHoldPoint()
    {
        foreach (var npc in friendlyNPCs)
        {
            if (npc == null) continue;
            if (!npc.IsAtHoldPoint()) return false;
        }
        return true;
    }

    /* ================= UPDATE ================= */

    void Update()
    {
        if (!active) return;

        // Close dialogue
        if (waitingForDialogueClose && Input.GetKeyDown(KeyCode.Return))
        {
            // First Enter → finish typing
            if (typewriter != null && typewriter.IsTyping())
            {
                typewriter.Skip();
                return;
            }

            // Second Enter → close dialogue
            waitingForDialogueClose = false;
            dialoguePanel.SetActive(false);

            // Unpause when dialogue closes
            if (pauseGameDuringDialogue)
                Time.timeScale = 1f;

            if (teachState == TeachState.Teaching)
                ShowTeachingDialogue();
        }

        // 🔒 OPEN TERMINAL ONLY ONCE
        if (!lessonCompleted &&
            teachState == TeachState.ReadyToEdit &&
            !editing &&
            !waitingForDialogueClose &&
            Input.GetKeyDown(KeyCode.E))
        {
            OpenTerminal();
        }

        if (!editing) return;

        HandleTyping();
        UpdateTerminal();
    }

    /* ================= TEACHING ================= */

    void ShowTeachingDialogue()
    {
        if (teachIndex >= 11) // after last teaching line
            return;

        string speaker = "";
        string text = "";

        switch (teachIndex)
        {
            case 0: speaker = "Abel"; text = "This bridge follows rules."; break;
            case 1: speaker = "Kuttan"; text = "Rules ask questions. True or false."; break;
            case 2: speaker = "Abel"; text = "people_count == 3 means EXACTLY three."; break;
            case 3: speaker = "Kuttan"; text = "Not two. Not four. Only three."; break;
            case 4: speaker = "Abel"; text = "people_count > 3 means more than three."; break;
            case 5: speaker = "Kuttan"; text = "Four breaks it. Five breaks it."; break;
            case 6: speaker = "Abel"; text = ">= means three or more."; break;
            case 7: speaker = "Abel"; text = "< means less than."; break;
            case 8: speaker = "Kuttan"; text = "!= means NOT equal."; break;
            case 9: speaker = "Abel"; text = "You edit only the condition."; break;
            case 10:
                speaker = "Abel";
                text = "Press E. Decide the rule.";
                teachState = TeachState.ReadyToEdit;
                break;
            default:
                return;
        }

        teachIndex++;
        Speak(speaker, text);
    }

    /* ================= TERMINAL ================= */

    void OpenTerminal()
    {
        editing = true;
        conditionInput = "";
        terminalPanel.SetActive(true);
        UpdateTerminal();

        // Pause game while terminal is open
        if (pauseGameDuringDialogue)
            Time.timeScale = 0f;

        // Enable hints when terminal opens
        if (hintSystem != null)
            hintSystem.EnableHints();
    }

    void CloseTerminal()
    {
        editing = false;
        terminalPanel.SetActive(false);

        // Unpause after terminal closes
        if (pauseGameDuringDialogue)
            Time.timeScale = 1f;

        // Disable hints when terminal closes
        if (hintSystem != null)
            hintSystem.DisableHints();
    }

    void HandleTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\n' || c == '\r')
            {
                CloseTerminal();
                ValidateAndExecute();
                return;
            }

            if (c == '\b')
            {
                if (conditionInput.Length > 0)
                    conditionInput = conditionInput.Remove(conditionInput.Length - 1);
            }
            else
            {
                conditionInput += c;
            }
        }
    }

    void UpdateTerminal()
    {
        terminalText.text =
            "<color=#9CDCFE>people_count</color> = " + bridgeController.peopleCount + "\n\n" +
            "<color=#C586C0>if</color> " +
            "<color=#FFD700>" +
            (string.IsNullOrEmpty(conditionInput) ? "___________" : conditionInput) +
            "</color>:\n" +
            "    <color=#DCDCAA>break_bridge</color>()\n\n" +
            "<color=#6A9955># Edit only the condition</color>";
    }

    /* ================= VALIDATION ================= */

    void ValidateAndExecute()
    {
        if (string.IsNullOrWhiteSpace(conditionInput))
        {
            Speak("Kuttan", "An empty condition always fails.");
            return;
        }

        string ifLine = "if " + conditionInput + ":";
        bridgeController.EvaluateCondition(ifLine);

        // 🔒 PERMANENT LOCK
        lessonCompleted = true;
        teachState = TeachState.None;

        RestoreScene();
    }

    /* ================= DIALOGUE ================= */
    void Speak(string speaker, string text)
    {
        dialoguePanel.SetActive(true);
        speakerText.text = speaker;
        speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;

        if (typewriter != null)
            typewriter.Play(text);
        else
            dialogueText.text = text;

        waitingForDialogueClose = true;
        DialogueBacklogManager.Instance?.AddLine(speaker, text);

        // Pause game when dialogue appears
        if (pauseGameDuringDialogue)
            Time.timeScale = 0f;
    }

    /* ================= RESTORE ================= */

    void RestoreScene()
    {
        foreach (var enemy in enemies)
            if (enemy) enemy.SetSlow(false, 1f);

        foreach (var npc in friendlyNPCs)
            if (npc) npc.ReleaseFromHoldPoint();

        // Ensure time is unpaused at the very end (just in case)
        if (pauseGameDuringDialogue)
            Time.timeScale = 1f;
    }
}