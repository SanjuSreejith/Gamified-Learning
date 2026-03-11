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

    [Header("Hint System")]
    public BotHintSystem hintSystem;

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
    public Color inputTextColor = Color.yellow;

    [Header("Audio")]
    public AudioSource answerAudio;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    enum State
    {
        Idle,
        StatueTalking,
        WaitingForContinue,
        WaitingForAnswer
    }

    State state = State.Idle;

    private BasicQuestion[] questions;
    private int questionIndex = 0;
    public int platformsActivated = 0;

    private string typedInput = "";
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public int CorrectAnswersCount => platformsActivated;
    public int TotalQuestions => questions != null ? questions.Length : 0;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

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

        dialoguePanel.SetActive(true);
        speakerText.text = "Statue";

        questionIndex = 0;
        platformsActivated = 0;

        StartStatueLine("I do not test memory. I observe understanding.");
    }

    public void EndDialogueEarly()
    {
        StopAllCoroutines();

        if (hintSystem)
            hintSystem.DisableHints();

        dialoguePanel.SetActive(false);

        SetGamePaused(false);

        state = State.Idle;
    }

    // ---------------- DIALOGUE ----------------

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

        foreach (char c in line)
        {
            if (!isTyping)
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        isTyping = false;
        state = State.WaitingForContinue;
    }

    void CheckSkipTyping()
    {
        if (allowTypingSkip && Input.GetKeyDown(skipKey) && isTyping)
            isTyping = false;
    }

    void CheckContinueInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            AskQuestion();
    }

    void PlayAnswerSound(bool correct)
    {
        if (answerAudio == null) return;

        AudioClip clip = correct ? correctClip : wrongClip;

        if (clip != null)
            answerAudio.PlayOneShot(clip);
    }

    // ---------------- QUESTIONS ----------------

    void SetupQuestions()
    {
        questions = new BasicQuestion[]
        {
            new BasicQuestion(
                "If a user types Tarkya, what does input() return?",
                "tarkya",
                new string[]
                {
                    "Python does not change what the user types.",
                    "input() returns exactly what the user enters.",
                    "If the user typed Tarkya, what would the result be?"
                }
            ),

            new BasicQuestion(
                "print(\"Hello\", name) where name = Alex. What prints?",
                "hello alex",
                new string[]
                {
                    "print() automatically adds a space.",
                    "The output combines Hello and Alex.",
                    "Think: Hello + space + Alex."
                }
            ),

            new BasicQuestion(
                "Does input() return a number or text?",
                "text",
                new string[]
                {
                    "input() reads user input as a string.",
                    "Even digits are treated as something else.",
                    "Python considers input() result as text."
                }
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

        dialogueText.text =
            questions[questionIndex].questionText + "\n> ";

        state = State.WaitingForAnswer;

        SetGamePaused(true);

        if (hintSystem)
        {
            hintSystem.SetHints(questions[questionIndex].hints);
            hintSystem.EnableHints();
        }
    }

    // ---------------- ANSWER INPUT ----------------

    void HandleAnswerTyping()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (typedInput.Length > 0)
                    typedInput = typedInput.Substring(0, typedInput.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                SubmitAnswer();
                return;
            }
            else if (!char.IsControl(c))
            {
                typedInput += c;
            }
        }

        UpdateAnswerDisplay();
    }

    void UpdateAnswerDisplay()
    {
        dialogueText.text =
            questions[questionIndex].questionText +
            "\n> <color=#" + ColorUtility.ToHtmlStringRGB(inputTextColor) + ">" +
            typedInput + "</color>";
    }

    void SubmitAnswer()
    {
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
            PlayAnswerSound(true);

            ActivatePlatform();
            questionIndex++;

            StartStatueLine("Correct. Understanding acknowledged.");
        }
        else if (similarity >= almostCorrectThreshold)
        {
            PlayAnswerSound(false);
            StartStatueLine("Almost correct. Think carefully.");
        }
        else
        {
            PlayAnswerSound(false);
            StartStatueLine("Incorrect. Try again.");
        }
    }

    // ---------------- END ----------------

    void EndDialogue()
    {
        if (hintSystem)
            hintSystem.DisableHints();

        string finalMessage =
            platformsActivated == platforms.Length
            ? "All paths awaken. You may proceed."
            : "Some paths remain silent.";

        StartStatueLine(finalMessage);

        SetGamePaused(false);

        StartCoroutine(HidePanelDelayed());
    }

    IEnumerator HidePanelDelayed()
    {
        yield return new WaitForSecondsRealtime(2.2f);

        while (isTyping)
            yield return null;

        dialoguePanel.SetActive(false);

        state = State.Idle;
    }

    // ---------------- PLATFORM ----------------

    void ActivatePlatform()
    {
        if (platformsActivated >= platforms.Length)
            return;

        if (platforms[platformsActivated] != null)
        {
            platforms[platformsActivated].Resume();
            platformsActivated++;
        }
    }

    // ---------------- STRING SIMILARITY ----------------

    float CalculateSimilarity(string a, string b)
    {
        a = Regex.Replace(a.ToLower().Trim(), @"\s+", " ");
        b = Regex.Replace(b.ToLower().Trim(), @"\s+", " ");

        if (a == b) return 1f;

        int distance = ComputeLevenshteinDistance(a, b);
        int maxLength = Mathf.Max(a.Length, b.Length);

        return 1f - (float)distance / maxLength;
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

    void SetGamePaused(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
    }
}

[System.Serializable]
public class BasicQuestion
{
    public string questionText;
    public string correctAnswer;
    public string[] hints;

    public BasicQuestion(string q, string a, string[] h)
    {
        questionText = q;
        correctAnswer = a.ToLower();
        hints = h;
    }
}