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

    public GameObject jetpackPanel;
    public TextMeshProUGUI energyText;

    /* ================= PLAYER ================= */
    public JetpackController2D jetpack;
    public PlayerJetpackAnimator2D animatorController;

    /* ================= DATA ================= */
    public int[] riverDistances = { 10, 2, 6 };
    int currentRiverIndex = 0;

    public int playerEnergy = 100;
    const int ENERGY_RATE = 4;

    /* ================= TERMINAL INPUT ================= */
    string ifLine = "";
    string ifBody = "";
    string elifLine = "";
    string elifBody = "";
    string elseBody = "";

    const string ELSE_LINE = "else:";

    int currentLine = 0;
    bool editing = false;
    bool active = false;
    bool conceptTaught = false;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Start()
    {
        dialoguePanel.SetActive(false);
        terminalPanel.SetActive(false);
        jetpackPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active || !other.CompareTag("Player")) return;
        active = true;
        StartCoroutine(IntroSequence());
    }

    /* ================= INTRO SEQUENCE ================= */

    IEnumerator IntroSequence()
    {
        Speak("Abel", "No bridge ahead.");
        yield return Wait();

        Speak("Kuttan", "Looks like we can only cross by flying.");
        yield return Wait();

        Speak("Abel", "But a jetpack is like a bike. No fuel, no ride.");
        yield return Wait();

        // 🔹 ANNOUNCE ALL RIVER DISTANCES
        Speak("Kuttan", $"Three rivers ahead: first one is {riverDistances[0]} meters.");
        yield return Wait();

        Speak("Abel", $"Second river is short — about {riverDistances[1]} meters.");
        yield return Wait();

        Speak("Kuttan", $"Last river is medium — {riverDistances[2]} meters.");
        yield return Wait();

        // 🔹 EXPLAIN ENERGY CALCULATION
        Speak("Abel",
            "For the 10-meter river, we need 40 energy.\n" +
            $"That's {ENERGY_RATE} energy per meter.");
        yield return Wait();

        Speak("Kuttan",
            "Use that rate to calculate for other distances.\n" +
            "We have 100 energy to start with.");
        yield return Wait();

        // 🔹 TEACH IF / ELIF / ELSE ONCE
        if (!conceptTaught)
        {
            Speak("Abel",
                "We use if, elif, and else for decisions.\n" +
                "Let me show you how it works.");
            yield return Wait();

            terminalPanel.SetActive(true);
            terminalText.text =
                "<color=#9CDCFE>river_length</color> = 10\n" +
                "<color=#9CDCFE>energy</color> = 100\n\n" +
                "if river_length > 8:\n" +
                "    energy -= 40\n" +
                "elif river_length > 4:\n" +
                "    energy -= 20\n" +
                "else:\n" +
                "    energy -= 8";

            Speak("Kuttan", "This checks if the river is long, medium, or short.");
            yield return Wait();
            Speak("Abel", "Only one condition runs — the first one that matches!");
            yield return Wait();

            terminalPanel.SetActive(false);
            conceptTaught = true;
        }

        EquipJetpack();
        OpenTerminal();
    }

    /* ================= TERMINAL ================= */

    void OpenTerminal()
    {
        editing = true;
        currentLine = 0;
        ifLine = ifBody = elifLine = elifBody = elseBody = "";
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
            if (c == '\t')
            {
                AddText("    ");
                continue;
            }

            if (c == '\n' || c == '\r')
            {
                // Skip the fixed "else:" line (index 4)
                if (currentLine == 3)
                    currentLine = 5;
                else
                    currentLine++;

                // Auto-indent bodies
                if (currentLine == 1 || currentLine == 3 || currentLine == 5)
                    AddTextToLine(currentLine, "    ");

                // ENTER after else body completes
                if (currentLine > 5)
                {
                    editing = false;
                    terminalPanel.SetActive(false);
                    ValidateLogic();
                }
                return;
            }

            if (c == '\b') RemoveChar();
            else AddText(c.ToString());
        }
    }

    void AddText(string t) => AddTextToLine(currentLine, t);

    void AddTextToLine(int line, string t)
    {
        switch (line)
        {
            case 0: ifLine += t; break;
            case 1: ifBody += t; break;
            case 2: elifLine += t; break;
            case 3: elifBody += t; break;
            case 5: elseBody += t; break;
        }
    }

    void RemoveChar()
    {
        switch (currentLine)
        {
            case 0: if (ifLine.Length > 0) ifLine = ifLine.Substring(0, ifLine.Length - 1); break;
            case 1: if (ifBody.Length > 0) ifBody = ifBody.Substring(0, ifBody.Length - 1); break;
            case 2: if (elifLine.Length > 0) elifLine = elifLine.Substring(0, elifLine.Length - 1); break;
            case 3: if (elifBody.Length > 0) elifBody = elifBody.Substring(0, elifBody.Length - 1); break;
            case 5: if (elseBody.Length > 0) elseBody = elseBody.Substring(0, elseBody.Length - 1); break;
        }
    }

    void UpdateTerminal()
    {
        terminalText.text =
            "<color=#9CDCFE>river_length</color> = ?\n" +
            "<color=#9CDCFE>energy</color> = 100\n\n" +
            (string.IsNullOrEmpty(ifLine) ? "if ____________:" : ifLine) + "\n" +
            (string.IsNullOrEmpty(ifBody) ? "    energy -= ______" : ifBody) + "\n" +
            (string.IsNullOrEmpty(elifLine) ? "elif ____________:" : elifLine) + "\n" +
            (string.IsNullOrEmpty(elifBody) ? "    energy -= ______" : elifBody) + "\n" +
            ELSE_LINE + "\n" +
            (string.IsNullOrEmpty(elseBody) ? "    energy -= ______" : elseBody);
    }

    /* ================= VALIDATION ================= */

    void ValidateLogic()
    {
        if (!ifLine.StartsWith("if") || !ifLine.EndsWith(":"))
        {
            Speak("Abel", "Start with 'if' and end with ':'.");
            OpenTerminal();
            return;
        }

        if (!ifBody.StartsWith("    energy -="))
        {
            Speak("Kuttan", "The IF block must reduce energy.");
            OpenTerminal();
            return;
        }

        if (!elifLine.StartsWith("elif") || !elifLine.EndsWith(":"))
        {
            Speak("Abel", "Use 'elif' for the second condition.");
            OpenTerminal();
            return;
        }

        if (!elifBody.StartsWith("    energy -="))
        {
            Speak("Kuttan", "The ELIF block must reduce energy too.");
            OpenTerminal();
            return;
        }

        if (!elseBody.StartsWith("    energy -="))
        {
            Speak("Abel", "The ELSE block must also reduce energy.");
            OpenTerminal();
            return;
        }

        StartCoroutine(ExecuteLogic());
    }

    /* ================= EXECUTION ================= */

    IEnumerator ExecuteLogic()
    {
        int distance = riverDistances[currentRiverIndex];
        int energyUsed = distance * ENERGY_RATE;
        playerEnergy -= energyUsed;

        jetpackPanel.SetActive(true);
        energyText.text =
            $"Distance: {distance}m\nEnergy used: {energyUsed}\nRemaining: {playerEnergy}";

        if (playerEnergy < 0)
        {
            jetpack.FailFall();
            Speak("Kuttan", "Out of fuel mid-air! Need better planning.");
            yield break;
        }

        Speak("Abel", $"Good! Used {energyUsed} energy for {distance}m river.");
        yield return StartCoroutine(Wait());

        jetpack.FlyToNextPoint();
        currentRiverIndex++;

        if (currentRiverIndex < riverDistances.Length)
        {
            yield return StartCoroutine(NextRiver());
        }
        else
        {
            Speak("Kuttan", "All rivers crossed successfully!");
            yield return StartCoroutine(Wait());
            Speak("Abel", "Excellent logic. You've mastered if/elif/else!");
        }
    }

    IEnumerator NextRiver()
    {
        yield return new WaitForSeconds(1.2f);
        jetpackPanel.SetActive(false);

        if (currentRiverIndex < riverDistances.Length)
        {
            Speak("Abel", $"Next river: {riverDistances[currentRiverIndex]} meters.");
            yield return StartCoroutine(Wait());
            OpenTerminal();
        }
    }

    /* ================= HELPERS ================= */

    void EquipJetpack()
    {
        jetpack.Equip();
        animatorController.SetJetpack(true);
    }

    void Speak(string who, string text)
    {
        dialoguePanel.SetActive(true);
        speakerText.text = who;
        dialogueText.text = text;
        speakerImage.sprite = who == "Abel" ? abelPortrait : kuttanPortrait;
    }

    IEnumerator Wait()
    {
        while (Input.GetKey(KeyCode.Return))
            yield return null;

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
        dialoguePanel.SetActive(false);
    }
}