using UnityEngine;
using TMPro;
using System.Collections;

public class ForLoopTerminal : MonoBehaviour
{
    [Header("Terminal UI")]
    public GameObject terminalUI;
    public TMP_Text terminalText;

    [Header("Steps")]
    public GameObject[] steps;

    [Header("Timing")]
    public float createDelay = 0.5f;
    public float disappearDelay = 5f;
    public float errorDisplayTime = 3f;

    bool playerNear = false;
    bool terminalOpen = false;

    string playerInput = "";
    bool ignoreFirstKey = false;
    bool showError = false;
    bool isExecuting = false;

    string explanationText = ""; // Store the explanation separately

    void Start()
    {
        terminalUI.SetActive(false);
        ResetSteps();
        BuildExplanation();
    }

    void BuildExplanation()
    {
        int stepCount = steps.Length;
        explanationText =
        "This cliff seems to be " + stepCount + " steps long.\n\n" +

        "To create the steps we must use a FOR loop.\n\n" +

        "Syntax:\nfor i in range(start,end,interval)\n\n" +

        "Explanation:\n" +
        "start = where the loop begins\n" +
        "end = where the loop stops (not included)\n" +
        "interval = how much i increases\n\n" +

        "Example:\nfor i in range(1,5,1)\n\n" +

        "This means:\n" +
        "i = 1\n" +
        "i = 2\n" +
        "i = 3\n" +
        "i = 4\n\n";
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E) && !terminalOpen && !isExecuting)
        {
            OpenTerminal();
        }

        if (terminalOpen && !showError && !isExecuting)
        {
            ReadKeyboard();
        }
    }

    void OpenTerminal()
    {
        terminalOpen = true;
        isExecuting = false;
        Time.timeScale = 0f;

        terminalUI.SetActive(true);

        playerInput = "";
        ignoreFirstKey = true;
        showError = false;

        ResetSteps();

        UpdateTerminalDisplay();
    }

    void UpdateTerminalDisplay()
    {
        string displayText = explanationText;

        // Add current input section
        displayText += "Cliff Steps: " + steps.Length + "\n\n" +
                      "Your code:\n" +
                      "> " + playerInput + "\n{\n   StepsGen();\n}\n\n";

        // Add error section if there's an error
        if (showError)
        {
            displayText += "╔════════════════════════════╗\n" +
                          "║         ERROR!             ║\n" +
                          "╚════════════════════════════╝\n";
        }

        terminalText.text = displayText;
    }

    void ReadKeyboard()
    {
        foreach (char c in Input.inputString)
        {
            if (ignoreFirstKey)
            {
                ignoreFirstKey = false;
                continue;
            }

            if (c == '\b' && playerInput.Length > 0)
            {
                playerInput = playerInput.Substring(0, playerInput.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                CheckCode();
                return;
            }
            else
            {
                playerInput += c;
            }
        }

        UpdateTerminalDisplay();
    }

    void CheckCode()
    {
        if (!playerInput.Contains("range"))
        {
            ShowError("Error: range() not found.");
            return;
        }

        int startBracket = playerInput.IndexOf("(");
        int endBracket = playerInput.IndexOf(")");

        if (startBracket == -1 || endBracket == -1)
        {
            ShowError("Error: Invalid syntax. Missing parentheses.");
            return;
        }

        string inside = playerInput.Substring(startBracket + 1, endBracket - startBracket - 1);

        string[] nums = inside.Split(',');

        if (nums.Length < 3)
        {
            ShowError("Error: range needs 3 numbers (start, end, interval).");
            return;
        }

        int start, end, interval;

        if (!int.TryParse(nums[0].Trim(), out start) ||
            !int.TryParse(nums[1].Trim(), out end) ||
            !int.TryParse(nums[2].Trim(), out interval))
        {
            ShowError("Error: Invalid numbers. Use whole numbers only.");
            return;
        }

        if (start < 1)
        {
            ShowError("Error: Start must be >= 1.");
            return;
        }

        if (end > steps.Length + 1)
        {
            ShowError("Error: Cliff supports only " + steps.Length + " steps. End value too high.");
            return;
        }

        if (end <= start)
        {
            ShowError("Error: End must be greater than start.");
            return;
        }

        if (interval <= 0)
        {
            ShowError("Error: Interval must be greater than 0.");
            return;
        }

        // Success - execute the loop
        isExecuting = true;

        // Show success message while keeping explanation
        string successMsg = explanationText +
                           "Cliff Steps: " + steps.Length + "\n\n" +
                           "Your code:\n" +
                           "> " + playerInput + "\n{\n   StepsGen();\n}\n\n" +
                           "╔════════════════════════════╗\n" +
                           "║         SUCCESS!           ║\n" +
                           "╚════════════════════════════╝\n\n" +
                           "✓ Valid syntax! Running loop...\n";

        terminalText.text = successMsg;

        StartCoroutine(CreateSteps(start, end, interval));
    }

    void ShowError(string msg)
    {
        showError = true;

        // Keep the explanation and input visible, add error message below
        string errorDisplay = explanationText +
                             "Cliff Steps: " + steps.Length + "\n\n" +
                             "Your code:\n" +
                             "> " + playerInput + "\n{\n   StepsGen();\n}\n\n" +
                             "╔════════════════════════════╗\n" +
                             "║         ERROR!             ║\n" +
                             "╚════════════════════════════╝\n\n" +
                             msg + "\n\n";

        terminalText.text = errorDisplay;

        StartCoroutine(HandleError());
    }

    IEnumerator HandleError()
    {
        float timer = 0f;

        // Show countdown while explanation and error remain visible
        while (timer < errorDisplayTime)
        {
            if (!showError) yield break; // Exit if error cleared

            float remainingTime = errorDisplayTime - timer;

            // Update only the countdown line while keeping everything else
            string currentText = terminalText.text;
            int lastNewLine = currentText.LastIndexOf('\n');
            if (lastNewLine > 0)
            {
                // Keep the main content, update only the last line
                string baseText = currentText.Substring(0, lastNewLine + 1);
                terminalText.text = baseText +
                                   $"⏳ {remainingTime:F1}s - Press ENTER to continue";
            }

            // Check for ENTER key
            if (Input.GetKeyDown(KeyCode.Return))
            {
                break;
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Clear error and return to normal input mode
        showError = false;
        playerInput = "";
        ignoreFirstKey = true;
        UpdateTerminalDisplay();
    }

    IEnumerator CreateSteps(int start, int end, int interval)
    {
        Time.timeScale = 1f;

        for (int i = start; i < end; i += interval)
        {
            if (i - 1 >= 0 && i - 1 < steps.Length)
            {
                GameObject step = steps[i - 1];

                step.SetActive(true);

                // Append step creation message while keeping explanation
                terminalText.text += "✓ i = " + i + " → Step " + i + " created\n";

                StartCoroutine(RemoveStepAfterTime(step));
            }

            yield return new WaitForSeconds(createDelay);
        }

        terminalText.text += "\n✅ Loop completed successfully!\n";
        yield return new WaitForSeconds(2f);

        CloseTerminal();
    }

    IEnumerator RemoveStepAfterTime(GameObject step)
    {
        yield return new WaitForSeconds(disappearDelay);

        if (step != null && step.activeSelf)
        {
            step.SetActive(false);
        }
    }

    void ResetSteps()
    {
        foreach (GameObject s in steps)
        {
            if (s != null)
                s.SetActive(false);
        }
    }

    void CloseTerminal()
    {
        terminalUI.SetActive(false);
        terminalOpen = false;
        isExecuting = false;
        Time.timeScale = 1f;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}