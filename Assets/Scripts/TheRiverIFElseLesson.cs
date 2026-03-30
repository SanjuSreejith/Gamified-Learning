using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private TMPTypewriter typewriter;

    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    public GameObject jetpackPanel;
    public TextMeshProUGUI energyText;

    /* ================= PLAYER ================= */
    public JetpackController2D jetpack;
    public PlayerJetpackAnimator2D animatorController;

    /* ================= DATA ================= */
    public int[] riverDistances = { 10, 2, 6 };
    private int currentRiverIndex = 0;

    public int playerEnergy = 100;
    private const int ENERGY_RATE = 4;

    [Header("Fade System")]
    public StartFadeOut fadeController;

    private string WithCursor(string text, bool active)
    {
        return active ? text + "<color=#FFD54F>|</color>" : text;
    }

    /* ================= TERMINAL INPUT ================= */
    private string ifLine = "";
    private string ifBody = "";
    private string elifLine = "";
    private string elifBody = "";
    private string elseBody = "";

    private const string ELSE_LINE = "else:";
    private const string INDENT = "    ";
    private const string USER_COLOR = "#4FC3F7";

    private int currentLine = 0;
    private bool editing = false;
    private bool active = false;
    private bool conceptTaught = false;

    /* ================= FLOW CONTROL ================= */
    private bool logicLocked = false;
    private bool canFly = false;
    private bool isFlying = false;

    /* ================= PROTECTION SYSTEM ================= */
    private bool isCorrectLogic = false;
    private bool gameOverTriggered = false;

    /* ================= DEBUG/AUTOFILL ================= */
    [Header("Debug/AutoFill")]
    public bool autoFillCorrect = false;
    public bool autoFillFalse = false;

    [Header("NPCs")]
    public Transform[] npcTransforms;
    public Transform npcFinalPoint;

    /* ================= SCENE TRANSITION ================= */
    [Header("Scene Transition")]
    public string nextSceneName;
    public float sceneDelay = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sceneEndSound;

    /* ================= GAME OVER ================= */
    [Header("Game Over")]
    public GameObject gameOverPanel; // Fallback panel if GameOverManager doesn't exist
    public int maxAttempts = 3;
    private int failCount = 0;
    public float gameOverDelay = 2f;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void Start()
    {
        dialoguePanel.SetActive(false);
        terminalPanel.SetActive(false);
        jetpackPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (jetpack != null)
            jetpack.OnFlightEnd += OnFlightEnded;

        UpdateEnergyUI();

        if (dialogueText != null)
            typewriter = dialogueText.GetComponent<TMPTypewriter>();

        // Ensure GameOverManager is active in the scene
        EnsureGameOverManagerExists();
    }

    private void EnsureGameOverManagerExists()
    {
        // Try to find GameOverManager if it exists but might be inactive
        if (GameOverManager.Instance == null)
        {
            GameOverManager existingManager = FindObjectOfType<GameOverManager>(true);
            if (existingManager != null)
            {
                // If found but Instance is null, manually set it
                if (GameOverManager.Instance == null)
                {
                    // This is a workaround - in a proper setup, the manager should initialize itself
                    Debug.Log("GameOverManager found but Instance was null. It should initialize itself.");
                }
            }
            else
            {
                Debug.LogWarning("No GameOverManager found in scene. Game over functionality will use fallback.");
            }
        }
    }

    private void OnDestroy()
    {
        if (jetpack != null)
            jetpack.OnFlightEnd -= OnFlightEnded;
    }

    /* ================= FLIGHT HANDLING ================= */
    private void OnFlightEnded(bool success)
    {
        isFlying = false;

        if (!success)
        {
            // If correct logic is entered, protect from failure
            if (isCorrectLogic)
            {
                Debug.Log("PROTECTED: Correct logic detected - Preventing failure consequences");
                StartCoroutine(ProtectedFallDialogue());
                canFly = true;
                return;
            }

            HandleFailure();
            return;
        }

        currentRiverIndex++;

        if (currentRiverIndex == 1)
        {
            StartCoroutine(TeleportNPCsToFinalPoint());
            StartCoroutine(FirstPointDialogue());
            canFly = true;
            return;
        }

        if (currentRiverIndex >= riverDistances.Length)
        {
            StartCoroutine(FinalArrivalDialogue());
            canFly = false;
            return;
        }

        Speak("Abel", "Press F to cross the next river.");
        canFly = true;
    }

    private IEnumerator ProtectedFallDialogue()
    {
        yield return Wait();

        Speak("System", "⚠️ PROTECTION ACTIVE ⚠️");
        yield return Wait();

        Speak("Abel", "Your logic is correct, but something went wrong with the flight system.");
        yield return Wait();

        Speak("Kuttan", "Don't worry! The protection system will keep you safe.");
        yield return Wait();

        Speak("Abel", "Let's try that crossing again.");
        yield return Wait();

        ResetEnergyToCorrectValue();
        canFly = true;
    }

    private void ResetEnergyToCorrectValue()
    {
        int expectedEnergy = 100;

        for (int i = 0; i < currentRiverIndex; i++)
        {
            int riverLength = riverDistances[i];
            int requiredEnergy = riverLength * ENERGY_RATE;

            if (i == 0)
                expectedEnergy -= 40;
            else if (i == 1)
                expectedEnergy -= 8;
            else if (i == 2)
                expectedEnergy -= 24;
        }

        playerEnergy = Mathf.Max(expectedEnergy, 0);
        UpdateEnergyUI();
        Debug.Log($"Energy reset to correct value: {playerEnergy}");
    }

    private IEnumerator FinalArrivalDialogue()
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

        if (audioSource != null && sceneEndSound != null)
        {
            audioSource.PlayOneShot(sceneEndSound);
        }

        if (fadeController != null)
        {
            yield return StartCoroutine(fadeController.FadeIn());
        }
        else
        {
            yield return new WaitForSeconds(sceneDelay);
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator FallDialogue()
    {
        yield return Wait();

        Speak("Kuttan", "You didn't calculate enough energy for that river.");
        yield return Wait();

        Speak("Abel", "In programming, wrong conditions don't stop the program...");
        yield return Wait();

        Speak("Abel", "They just lead to wrong results.");
        yield return Wait();

        Speak("Abel", "Try again. Fix the logic.");
    }

    private IEnumerator TeleportNPCsToFinalPoint()
    {
        if (npcTransforms == null || npcFinalPoint == null)
            yield break;

        PauseNPCs(true);
        yield return null;

        foreach (var npc in npcTransforms)
        {
            if (npc != null)
                npc.position = npcFinalPoint.position;
        }

        Debug.Log("NPCs teleported");
        yield return null;
        PauseNPCs(false);
    }

    private void PauseNPCs(bool paused)
    {
        if (npcTransforms == null) return;

        foreach (var npc in npcTransforms)
        {
            if (npc == null) continue;

            var behaviours = npc.GetComponents<MonoBehaviour>();
            foreach (var b in behaviours)
            {
                if (b != this)
                    b.enabled = !paused;
            }
        }
    }

    private IEnumerator FirstPointDialogue()
    {
        yield return new WaitForSeconds(0.4f);

        Speak("Abel", "Good. You made it across the first river.");
        yield return Wait();

        Speak("Kuttan", "Your logic worked. Keep going.");
        yield return Wait();

        Speak("Abel", "Press F when you're ready for the next one.");
    }

    private void DisableJetpack()
    {
        if (animatorController != null)
            animatorController.SetJetpack(false);

        if (jetpackPanel != null)
            jetpackPanel.SetActive(false);
    }

    /* ================= TRIGGER ================= */
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (active || !other.CompareTag("Player")) return;
        active = true;
        StartCoroutine(IntroSequence());
    }

    private void AddIndent()
    {
        if (currentLine == 1 || currentLine == 3 || currentLine == 5)
        {
            string line = GetLineText(currentLine);
            if (!line.StartsWith(INDENT))
                AddTextToLine(currentLine, INDENT);
        }
    }

    /* ================= INTRO SEQUENCE ================= */
    private IEnumerator IntroSequence()
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
    private void OpenTerminal()
    {
        editing = true;
        currentLine = 0;
        ifLine = ifBody = elifLine = elifBody = elseBody = "";

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

    private void FinishTerminal()
    {
        editing = false;
        terminalPanel.SetActive(false);
        ValidateLogic();
    }

    private void Update()
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

    private void HandleTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                AddIndent();
                return;
            }

            if (c == '\n' || c == '\r')
            {
                if (currentLine == 3)
                    currentLine = 5;
                else
                    currentLine++;

                if (currentLine == 1 || currentLine == 3 || currentLine == 5)
                    AddTextToLine(currentLine, INDENT);

                if (currentLine > 5)
                {
                    editing = false;
                    terminalPanel.SetActive(false);
                    ValidateLogic();
                }
                return;
            }

            if (c == '\b')
            {
                HandleBackspace();
            }
            else if (c >= ' ' && c <= '~')
            {
                AddText(c.ToString());
            }
        }
    }

    private void HandleBackspace()
    {
        string lineText = GetLineText(currentLine);

        if (!string.IsNullOrEmpty(lineText))
        {
            RemoveCharFromLine(currentLine);
            return;
        }

        int prevLine = GetPreviousEditableLine(currentLine);
        if (prevLine != -1)
        {
            currentLine = prevLine;
            RemoveCharFromLine(currentLine);
        }
    }

    private void RemoveCharFromLine(int line)
    {
        string text = GetLineText(line);

        if (string.IsNullOrEmpty(text))
            return;

        if ((line == 1 || line == 3 || line == 5) && text.Length <= INDENT.Length)
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

    private string GetLineText(int line)
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

    private int GetPreviousEditableLine(int line)
    {
        if (line == 5) return 3;
        if (line == 3) return 1;
        return -1;
    }

    private void AddText(string t) => AddTextToLine(currentLine, t);

    private void AddTextToLine(int line, string t)
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

    private void UpdateTerminal()
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
    private void ValidateLogic()
    {
        string ifL = ifLine.Trim().ToLower();
        string elifL = elifLine.Trim().ToLower();

        string ifB = ifBody.Trim().ToLower();
        string elifB = elifBody.Trim().ToLower();
        string elseB = elseBody.Trim().ToLower();

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

        if (!IsValidEnergyReduction(elseB))
        {
            Speak("Abel", "Inside ELSE, you must reduce energy using '-='.");
            OpenTerminal();
            return;
        }

        // Check if logic is mathematically correct
        if (IsMathematicallyCorrect())
        {
            isCorrectLogic = true;
            logicLocked = true;
            EquipJetpack();
            canFly = true;

            Speak("Abel", "✅ PERFECT! Your logic is mathematically correct!");
            StartCoroutine(ShowProtectionActivation());
        }
        else
        {
            isCorrectLogic = false;
            Speak("Abel", "Your logic is syntactically correct but mathematically wrong.");
            StartCoroutine(ShowMathErrorAndReopen());
            return;
        }
    }

    private IEnumerator ShowProtectionActivation()
    {
        yield return new WaitForSeconds(1f);
        Speak("Kuttan", "The protection system is now active. No failure will stop you!");
        yield return new WaitForSeconds(1f);
        Speak("Abel", "Logic locked. Press F to fly across the first river.");
    }

    private IEnumerator ShowMathErrorAndReopen()
    {
        yield return Wait();
        Speak("Kuttan", "Check your conditions and energy values again.");
        yield return Wait();
        OpenTerminal();
    }

    private bool IsMathematicallyCorrect()
    {
        // Test all three river distances
        int[] testDistances = { 10, 2, 6 };
        int[] expectedEnergy = { 40, 8, 24 };

        for (int i = 0; i < testDistances.Length; i++)
        {
            int calculatedEnergy = EvaluateEnergyCost(testDistances[i]);
            Debug.Log($"Testing River {i + 1}: Distance={testDistances[i]}m, Expected={expectedEnergy[i]}, Calculated={calculatedEnergy}");

            if (calculatedEnergy != expectedEnergy[i])
            {
                Debug.LogError($"Math check FAILED! River {testDistances[i]}m should cost {expectedEnergy[i]} but calculated {calculatedEnergy}");
                return false;
            }
        }

        Debug.Log("✅ Math check PASSED! All river calculations are correct.");
        return true;
    }

    private bool IsValidConditionalLine(string line, string keyword)
    {
        if (!line.StartsWith(keyword)) return false;
        if (!line.EndsWith(":")) return false;

        string condition = line
            .Substring(keyword.Length)
            .Trim()
            .TrimEnd(':')
            .Trim();

        return condition.Length > 0;
    }

    private bool IsValidEnergyReduction(string body)
    {
        return body.Contains("energy") && body.Contains("-=");
    }

    private string ColorUserText(string text)
    {
        return $"<color={USER_COLOR}>{text}</color>";
    }

    /* ================= ENERGY EVALUATION ================= */
    private int EvaluateEnergyCost(int riverLength)
    {
        // Check IF condition
        if (EvaluateCondition(ifLine, riverLength))
            return ExtractNumber(ifBody);

        // Check ELIF condition
        if (EvaluateCondition(elifLine, riverLength))
            return ExtractNumber(elifBody);

        // ELSE fallback
        return ExtractNumber(elseBody);
    }

    private bool EvaluateCondition(string conditionLine, int riverLength)
    {
        if (string.IsNullOrEmpty(conditionLine))
            return false;

        // Remove 'if' or 'elif' keyword and colon
        string condition = conditionLine
            .Replace("if", "")
            .Replace("elif", "")
            .Trim()
            .TrimEnd(':')
            .Trim();

        Debug.Log($"Parsing condition: '{condition}' for river length: {riverLength}");

        // Find operator
        string[] operators = { ">=", "<=", "==", ">", "<" };
        string foundOperator = "";
        int operatorIndex = -1;

        foreach (string op in operators)
        {
            int idx = condition.IndexOf(op);
            if (idx != -1)
            {
                foundOperator = op;
                operatorIndex = idx;
                break;
            }
        }

        if (operatorIndex == -1)
        {
            Debug.LogError($"No valid operator found in condition: {condition}");
            return false;
        }

        // Get the right side (the number)
        string rightSide = condition.Substring(operatorIndex + foundOperator.Length).Trim();

        // Parse the number
        if (!int.TryParse(rightSide, out int threshold))
        {
            string digits = "";
            foreach (char c in rightSide)
            {
                if (char.IsDigit(c))
                    digits += c;
            }
            if (!int.TryParse(digits, out threshold))
            {
                Debug.LogError($"Failed to parse number from: {rightSide}");
                return false;
            }
        }

        // Evaluate based on operator
        bool result = foundOperator switch
        {
            ">=" => riverLength >= threshold,
            "<=" => riverLength <= threshold,
            "==" => riverLength == threshold,
            ">" => riverLength > threshold,
            "<" => riverLength < threshold,
            _ => false
        };

        Debug.Log($"Condition: {riverLength} {foundOperator} {threshold} = {result}");
        return result;
    }

    private int ExtractNumber(string line)
    {
        if (string.IsNullOrEmpty(line))
            return 0;

        // Look for pattern like "energy -= 40" or "energy-=40"
        string cleanedLine = line.Replace(" ", "").ToLower();

        int index = cleanedLine.IndexOf("-=");
        if (index != -1 && index + 2 < cleanedLine.Length)
        {
            string numberPart = cleanedLine.Substring(index + 2);

            string digits = "";
            foreach (char c in numberPart)
            {
                if (char.IsDigit(c))
                    digits += c;
                else
                    break;
            }

            if (!string.IsNullOrEmpty(digits))
            {
                int result = int.Parse(digits);
                Debug.Log($"Extracted number: {result} from line: {line}");
                return result;
            }
        }

        // Fallback: extract all digits
        string allDigits = "";
        foreach (char c in line)
        {
            if (char.IsDigit(c))
                allDigits += c;
        }

        if (string.IsNullOrEmpty(allDigits))
        {
            Debug.LogError($"No number found in line: {line}");
            return 0;
        }

        return int.Parse(allDigits);
    }

    /* ================= FLIGHT ATTEMPT ================= */
    private void TryFly()
    {
        if (!canFly || isFlying || currentRiverIndex >= riverDistances.Length)
            return;

        int riverLength = riverDistances[currentRiverIndex];
        int requiredEnergy = riverLength * ENERGY_RATE;

        Debug.Log($"\n=== Attempting to cross River {currentRiverIndex + 1} ===");
        Debug.Log($"River Length: {riverLength}m, Required Energy: {requiredEnergy}");
        Debug.Log($"Current Player Energy: {playerEnergy}");

        int usedEnergy = EvaluateEnergyCost(riverLength);
        usedEnergy = Mathf.Clamp(usedEnergy, 0, requiredEnergy);

        Debug.Log($"Used Energy: {usedEnergy}");

        if (usedEnergy > playerEnergy)
        {
            Debug.Log($"FAIL: Not enough energy! Need {usedEnergy}, have {playerEnergy}");

            if (isCorrectLogic)
            {
                Debug.Log("PROTECTED: Correct logic detected - Preventing energy failure");
                StartCoroutine(ProtectedEnergyFailure());
                return;
            }

            if (jetpack != null)
                jetpack.FailFall();
            HandleFailure();
            return;
        }

        playerEnergy -= usedEnergy;
        UpdateEnergyUI();

        Debug.Log($"SUCCESS: Energy used: {usedEnergy}, Remaining: {playerEnergy}");

        isFlying = true;
        canFly = false;

        float travelPercent = usedEnergy > 0 ? (float)usedEnergy / requiredEnergy : 0f;
        travelPercent = Mathf.Clamp(travelPercent, 0f, 1.2f);

        if (jetpack != null)
            jetpack.FlyToNextPoint(travelPercent);
    }

    private IEnumerator ProtectedEnergyFailure()
    {
        Speak("System", "⚠️ PROTECTION ACTIVE ⚠️");
        yield return Wait();

        Speak("Abel", "Your logic is correct, but you don't have enough energy stored.");
        yield return Wait();

        Speak("Kuttan", "Let me restore your energy to the correct amount!");
        yield return Wait();

        ResetEnergyToCorrectValue();

        Speak("Abel", "Energy restored! Try flying again.");
        canFly = true;
    }

    /* ================= GAME OVER HANDLING ================= */
    private void HandleFailure()
    {
        if (isCorrectLogic)
        {
            Debug.Log("PROTECTED: Correct logic prevents game over!");
            StartCoroutine(ProtectedRecovery());
            return;
        }

        failCount++;

        if (failCount >= maxAttempts)
        {
            GameOver();
        }
        else
        {
            Speak("Kuttan", $"You have {maxAttempts - failCount} attempts remaining.");
            StartCoroutine(ResetForRetry());
        }
    }

    private IEnumerator ProtectedRecovery()
    {
        Speak("System", "🛡️ PROTECTION SYSTEM ENGAGED 🛡️");
        yield return Wait();

        Speak("Abel", "Your logic is mathematically correct!");
        yield return Wait();

        Speak("Kuttan", "The system will automatically recover you from any failure.");
        yield return Wait();

        Speak("Abel", "Let's continue from where we left off.");
        yield return Wait();

        ResetEnergyToCorrectValue();
        canFly = true;
    }

    private IEnumerator ResetForRetry()
    {
        yield return new WaitForSeconds(1f);

        playerEnergy = 100;
        UpdateEnergyUI();
        canFly = true;

        Speak("Abel", "Energy restored. Try again with the correct logic.");
    }

    private void GameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;

        canFly = false;
        logicLocked = false;

        // Try to find GameOverManager if it exists but might be inactive
        GameOverManager manager = GameOverManager.Instance;

        if (manager == null)
        {
            // Try to find it in the scene even if inactive
            manager = FindObjectOfType<GameOverManager>(true);
        }

        // Use the centralized GameOverManager
        if (manager != null)
        {
            manager.ShowGameOver();
        }
        else
        {
            // Fallback if GameOverManager doesn't exist in the scene
            Debug.LogWarning("GameOverManager not found in scene. Using fallback game over panel.");
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Speak("Abel", "You've failed too many times.");
            StartCoroutine(FallbackRestart());
        }
    }

    // Fallback restart coroutine in case GameOverManager doesn't exist
    private IEnumerator FallbackRestart()
    {
        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /* ================= HELPER METHODS ================= */
    private void UpdateEnergyUI()
    {
        if (energyText != null)
            energyText.text = $"Energy: {playerEnergy}";
    }

    private void EquipJetpack()
    {
        if (jetpack != null)
            jetpack.Equip();

        if (animatorController != null)
            animatorController.SetJetpack(true);

        if (jetpackPanel != null)
            jetpackPanel.SetActive(true);
    }

    private void Speak(string who, string text)
    {
        if (dialoguePanel == null) return;

        dialoguePanel.SetActive(true);
        speakerText.text = who;

        if (who == "Abel")
            speakerImage.sprite = abelPortrait;
        else if (who == "Kuttan")
            speakerImage.sprite = kuttanPortrait;

        if (typewriter != null)
            typewriter.Play(text);
        else if (dialogueText != null)
            dialogueText.text = text;


        DialogueBacklogManager.Instance?.AddLine(who, text);
    }

    private IEnumerator Wait()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (typewriter != null && typewriter.IsTyping())
                {
                    typewriter.Skip();
                }
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

    private void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        PlayerPrefs.Save();
        Debug.Log("Scene marked as completed: " + sceneName);
    }
}