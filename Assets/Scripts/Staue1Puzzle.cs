using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class StatueDialogueTriggerSystem2D : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Platforms (3 ordered)")]
    public platformMove[] platforms;

    [Header("Typing")]
    public float typeSpeed = 0.035f;

    [Header("Skip Typing")]
    public KeyCode skipKey = KeyCode.Space;
    public bool allowTypingSkip = true;

    [Header("Answer Logic")]
    [Range(0.6f, 0.9f)]
    public float almostCorrectThreshold = 0.7f;

    [Header("Input Display")]
    public Color normalTextColor = Color.white;
    public Color inputTextColor = Color.yellow;

    // ---------------- STATES ----------------
    enum State
    {
        Idle,
        StatueTalking,
        WaitingForContinue,
        WaitingForAnswer
    }

    State state = State.Idle;

    // ---------------- DATA ----------------
    private BasicQuestion[] questions;
    private int questionIndex = 0;
    public int platformsActivated = 0; // Made public for second statue
    private string typedInput = "";
    private string currentLine = "";
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    // Public property to track performance
    public int CorrectAnswersCount => platformsActivated;
    public int TotalQuestions => questions != null ? questions.Length : 0;

    // ---------------- UNITY ----------------
    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        else
            Debug.LogError("Dialogue Panel not assigned!");

        if (speakerText == null || dialogueText == null)
            Debug.LogError("TextMeshProUGUI components not assigned!");

        SetupQuestions();
    }

    void Update()
    {
        switch (state)
        {
            case State.WaitingForContinue:
                CheckContinueInput();
                break;
            case State.WaitingForAnswer:
                HandleAnswerTyping();
                break;
            case State.StatueTalking:
                CheckSkipTyping();
                break;
        }
    }

    // ---------------- TRIGGER ----------------
    public void StartDialogue()
    {
        if (state != State.Idle) return;
        if (dialoguePanel == null) return;

        dialoguePanel.SetActive(true);
        speakerText.text = "Statue";
        questionIndex = 0;
        platformsActivated = 0;

        StartStatueLine("I do not test memory. I observe understanding.");
    }

    public void EndDialogueEarly()
    {
        StopAllCoroutines();
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        state = State.Idle;
    }

    // ---------------- DIALOGUE FLOW ----------------
    void StartStatueLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(string line)
    {
        state = State.StatueTalking;
        isTyping = true;
        dialogueText.text = "";
        currentLine = line;

        foreach (char c in line)
        {
            if (!isTyping) // Skip typing if interrupted
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        state = State.WaitingForContinue;
    }

    void CheckSkipTyping()
    {
        if (allowTypingSkip && Input.GetKeyDown(skipKey) && isTyping)
        {
            isTyping = false;
        }
    }

    void CheckContinueInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            AskQuestion();
        }
    }

    // ---------------- QUESTIONS ----------------
    void SetupQuestions()
    {
        questions = new BasicQuestion[]
        {
            new BasicQuestion(
                "If a user types Tarkya, what does input() return?",
                "tarkya",
                "Does Python change what the user types?"
            ),
            new BasicQuestion(
                "print(\"Hello\", name) where name is Alex, what is printed?",
                "hello alex",
                "Remember the space added by print()"
            ),
            new BasicQuestion(
                "Does input() return a number or text?",
                "text",
                "Even digits are treated as something else"
            )
        };
    }

    void AskQuestion()
    {
        if (questionIndex >= questions.Length)
        {
            EndDialogue();
            return;
        }

        typedInput = "";
        dialogueText.text = questions[questionIndex].questionText + "\n> ";
        state = State.WaitingForAnswer;
    }

    // ---------------- ANSWER INPUT ----------------
    void HandleAnswerTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b') // Backspace
            {
                if (typedInput.Length > 0)
                {
                    typedInput = typedInput.Substring(0, typedInput.Length - 1);
                }
            }
            else if (c == '\n' || c == '\r') // Enter
            {
                SubmitAnswer();
                return;
            }
            else if (!char.IsControl(c)) // Only add non-control characters
            {
                typedInput += c;
            }
        }

        UpdateAnswerDisplay();
    }

    void UpdateAnswerDisplay()
    {
        if (questionIndex < questions.Length)
        {
            dialogueText.text = questions[questionIndex].questionText + "\n> " +
                               "<color=#" + ColorUtility.ToHtmlStringRGB(inputTextColor) + ">" +
                               typedInput + "</color>";
        }
    }

    void SubmitAnswer()
    {
        if (questionIndex >= questions.Length) return;

        string input = typedInput.Trim().ToLower();
        BasicQuestion q = questions[questionIndex];

        if (string.IsNullOrEmpty(input))
        {
            StartStatueLine("Please provide an answer.");
            return;
        }

        float similarity = CalculateSimilarity(input, q.correctAnswer);

        if (input == q.correctAnswer)
        {
            ActivatePlatform();
            questionIndex++;
            StartStatueLine("Correct. Understanding acknowledged.");
        }
        else if (similarity >= almostCorrectThreshold)
        {
            StartStatueLine("Almost. Hint: " + q.hint);
        }
        else
        {
            StartStatueLine("Incorrect. Hint: " + q.hint);
        }
    }

    // ---------------- END ----------------
    void EndDialogue()
    {
        string finalMessage = platformsActivated == platforms.Length
            ? "All paths awaken. You may proceed."
            : "Some paths remain silent.";

        StartStatueLine(finalMessage);
        StartCoroutine(HidePanelDelayed());
    }

    IEnumerator HidePanelDelayed()
    {
        yield return new WaitForSeconds(2.2f);

        // Wait until typing is complete
        while (isTyping)
            yield return null;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        state = State.Idle;
    }

    // ---------------- PLATFORM ----------------
    void ActivatePlatform()
    {
        if (platformsActivated >= platforms.Length)
        {
            Debug.LogWarning("All platforms already activated!");
            return;
        }

        if (platforms[platformsActivated] != null)
        {
            platforms[platformsActivated].Resume();
            platformsActivated++;
        }
        else
        {
            Debug.LogError($"Platform at index {platformsActivated} is null!");
        }
    }

    // ---------------- UTILS ----------------
    float CalculateSimilarity(string a, string b)
    {
        // Clean strings
        a = Regex.Replace(a.ToLower().Trim(), @"\s+", " ");
        b = Regex.Replace(b.ToLower().Trim(), @"\s+", " ");

        if (a == b) return 1.0f;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0f;

        int distance = ComputeLevenshteinDistance(a, b);
        int maxLength = Mathf.Max(a.Length, b.Length);

        return 1.0f - (float)distance / maxLength;
    }

    int ComputeLevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++)
            dp[i, 0] = i;

        for (int j = 0; j <= b.Length; j++)
            dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                dp[i, j] = Mathf.Min(
                    Mathf.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }

        return dp[a.Length, b.Length];
    }
}

// ---------------- DATA CLASSES ----------------
[System.Serializable]
public class BasicQuestion
{
    public string questionText;
    public string correctAnswer;
    public string hint;

    public BasicQuestion(string q, string a, string h)
    {
        questionText = q;
        correctAnswer = a.ToLower(); // Ensure correct answer is always lowercase
        hint = h;
    }
}