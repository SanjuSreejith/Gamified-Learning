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

    /* ================= BRIDGE ================= */
    public BridgeBreak3Controller2D bridgeController;

    /* ================= SCENE CONTROL ================= */
    public NPCSmartFollower2D[] friendlyNPCs;
    public EnemyAI2D_Smart[] enemies;
    public Transform npcHoldPoint;
    public float enemySlowMultiplier = 0.25f;

    public CanvasGroup fadePanel;
    public float fadeSpeed = 2f;

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
        dialoguePanel.SetActive(false);

        if (fadePanel)
        {
            fadePanel.alpha = 0;
            fadePanel.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active) return;
        if (!other.CompareTag("Player")) return;

        active = true;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(FadeAndPrepare());
    }

    IEnumerator FadeAndPrepare()
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

        Speak(
            "Abel",
            "This time, you write everything.\n" +
            "If. Condition. Indentation.\n" +
            "Code decides what happens."
        );
    }

    void Update()
    {
        if (!active) return;

        if (waitingForDialogue && Input.GetKeyDown(KeyCode.Return))
        {
            waitingForDialogue = false;
            dialoguePanel.SetActive(false);
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
    }

    /* ================= DIALOGUE ================= */

    void Speak(string speaker, string text)
    {
        dialoguePanel.SetActive(true);
        speakerText.text = speaker;
        dialogueText.text = text;
        speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;
        waitingForDialogue = true;
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
