using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class BridgeDialogueSequenceController : MonoBehaviour
{
    /* ================= UI ================= */
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;

    [Header("Terminal UI")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    /* ================= Portraits ================= */
    public Sprite abelPortrait;
    public Sprite kuttanPortrait;
    bool waitingToCloseReaction;

    [Header("Scene Control")]
    public NPCSmartFollower2D[] friendlyNPCs;
    public EnemyAI2D_Smart[] enemies;
    public Transform npcHoldPoint; // where Abel & Kuttan should stop
    public float enemySlowMultiplier = 0.25f;
    [Header("Fade")]
    public CanvasGroup fadePanel;
    public float fadeSpeed = 2f;
    [Header("Bridge")]
    public BridgeBreakController2D bridgeController;



    /* ================= Dialogue ================= */
    public DialogueLine[] lines;

    /* ================= State ================= */
    int index;
    bool active;
    bool waitingForInput;
    bool terminalOpened;
    bool npcsReady;

    enum TerminalState { Viewing, Editing, Confirming, Closed }
    TerminalState terminalState = TerminalState.Viewing;

    int peopleCount = 3;
    int conditionValue = 20;
    string typedNumber = "";

    enum HighlightMode { None, If, Number, Indent }
    HighlightMode highlightMode = HighlightMode.None;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        if (lines == null || lines.Length == 0)
        {
            lines = new DialogueLine[]
            {
                new DialogueLine("Kuttan", "No… this shouldn't be happening."),
                new DialogueLine("Kuttan", "NULL can't enter this world."),
                new DialogueLine("Abel", "I know."),
                new DialogueLine("Abel", "But this world still follows simple rules."),

                new DialogueLine("Abel", "Think about the bridge."),
                new DialogueLine("Abel", "If too many people go on it…"),
                new DialogueLine("Abel", "Then the bridge breaks."),
                new DialogueLine("Abel", "If not, nothing happens."),

                new DialogueLine("Abel", "This word 'if' asks a question."),
                new DialogueLine("Abel", "This number is the limit."),
                new DialogueLine("Abel", "See the space before the line."),
                new DialogueLine("Abel", "That space means: do this only if the answer is yes."),

                new DialogueLine("Abel", "Now edit the number."),
                new DialogueLine("Abel", "Press E to change it."),
                new DialogueLine("Abel", "Press Enter to confirm.")
            };
        }

        foreach (var l in lines)
            l.portrait = l.speaker == "Abel" ? abelPortrait : kuttanPortrait;

        terminalPanel.SetActive(false);
        dialoguePanel.SetActive(false);
    }

    void PrepareNPCsForDialogue()
    {
        StartCoroutine(FadeAndTeleportNPCs());
    }

    IEnumerator FadeAndTeleportNPCs()
    {
        // Fade out
        fadePanel.gameObject.SetActive(true);
        while (fadePanel.alpha < 1f)
        {
            fadePanel.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Teleport friendly NPCs
        foreach (var npc in friendlyNPCs)
        {
            if (npc == null) continue;
            npc.TeleportToHoldPoint(npcHoldPoint);
        }

        // Slow enemies
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.SetSlow(true, enemySlowMultiplier);
        }

        yield return new WaitForSeconds(0.15f);

        // Fade back in
        while (fadePanel.alpha > 0f)
        {
            fadePanel.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        fadePanel.alpha = 0f;
        fadePanel.gameObject.SetActive(false);

        // Allow dialogue to start
        npcsReady = true;
        dialoguePanel.SetActive(true);
        index = 0;
        ShowLine();
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 🔒 Disable trigger so it can NEVER fire again
        GetComponent<Collider2D>().enabled = false;

        if (active) return;

        active = true;
        npcsReady = false;

        PrepareNPCsForDialogue(); // fade + teleport + slow enemies
    }

    void Update()
    {
        // Wait for NPCs to reach hold point before starting dialogue
        if (active && !npcsReady)
        {
            if (AreNPCsAtHoldPoint())
            {
                npcsReady = true;
                dialoguePanel.SetActive(true);
                index = 0;
                ShowLine();
            }
            return;
        }

        if (!active) return;

        if (waitingForInput && Input.GetKeyDown(KeyCode.Return))
            NextLine();

        if (!terminalOpened) return;

        if (terminalState == TerminalState.Viewing && Input.GetKeyDown(KeyCode.E))
            BeginEdit();

        if (terminalState == TerminalState.Editing)
            HandleTyping();

        if (terminalState == TerminalState.Confirming)
        {
            if (Input.GetKeyDown(KeyCode.E))
                BeginEdit();

            if (Input.GetKeyDown(KeyCode.Return))
                ConfirmAndClose();
        }

        if (waitingToCloseReaction && Input.GetKeyDown(KeyCode.Return))
        {
            dialoguePanel.SetActive(false);
            waitingToCloseReaction = false;
            RestoreEnemies();
            RestoreNPCs();
            active = false;
        }
    }

    /* ================= Dialogue ================= */
    void ShowLine()
    {
        if (index >= lines.Length)
        {
            EndDialogue();
            return;
        }

        speakerText.text = lines[index].speaker;
        dialogueText.text = lines[index].text;
        speakerImage.sprite = lines[index].portrait;

        UpdateHighlightMode();
        UpdateTerminal();

        waitingForInput = true;
    }

    void NextLine()
    {
        waitingForInput = false;
        index++;
        ShowLine();
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        terminalOpened = true;
        terminalPanel.SetActive(true);
        terminalState = TerminalState.Viewing;
        UpdateTerminal();
    }

    /* ================= Highlight ================= */
    void UpdateHighlightMode()
    {
        highlightMode = HighlightMode.None;

        if (index >= 8)
        {
            terminalOpened = true;
            terminalPanel.SetActive(true);
        }

        if (index == 8) highlightMode = HighlightMode.If;
        else if (index == 9) highlightMode = HighlightMode.Number;
        else if (index == 10 || index == 11) highlightMode = HighlightMode.Indent;
    }

    /* ================= Terminal ================= */
    void BeginEdit()
    {
        terminalState = TerminalState.Editing;
        typedNumber = conditionValue.ToString();
        highlightMode = HighlightMode.None;
        UpdateTerminal();
    }

    void HandleTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsDigit(c))
                typedNumber += c;

            if (c == '\b' && typedNumber.Length > 0)
                typedNumber = typedNumber.Remove(typedNumber.Length - 1);

            if (c == '\n' || c == '\r')
            {
                if (int.TryParse(typedNumber, out int value))
                    conditionValue = value;

                terminalState = TerminalState.Confirming;
                UpdateTerminal();
            }
        }

        UpdateTerminal();
    }

    void ConfirmAndClose()
    {
        terminalState = TerminalState.Closed;
        terminalPanel.SetActive(false);

        // 🔥 APPLY PYTHON CONDITION TO REAL BRIDGE
        bridgeController.EvaluateCondition(conditionValue);

        ReactToLogic();
    }


    void UpdateTerminal()
    {
        string ifText = highlightMode == HighlightMode.If
            ? "<b><color=#DDA0FF>if</color></b>"
            : "<color=#C586C0>if</color>";

        string numberText =
            highlightMode == HighlightMode.Number
                ? $"<b><color=#FFD700>{conditionValue}</color></b>"
                : conditionValue.ToString();

        if (terminalState == TerminalState.Editing)
            numberText = $"<b><color=#FFD700>{typedNumber}</color></b>";

        string indentLine =
            highlightMode == HighlightMode.Indent
                ? "<b><color=#7CFC00>    bridge_break()</color></b>"
                : "    <color=#DCDCAA>bridge_break</color>()";

        string footer = terminalState switch
        {
            TerminalState.Viewing => "# Press E to edit",
            TerminalState.Editing => "# Type number, Enter to finish",
            TerminalState.Confirming => "# Enter to confirm | E to edit again",
            _ => ""
        };

        terminalText.text =
            "<color=#9CDCFE>people_count</color> = " + peopleCount + "\n\n" +
            ifText + " people_count > " + numberText + ":\n" +
            indentLine + "\n\n" +
            "<color=#6A9955>" + footer + "</color>";
    }

    /* ================= Reaction ================= */
    void ReactToLogic()
    {
        dialoguePanel.SetActive(true);

        if (conditionValue < 2)
            Speak("Abel", "That limit is too small. The bridge breaks early.");
        else if (conditionValue < 20)
            Speak("Abel", "Good. The bridge stays safe.");
        else
            Speak("Kuttan", "That number feels dangerous…");

        waitingToCloseReaction = true;
    }

    void Speak(string speaker, string text)
    {
        speakerText.text = speaker;
        dialogueText.text = text;
        speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;
    }

    void RestoreEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.SetSlow(false, 1f);
        }
    }

    void RestoreNPCs()
    {
        foreach (var npc in friendlyNPCs)
        {
            if (npc == null) continue;
            npc.ReleaseFromHoldPoint();
        }
    }
}

/* ================= Data ================= */
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 4)] public string text;
    public Sprite portrait;

    public DialogueLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}