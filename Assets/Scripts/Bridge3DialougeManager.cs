using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class AdvancedBridgeTerminalController_Bridge3 : MonoBehaviour
{
    /* ================= UI ================= */
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;
    public Sprite abelPortrait;
    public Sprite kuttanPortrait;
    TMPTypewriter typewriter;

    /* ================= HINT SYSTEM ================= */          // <-- NEW
    public BotHintSystem hintSystem;

    /* ================= BRIDGE ================= */
    public BridgeBreak3Controller2D bridgeController;

    /* ================= SCENE CONTROL ================= */
    public NPCSmartFollower2D[] friendlyNPCs;
    public EnemyAI2D_Smart[] enemies;
    public Transform npcHoldPoint;
    public float enemySlowMultiplier = 0.25f;

    public CanvasGroup fadePanel;
    public float fadeSpeed = 2f;

    /* ================= BEHAVIOR TOGGLES ================= */    // <-- NEW
    [Header("Behavior")]
    public bool teleportNPCsToHoldPoint = true;   // false = rush without fade
    public float rushSpeedMultiplier = 2f;        // (requires NPC support)
    public bool pauseGameDuringDialogue = true;   // freeze time when dialogue/terminal is active
    public Collider2D block;

    /* ================= STATE ================= */
    bool active;
    bool editing;
    bool waitingForDialogue;

    string ifLine = "";
    string bodyLine = "";
    int currentLine;

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
            fadePanel.alpha = 0;
            fadePanel.gameObject.SetActive(false);
        }
        typewriter = dialogueText.GetComponent<TMPTypewriter>();

        // Optional: set up default hints for terminal phase
        if (hintSystem != null)
        {
            hintSystem.SetHints(new string[]
            {
                "people_count is the number of people on the bridge.",
                "The if line must start with 'if' and end with ':'.",
                "The body must be indented (4 spaces) and call break_bridge().",
                "Spelling and indentation matter in Python."
            });
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active) return;
        if (!other.CompareTag("Player")) return;

        active = true;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(PrepareScene());          // <-- changed from FadeAndPrepare
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

        // After NPCs are in place, start the first dialogue
        Speak(
            "Abel",
            "This time, you write everything.\n" +
            "If. Condition. Indentation.\n" +
            "Code decides what happens."
        );
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

        yield return new WaitForSeconds(0.2f);

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
        if (waitingForDialogue && Input.GetKeyDown(KeyCode.Return))
        {
            // First Enter → finish typing
            if (typewriter != null && typewriter.IsTyping())
            {
                typewriter.Skip();
                return;
            }

            // Second Enter → close dialogue
            waitingForDialogue = false;
            dialoguePanel.SetActive(false);

            // Unpause when dialogue closes
            if (pauseGameDuringDialogue)
                Time.timeScale = 1f;
        }

        if (!editing && !waitingForDialogue && Input.GetKeyDown(KeyCode.E))
            OpenTerminal();

        if (!editing) return;

        HandleTyping();
        UpdateTerminal();
    }

    /* ================= TERMINAL ================= */

    void OpenTerminal()
    {
        editing = true;
        currentLine = 0;
        ifLine = "";
        bodyLine = "";
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
                currentLine++;
                if (currentLine > 1)
                {
                    CloseTerminal();
                    ValidateAndExecute();
                }
                return;
            }

            if (c == '\b')
            {
                if (currentLine == 0 && ifLine.Length > 0)
                    ifLine = ifLine.Remove(ifLine.Length - 1);
                else if (currentLine == 1 && bodyLine.Length > 0)
                    bodyLine = bodyLine.Remove(bodyLine.Length - 1);
            }
            else
            {
                if (currentLine == 0) ifLine += c;
                else bodyLine += c;
            }
        }
    }

    void UpdateTerminal()
    {
        terminalText.text =
            "<color=#9CDCFE>people_count</color> = " + bridgeController.peopleCount + "\n\n" +
            (string.IsNullOrEmpty(ifLine) ? "if ____________:" : ifLine) + "\n" +
            (string.IsNullOrEmpty(bodyLine) ? "    ____________" : bodyLine) + "\n\n" +
            "<color=#6A9955>" +
            "# Type the full rule\n" +
            "# Spelling matters\n" +
            "# Indentation matters\n" +
            "</color>";
    }

    /* ================= VALIDATION ================= */

    void ValidateAndExecute()
    {
        if (!ifLine.StartsWith("if") || !ifLine.EndsWith(":"))
        {
            Speak("Kuttan", "That IF line is wrong.");
            return;
        }

        if (!ifLine.Contains("people_count"))
        {
            Speak("Abel", "The variable name must be exact.");
            return;
        }

        if (!bodyLine.StartsWith("    "))
        {
            Speak("Abel", "Indentation decides scope.");
            return;
        }

        if (bodyLine.Trim() != "break_bridge()")
        {
            Speak("Kuttan", "That function does nothing.");
            return;
        }

        bridgeController.SetCondition(ifLine);

        Speak("Abel", "Good. The bridge now listens.");

        RestoreScene();

        // ✅ DISABLE BLOCK AFTER SUCCESS
        if (block != null)
        {
            block.enabled = false;
            Debug.Log("Bridge 3 lesson completed. Block disabled.");
        }
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

        waitingForDialogue = true;
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