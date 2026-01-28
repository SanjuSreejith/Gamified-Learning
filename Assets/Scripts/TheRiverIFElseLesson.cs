using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class RiverIfElseLessonController2D : MonoBehaviour
{
    /* ================= UI ================= */
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;
    public Sprite abelPortrait;
    public Sprite kuttanPortrait;

    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    /* ================= PLAYER ================= */
    public JetpackController2D jetpack;
    public PlayerJetpackAnimator2D animatorController;

    /* ================= RIVER DATA ================= */
    public int[] riverDistances = { 8, 5, 3 };
    int currentRiverIndex = 0;

    /* ================= ENERGY ================= */
    public int playerEnergy = 10;

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
        dialoguePanel.SetActive(false);
        terminalPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active) return;
        if (!other.CompareTag("Player")) return;

        active = true;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        Speak("Abel", "You reached the crossing.");
        yield return Wait();

        Speak("Kuttan", "Three rivers. Different distances.");
        yield return Wait();

        Speak("Abel",
            "Jetpack is useless without logic.\n" +
            "Energy must be spent correctly.");
        yield return Wait();

        Speak("Kuttan",
            "If the distance is long, spend more energy.\n" +
            "Else, spend less.");
        yield return Wait();

        EquipJetpack();
        OpenTerminal();
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

    void Update()
    {
        if (!editing) return;
        HandleTyping();
        UpdateTerminal();
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
                    editing = false;
                    terminalPanel.SetActive(false);
                    ValidateLogic();
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
            "<color=#9CDCFE>river_distance</color> = " + riverDistances[currentRiverIndex] + "\n" +
            "<color=#9CDCFE>energy</color> = " + playerEnergy + "\n\n" +
            (string.IsNullOrEmpty(ifLine) ? "if ____________:" : ifLine) + "\n" +
            (string.IsNullOrEmpty(bodyLine) ? "    ____________" : bodyLine) + "\n\n" +
            "<color=#6A9955># Example\n# if river_distance > 5:\n#     energy -= 5</color>";
    }

    /* ================= LOGIC VALIDATION ================= */

    void ValidateLogic()
    {
        if (!ifLine.Contains("river_distance") || !ifLine.EndsWith(":"))
        {
            Speak("Abel", "Your IF condition is incorrect.");
            return;
        }

        if (!bodyLine.Contains("energy"))
        {
            Speak("Kuttan", "You must reduce energy.");
            return;
        }

        ApplyEnergyLogic();
    }

    void ApplyEnergyLogic()
    {
        int distance = riverDistances[currentRiverIndex];

        if (distance > 5)
            playerEnergy -= 5;
        else
            playerEnergy -= 3;

        if (playerEnergy < 0)
        {
            jetpack.FailFall();
            Speak("Kuttan", "Energy mismatch. You fall.");
            return;
        }

        jetpack.FlyToNextPoint();
        currentRiverIndex++;
    }

    /* ================= HELPERS ================= */

    void EquipJetpack()
    {
        jetpack.Equip();
        animatorController.SetJetpack(true);
    }

    void Speak(string speaker, string text)
    {
        dialoguePanel.SetActive(true);
        speakerText.text = speaker;
        dialogueText.text = text;
        speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;
        waitingForDialogue = true;
    }

    IEnumerator Wait()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
        dialoguePanel.SetActive(false);
    }
}

