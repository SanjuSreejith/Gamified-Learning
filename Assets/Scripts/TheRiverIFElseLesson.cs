using System.Collections;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.VolumeComponent;

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
    TMPTypewriter typewriter;

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
    [Header("Fade System")]
    public StartFadeOut fadeController;
    string WithCursor(string text, bool active)
    {
        return active ? text + "<color=#FFD54F>|</color>" : text;
    }

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

    /* ================= FLOW CONTROL ================= */
    bool logicLocked = false;
    bool canFly = false;
    bool isFlying = false;

    /* ================= DEBUG/AUTOFILL ================= */
    [Header("Debug/AutoFill")]
    public bool autoFillCorrect = false;
    public bool autoFillFalse = false;

    [Header("NPCs")]
    public Transform[] npcTransforms;
    public Transform npcFinalPoint;
    const string INDENT = "    ";
    const string USER_COLOR = "#4FC3F7";   // blue for player input

    /* ================= SCENE TRANSITION ================= */
    [Header("Scene Transition")]
    public string nextSceneName;
    public float sceneDelay = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sceneEndSound;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Start()
    {
        dialoguePanel.SetActive(false);
        terminalPanel.SetActive(false);
        jetpackPanel.SetActive(false);

        jetpack.OnFlightEnd += OnFlightEnded;
        UpdateEnergyUI();
        typewriter = dialogueText.GetComponent<TMPTypewriter>();
    }

    /* ================= FLIGHT HANDLING ================= */
    void OnFlightEnded(bool success)
    {
        isFlying = false;

        if (!success)
        {
            // ❌ Wrong logic → player falls, but lesson continues
            Speak("Abel", "Hmm… looks like your logic didn’t give enough energy.");
            StartCoroutine(FallDialogue());

            // Allow retry / progression
            canFly = true;
            return;
        }


        currentRiverIndex++;

        // 🔹 FIRST CHECKPOINT: teleport NPCs ONLY
        if (currentRiverIndex == 1)
        {
            StartCoroutine(TeleportNPCsToFinalPoint());
            StartCoroutine(FirstPointDialogue());
            canFly = true;
            return;
        }

        // 🔹 FINAL CHECKPOINT: dialogue + jetpack removal
        if (currentRiverIndex >= riverDistances.Length)
        {
            StartCoroutine(FinalArrivalDialogue());
            canFly = false;
            return;
        }

        // 🔹 MIDDLE CHECKPOINTS
        Speak("Abel", "Press F to cross the next river.");
        canFly = true;
    }
    IEnumerator FinalArrivalDialogue()
    {
        yield return new WaitForSeconds(0.4f);

        Speak("Abel", "You're a little late… we were wondering.");
        yield return Wait();

        Speak("Kuttan", "We already crossed ahead.");
        yield return Wait();

        Speak("Abel", "You don't need the jetpack anymore. Let's go.");
        yield return Wait();

        MarkSceneCompleted();
        DisableJetpack();
        // 🔊 Play sound
        if (audioSource != null && sceneEndSound != null)
        {
            audioSource.PlayOneShot(sceneEndSound);
        }

        // 🌑 Fade to black
        if (fadeController != null)
        {
            yield return StartCoroutine(fadeController.FadeIn());
        }
        else
        {
            yield return new WaitForSeconds(sceneDelay);
        }

        // 🎬 Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
    IEnumerator FallDialogue()
    {
        yield return Wait();

        Speak("Kuttan", "You didn’t calculate enough energy for that river.");
        yield return Wait();

        Speak("Abel", "In programming, wrong conditions don’t stop the program...");
        yield return Wait();

        Speak("Abel", "They just lead to wrong results.");
        yield return Wait();

        Speak("Abel", "Try again. Fix the logic.");
    }

    IEnumerator TeleportNPCsToFinalPoint()
    {
        if (npcTransforms == null || npcFinalPoint == null)
            yield break;

        // 🧊 Pause NPC logic so nothing fights the teleport
        PauseNPCs(true);

        // Small delay so pause fully applies (important)
        yield return null;

        foreach (var npc in npcTransforms)
        {
            npc.position = npcFinalPoint.position;
        }

        Debug.Log("NPCs teleported");

        // 🟢 Unpause AFTER teleport
        yield return null;
        PauseNPCs(false);
    }

    void PauseNPCs(bool paused)
    {
        if (npcTransforms == null) return;

        foreach (var npc in npcTransforms)
        {
            var behaviours = npc.GetComponents<MonoBehaviour>();
            foreach (var b in behaviours)
            {
                // Don't disable this controller itself if attached
                if (b != this)
                    b.enabled = !paused;
            }
        }
    }


    IEnumerator FirstPointDialogue()
    {
        yield return new WaitForSeconds(0.4f);

        Speak("Abel", "Good. You made it across the first river.");
        yield return Wait();

        Speak("Kuttan", "Your logic worked. Keep going.");
        yield return Wait();

        Speak("Abel", "Press F when you're ready for the next one.");
    }

    IEnumerator EndDialogue()
    {
        Speak("Abel", "Good thinking. You learned how logic saves energy.");
        yield return Wait();

        Speak("Kuttan", "That's how programming works too.");
        yield return Wait();
    }

    void DisableJetpack()
    {
        if (animatorController != null)
            animatorController.SetJetpack(false);

        if (jetpackPanel != null)
            jetpackPanel.SetActive(false);
    }

    /* ================= TRIGGER ================= */
    void OnTriggerEnter2D(Collider2D other)
    {
        if (active || !other.CompareTag("Player")) return;
        active = true;
        StartCoroutine(IntroSequence());
    }
    void AddIndent()
    {
        // Only allow indent on body lines
        if (currentLine == 1 || currentLine == 3 || currentLine == 5)
        {
            string line = GetLineText(currentLine);

            // Prevent double-indent
            if (!line.StartsWith(INDENT))
                AddTextToLine(currentLine, INDENT);
        }
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

        Speak("Kuttan", $"Three rivers ahead: {riverDistances[0]}, {riverDistances[1]}, {riverDistances[2]} meters.");
        yield return Wait();

        Speak("Abel", $"Energy cost is {ENERGY_RATE} per meter. Calculate carefully.");
        yield return Wait();

        if (!conceptTaught)
        {
            Speak("Abel", "Write your if / elif / else logic once.");
            yield return Wait();

            terminalPanel.SetActive(true);
            terminalText.text =
                "<color=#9CDCFE>river_length = ?</color>\n" +
                "<color=#9CDCFE>energy= 100</color> \n\n" +
                "if river_length > 8:\n" +
                "    energy -= 40\n" +
                "elif river_length > 4:\n" +
                "    energy -= 20\n" +
                "else:\n" +
                "    energy -= 8";

            yield return Wait();
            terminalPanel.SetActive(false);
            conceptTaught = true;
        }

        OpenTerminal();
    }

    /* ================= TERMINAL LOGIC ================= */
    void OpenTerminal()
    {
        editing = true;
        currentLine = 0;
        ifLine = ifBody = elifLine = elifBody = elseBody = "";

        // Auto-fill correct values if enabled
        if (autoFillCorrect)
        {
            ifLine = "if river_length >= 10:";
            ifBody = "    energy -= 40";
            elifLine = "elif river_length >= 6:";
            elifBody = "    energy -= 24";
            elseBody = "    energy -= 8";
            FinishTerminal();
            return;
        }
        else if (autoFillFalse)
        {
            ifLine = "if river_length >= 10:";
            ifBody = "    energy -= 40";
            elifLine = "elif river_length >= 6:";
            elifBody = "    energy -= 15";
            elseBody = "    energy -= 8";
            FinishTerminal();
            return;
        }

        terminalPanel.SetActive(true);
        UpdateTerminal();
    }

    void FinishTerminal()
    {
        editing = false;
        terminalPanel.SetActive(false);
        ValidateLogic();
    }

    void Update()
    {
        if (editing)
        {
            HandleTyping();
            UpdateTerminal();
            return;
        }

        if (logicLocked && canFly && !isFlying && Input.GetKeyDown(KeyCode.F))
        {
            TryFly();
        }
    }

    void HandleTyping()
    {
        foreach (char c in Input.inputString)
        {
           
                // TAB (Unity-safe)
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    AddIndent();
                    return;
                }


            // ENTER
            if (c == '\n' || c == '\r')
            {
                if (currentLine == 3) currentLine = 5;
                else currentLine++;

                if (currentLine == 1 || currentLine == 3 || currentLine == 5)
                    AddTextToLine(currentLine, "    ");

                if (currentLine > 5)
                {
                    editing = false;
                    terminalPanel.SetActive(false);
                    ValidateLogic();
                }
                return;
            }

            // BACKSPACE ⭐ FIXED
            if (c == '\b')
            {
                HandleBackspace();
            }
            else
            {
                AddText(c.ToString());
            }
        }
    }
    void HandleBackspace()
    {
        string lineText = GetLineText(currentLine);

        // Normal delete
        if (!string.IsNullOrEmpty(lineText))
        {
            RemoveCharFromLine(currentLine);
            return;
        }

        // Jump to previous editable line
        int prevLine = GetPreviousEditableLine(currentLine);
        if (prevLine != -1)
        {
            currentLine = prevLine;
            RemoveCharFromLine(currentLine);
        }
    }

    void RemoveCharFromLine(int line)
    {
        const string INDENT = "    ";
        string text = GetLineText(line);

        if (string.IsNullOrEmpty(text))
            return;

        // 🔒 Protect indentation (first 4 spaces)
        if ((line == 1 || line == 3 || line == 5) &&
            text.Length <= INDENT.Length)
            return;

        switch (line)
        {
            case 0: ifLine = text.Substring(0, text.Length - 1); break;
            case 1: ifBody = text.Substring(0, text.Length - 1); break;
            case 2: elifLine = text.Substring(0, text.Length - 1); break;
            case 3: elifBody = text.Substring(0, text.Length - 1); break;
            case 5: elseBody = text.Substring(0, text.Length - 1); break;
        }
    }


    string GetLineText(int line)
    {
        switch (line)
        {
            case 0: return ifLine;
            case 1: return ifBody;
            case 2: return elifLine;
            case 3: return elifBody;
            case 5: return elseBody;
        }
        return "";
    }


    int GetPreviousEditableLine(int line)
    {
        if (line == 5) return 3;
        if (line == 3) return 1;
        return -1;
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
            case 0:
                if (ifLine.Length > 0)
                    ifLine = ifLine.Substring(0, ifLine.Length - 1);
                break;

            case 1:
                if (ifBody.Length > 0)
                    ifBody = ifBody.Substring(0, ifBody.Length - 1);
                break;

            case 2:
                if (elifLine.Length > 0)
                    elifLine = elifLine.Substring(0, elifLine.Length - 1);
                break;

            case 3:
                if (elifBody.Length > 0)
                    elifBody = elifBody.Substring(0, elifBody.Length - 1);
                break;

            case 5:
                if (elseBody.Length > 0)
                    elseBody = elseBody.Substring(0, elseBody.Length - 1);
                break;
        }
    }

    void UpdateTerminal()
    {
        terminalText.text =
            "<color=#9CDCFE>river_length = ?</color>\n" +
            "<color=#9CDCFE>energy = 100</color>\n\n" +

            WithCursor(
                string.IsNullOrEmpty(ifLine)
                    ? "if ____________:"
                    : ColorUserText(ifLine),
                currentLine == 0
            ) + "\n" +

            WithCursor(
                string.IsNullOrEmpty(ifBody)
                    ? INDENT + "energy -= ______"
                    : ColorUserText(ifBody),
                currentLine == 1
            ) + "\n" +

            WithCursor(
                string.IsNullOrEmpty(elifLine)
                    ? "elif ____________:"
                    : ColorUserText(elifLine),
                currentLine == 2
            ) + "\n" +

            WithCursor(
                string.IsNullOrEmpty(elifBody)
                    ? INDENT + "energy -= ______"
                    : ColorUserText(elifBody),
                currentLine == 3
            ) + "\n" +

            ELSE_LINE + "\n" +

            WithCursor(
                string.IsNullOrEmpty(elseBody)
                    ? INDENT + "energy -= ______"
                    : ColorUserText(elseBody),
                currentLine == 5
            );
    }

    /* ================= VALIDATION ================= */

    void ValidateLogic()
    {
        string ifL = ifLine.Trim().ToLower();
        string elifL = elifLine.Trim().ToLower();

        string ifB = ifBody.Trim().ToLower();
        string elifB = elifBody.Trim().ToLower();
        string elseB = elseBody.Trim().ToLower();

        // ---------- IF ----------
        if (!IsValidConditionalLine(ifL, "if"))
        {
            Speak("Abel", "The IF line is wrong. Use: if <condition>:");
            OpenTerminal();
            return;
        }

        if (!IsValidEnergyReduction(ifB))
        {
            Speak("Kuttan", "Inside IF, you must reduce energy using '-='.");
            OpenTerminal();
            return;
        }

        // ---------- ELIF ----------
        if (!IsValidConditionalLine(elifL, "elif"))
        {
            Speak("Abel", "The ELIF line is wrong. Use: elif <condition>:");
            OpenTerminal();
            return;
        }

        if (!IsValidEnergyReduction(elifB))
        {
            Speak("Kuttan", "Inside ELIF, you must reduce energy using '-='.");
            OpenTerminal();
            return;
        }

        // ---------- ELSE ----------
        if (!IsValidEnergyReduction(elseB))
        {
            Speak("Abel", "Inside ELSE, you must reduce energy using '-='.");
            OpenTerminal();
            return;
        }

        // ---------- SUCCESS ----------
        logicLocked = true;
        EquipJetpack();
        canFly = true;

        Speak("Abel", "Logic locked. Press F to fly across the first river.");
    }


    bool IsValidConditionalLine(string line, string keyword)
    {
        // Must start with keyword (if / elif)
        if (!line.StartsWith(keyword)) return false;

        // Must end with colon
        if (!line.EndsWith(":")) return false;

        // Remove keyword and colon safely
        string condition = line
            .Substring(keyword.Length)
            .Trim()
            .TrimEnd(':')
            .Trim();

        // Condition must exist
        return condition.Length > 0;
    }

    bool IsValidEnergyReduction(string body)
    {
        // Accepts: energy-=40, energy -= 40, energy  -=40, etc.
        return body.Contains("energy") && body.Contains("-=");
    }
    string ColorUserText(string text)
    {
        return $"<color={USER_COLOR}>{text}</color>";
    }


    /* ================= ENERGY EVALUATION ================= */
    int EvaluateEnergyCost(int riverLength)
    {
        // ---------- IF ----------
        if (ifLine.Contains("==") && riverLength == ExtractNumber(ifLine))
            return ExtractNumber(ifBody);

        if (ifLine.Contains(">=") && riverLength >= ExtractNumber(ifLine))
            return ExtractNumber(ifBody);

        if (ifLine.Contains(">") && riverLength > ExtractNumber(ifLine))
            return ExtractNumber(ifBody);

        if (ifLine.Contains("<=") && riverLength <= ExtractNumber(ifLine))
            return ExtractNumber(ifBody);

        if (ifLine.Contains("<") && riverLength < ExtractNumber(ifLine))
            return ExtractNumber(ifBody);

        // ---------- ELIF ----------
        if (elifLine.Contains("==") && riverLength == ExtractNumber(elifLine))
            return ExtractNumber(elifBody);

        if (elifLine.Contains(">=") && riverLength >= ExtractNumber(elifLine))
            return ExtractNumber(elifBody);

        if (elifLine.Contains(">") && riverLength > ExtractNumber(elifLine))
            return ExtractNumber(elifBody);

        if (elifLine.Contains("<=") && riverLength <= ExtractNumber(elifLine))
            return ExtractNumber(elifBody);

        if (elifLine.Contains("<") && riverLength < ExtractNumber(elifLine))
            return ExtractNumber(elifBody);

        // ---------- ELSE ----------
        return ExtractNumber(elseBody);
    }

    int ExtractNumber(string line)
    {
        string digits = "";
        foreach (char c in line)
            if (char.IsDigit(c)) digits += c;

        return int.Parse(digits);
    }

    /* ================= FLIGHT ATTEMPT ================= */
    void TryFly()
    {
        if (!canFly || isFlying || currentRiverIndex >= riverDistances.Length)
            return;

        int riverLength = riverDistances[currentRiverIndex];
        int requiredEnergy = riverLength * ENERGY_RATE;

        int usedEnergy = EvaluateEnergyCost(riverLength);

        float travelPercent = Mathf.Clamp(
            (float)usedEnergy / requiredEnergy,
            0f,
            1.2f
        );

        if (usedEnergy > playerEnergy)
        {
            jetpack.FailFall();
            Speak("Kuttan", "You don't have enough energy!");
            return;
        }

        playerEnergy -= usedEnergy;
        UpdateEnergyUI();

        isFlying = true;
        canFly = false;

        // 🚀 ONLY start flight — DO NOT TOUCH EVENTS
        jetpack.FlyToNextPoint(travelPercent);
    }


    /* ================= HELPER METHODS ================= */
    void UpdateEnergyUI()
    {
        if (energyText != null)
            energyText.text = $"Energy: {playerEnergy}";
    }

    void EquipJetpack()
    {
        if (jetpack != null)
            jetpack.Equip();

        if (animatorController != null)
            animatorController.SetJetpack(true);

        if (jetpackPanel != null)
            jetpackPanel.SetActive(true);
    }

    void Speak(string who, string text)
    {
        if (dialoguePanel == null) return;

        dialoguePanel.SetActive(true);
        speakerText.text = who;
        speakerImage.sprite = who == "Abel" ? abelPortrait : kuttanPortrait;

        if (typewriter != null)
            typewriter.Play(text);
        else
            dialogueText.text = text;

        DialogueBacklogManager.Instance?.AddLine(who, text);
    }

    IEnumerator Wait()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                // First Enter → finish typing
                if (typewriter != null && typewriter.IsTyping())
                {
                    typewriter.Skip();
                }
                // Second Enter → continue
                else
                {
                    break;
                }
            }
            yield return null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        // or use the other pattern: "SceneCompleted_" + sceneName
        PlayerPrefs.Save();
        Debug.Log("Scene marked as completed: " + sceneName);
    }
}