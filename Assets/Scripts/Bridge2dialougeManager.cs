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

    /* ================= BRIDGE ================= */
    [Header("Bridge")]
    public BridgeBreak2Controller2D bridgeController;

    /* ================= DIALOGUE UI ================= */
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
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
        dialoguePanel.SetActive(false);

        if (fadePanel)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
    }

    /* ================= TRIGGER ================= */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active) return;
        if (!other.CompareTag("Player")) return;

        active = true;
        GetComponent<Collider2D>().enabled = false;

        StartCoroutine(FadeAndPrepareScene());
    }

    /* ================= FADE + NPC CONTROL ================= */

    IEnumerator FadeAndPrepareScene()
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

        foreach (var npc in friendlyNPCs)
            if (npc) npc.TeleportToHoldPoint(npcHoldPoint);

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

        teachState = TeachState.Teaching;
        teachIndex = 0;
        ShowTeachingDialogue();
    }

    /* ================= UPDATE ================= */

    void Update()
    {
        if (!active) return;

        // Close dialogue
        if (waitingForDialogueClose && Input.GetKeyDown(KeyCode.Return))
        {
            waitingForDialogueClose = false;
            dialoguePanel.SetActive(false);

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
    }

    void CloseTerminal()
    {
        editing = false;
        terminalPanel.SetActive(false);
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
        dialogueText.text = text;
        speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;
        waitingForDialogueClose = true;
    }

    /* ================= RESTORE ================= */

    void RestoreScene()
    {
        foreach (var enemy in enemies)
            if (enemy) enemy.SetSlow(false, 1f);

        foreach (var npc in friendlyNPCs)
            if (npc) npc.ReleaseFromHoldPoint();
    }
}
