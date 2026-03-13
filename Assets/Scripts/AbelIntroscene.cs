using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AbelIntroNPC : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Typing")]
    public float typeSpeed = 0.035f;

    [Header("Jetpack UI")]
    public CanvasGroup jetpackUI;
    public float jetpackFadeSpeed = 2f;

    [Header("Terminal Puzzle UI")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    [Header("Energy Controller")]
    public GameObject energyPanel;
    public Slider energySlider;
    public TextMeshProUGUI energyText;
    public float maxEnergy = 100f;

    // Color stages
    public Color lowEnergyColor = new Color(1f, 0.25f, 0.25f);
    public Color midEnergyColor = new Color(1f, 0.8f, 0.25f);
    public Color highEnergyColor = new Color(0.25f, 1f, 0.4f);

    // Animation
    public float pulseSpeed = 4f;
    public float glitchChance = 0.08f;

    [Header("Optional Morning Toggle")]
    public bool isMorning;

    [Header("Scene Transition")]
    public CanvasGroup fadeCanvas;
    public float fadeSpeed = 1.5f;
    public string nextSceneName = "OutsideWorld";

    [Header("Player Performance")]
    public OverallPerformance playerPerformance;

    [Header("Hint System")]
    public BotHintSystem hintSystem;

    bool waitingForContinue;
    bool waitingForTerminalInput;
    string typedInput = "";

    bool inputFixed;
    bool outputFixed;

    enum State
    {
        Dialogue,
        TerminalFix,
        EnergySet
    }

    State currentState;

    public enum OverallPerformance
    {
        Master,
        Competent,
        Novice,
        Struggling
    }

    void Start()
    {
        // Ensure UI elements start disabled
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (terminalPanel) terminalPanel.SetActive(false);
        if (energyPanel) energyPanel.SetActive(false);
        if (energyText) energyText.text = "ENERGY : 0%";
        if (jetpackUI)
        {
            jetpackUI.alpha = 0;
            jetpackUI.gameObject.SetActive(false);
        }
        if (fadeCanvas)
        {
            fadeCanvas.alpha = 0;
            fadeCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (currentState == State.TerminalFix)
            HandleTerminalTyping();
    }

    public void StartDialogue()
    {
        Debug.Log("AbelIntroNPC StartDialogue called");

        if (!dialoguePanel)
        {
            Debug.LogError("[AbelIntroNPC] Dialogue panel not assigned!");
            return;
        }

        dialoguePanel.SetActive(true);
        speakerText.text = "???";
        StartCoroutine(DialogueSequence());
    }

    void UpdateEnergyTextAnimated()
    {
        if (!energySlider || !energyText) return;

        float value01 = energySlider.maxValue > 1
            ? energySlider.value / maxEnergy
            : energySlider.value;

        value01 = Mathf.Clamp01(value01);
        int energyPercent = Mathf.RoundToInt(value01 * 100f);

        // Color transition
        Color targetColor;
        if (value01 < 0.5f)
            targetColor = Color.Lerp(lowEnergyColor, midEnergyColor, value01 / 0.5f);
        else
            targetColor = Color.Lerp(midEnergyColor, highEnergyColor, (value01 - 0.5f) / 0.5f);

        energyText.color = targetColor;

        // Glitch effect
        if (Random.value < glitchChance)
        {
            energyText.text = "ENERGY : ##%";
        }
        else
        {
            energyText.text = "ENERGY : " + energyPercent + "%";
        }

        // Pulse near full
        if (value01 > 0.85f)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.08f;
            energyText.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            energyText.transform.localScale = Vector3.one;
        }
    }

    IEnumerator DialogueSequence()
    {
        yield return Speak("…So, you made it.");

        speakerText.text = "Abel";

        yield return Speak("My name is Abel.");

        foreach (string line in GetPerformanceDialogue())
            yield return Speak(line);

        yield return Speak("NULL is attacking this world.");

        yield return Speak("Let’s set the energy meter.");

        yield return Speak("Oh—sorry.");
        yield return Speak("The system is corrupted.");

        yield return Speak(
            "We need to repair it.\n" +
            "Fix the input first.\n" +
            "Then the output."
        );

        StartTerminalPuzzle();
    }

    // ---------------- TERMINAL PUZZLE ----------------

    void StartTerminalPuzzle()
    {
        currentState = State.TerminalFix;
        terminalPanel.SetActive(true);
        terminalText.text =
            "SYSTEM ERROR\n" +
            "INPUT MODULE : BROKEN\n" +
            "OUTPUT MODULE : BROKEN\n\n> ";

        if (hintSystem != null)
        {
            string[] hints = new string[]
            {
                "Fix input module: type 'input'",
                "Fix output module: type 'print'"
            };
            hintSystem.SetHints(hints);
            hintSystem.EnableHints();
        }
    }

    void HandleTerminalTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && typedInput.Length > 0)
                typedInput = typedInput.Substring(0, typedInput.Length - 1);
            else if (c == '\n' || c == '\r')
                SubmitTerminalInput();
            else if (!char.IsControl(c))
                typedInput += c;
        }

        terminalText.text =
            "SYSTEM ERROR\n" +
            "INPUT MODULE : " + (inputFixed ? "OK" : "BROKEN") + "\n" +
            "OUTPUT MODULE : " + (outputFixed ? "OK" : "BROKEN") + "\n\n> " +
            typedInput;
    }

    void SubmitTerminalInput()
    {
        string cmd = typedInput.Trim().ToLower();
        typedInput = "";

        bool changed = false;

        if (!inputFixed && cmd.Contains("input"))
        {
            inputFixed = true;
            changed = true;
        }
        else if (inputFixed && !outputFixed && cmd.Contains("print"))
        {
            outputFixed = true;
            changed = true;
        }

        if (changed && hintSystem != null)
        {
            string[] updatedHints = new string[2];
            updatedHints[0] = inputFixed ? "✓ Fix input module (done)" : "Fix input module: type 'input'";
            updatedHints[1] = outputFixed ? "✓ Fix output module (done)" : "Fix output module: type 'print'";
            hintSystem.SetHints(updatedHints);
        }

        if (inputFixed && outputFixed)
        {
            if (hintSystem != null)
                hintSystem.DisableHints();

            terminalPanel.SetActive(false);
            StartCoroutine(EnergySequence());
        }
    }

    // ---------------- ENERGY SET (FIXED) ----------------
    IEnumerator EnergySequence()
    {
        currentState = State.EnergySet;

        yield return Speak("Good.");
        yield return Speak("Now set the energy.");

        energyPanel.SetActive(true);

        // Reset slider
        energySlider.value = 0;
        energyText.transform.localScale = Vector3.one;
        UpdateEnergyTextAnimated();

        // 🟢 AUTO-FILL the slider over 2 seconds
        float fillDuration = 2f;
        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            // Increase slider value gradually
            energySlider.value = Mathf.Lerp(0, energySlider.maxValue, elapsed / fillDuration);
            UpdateEnergyTextAnimated();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure it's exactly full
        energySlider.value = energySlider.maxValue;
        UpdateEnergyTextAnimated();

        // Final lock
        energyText.transform.localScale = Vector3.one;
        energyText.color = highEnergyColor;
        energyText.text = "ENERGY SET";

        yield return new WaitForSeconds(1.2f);

        energyPanel.SetActive(false);

        yield return Speak("Energy stable.");

        yield return ShowJetpackUI();

        if (isMorning)
        {
            yield return Speak(
                "It’s night.\n" +
                "We can wait till tomorrow."
            );
        }
        else
        {
            yield return Speak("Come.");
            yield return Speak("Let’s go out.");
        }

        yield return FadeAndLoad();
    }

    // ---------------- UTILITIES ----------------

    string[] GetPerformanceDialogue()
    {
        switch (playerPerformance)
        {
            case OverallPerformance.Master:
                return new[] { "You understood the house." };
            case OverallPerformance.Competent:
                return new[] { "You pushed through uncertainty." };
            case OverallPerformance.Novice:
                return new[] { "You hesitated—but continued." };
            case OverallPerformance.Struggling:
                return new[] { "You struggled—but survived." };
        }
        return new string[0];
    }

    IEnumerator ShowJetpackUI()
    {
        if (!jetpackUI) yield break;

        jetpackUI.gameObject.SetActive(true);
        float t = 0;
        while (t < 1)
        {
            jetpackUI.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * jetpackFadeSpeed;
            yield return null;
        }
        jetpackUI.alpha = 1;
    }

    IEnumerator Speak(string line)
    {
        // Limit dialogue to 2 lines max
        string[] splitLines = line.Split('\n');

        if (splitLines.Length > 2)
        {
            line = splitLines[0] + "\n" + splitLines[1];
        }

        dialogueText.text = "";

        // Typing effect
        foreach (char c in line)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        waitingForContinue = true;

        while (waitingForContinue)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                waitingForContinue = false;

            yield return null;
        }
    }

    IEnumerator FadeAndLoad()
    {
        if (!fadeCanvas)
        {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        fadeCanvas.gameObject.SetActive(true);
        float t = 0;
        while (t < 1)
        {
            fadeCanvas.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        SceneManager.LoadScene(nextSceneName);
    }
}