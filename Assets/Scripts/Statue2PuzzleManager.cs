using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class AdaptiveStatuePuzzle2D : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Reference to First Statue")]
    public StatueDialogueTriggerSystem2D firstStatue;

    [Header("Platforms for Second Puzzle")]
    public platformMove[] puzzlePlatforms;

    [Header("Typing")]
    public float typeSpeed = 0.035f;

    [Header("Answer Logic")]
    [Range(0.6f, 0.9f)]
    public float almostCorrectThreshold = 0.7f;

    [Header("Puzzle Parameters")]
    [Range(1, 3)] public int maxAttemptsPerQuestion = 2;
    public int questionsToPass = 2;

    // ---------------- STATES ----------------
    enum State
    {
        Idle,
        StatueTalking,
        WaitingForContinue,
        WaitingForAnswer,
        ReviewingAnswer
    }

    enum PlayerPerformance
    {
        Perfect,    // All correct in first puzzle
        Average,    // 1-2 correct in first puzzle
        Poor        // None correct in first puzzle
    }

    State state = State.Idle;
    PlayerPerformance performance;

    // ---------------- DATA ----------------
    private AdvancedQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private int currentAttempt = 0;
    private string typedInput = "";
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private int questionsCorrect = 0;

    // ---------------- UNITY ----------------
    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
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
            case State.ReviewingAnswer:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    NextQuestionOrConclude();
                }
                break;
        }
    }

    // ---------------- PUZZLE START ----------------
    public void StartPuzzle()
    {
        if (state != State.Idle) return;
        if (dialoguePanel == null) return;

        dialoguePanel.SetActive(true);
        speakerText.text = "Second Statue";

        // Get performance from first statue
        AssessFirstStatuePerformance();

        // Start with assessment dialogue
        StartCoroutine(AssessmentSequence());
    }

    void AssessFirstStatuePerformance()
    {
        if (firstStatue != null)
        {
            int correctAnswers = firstStatue.CorrectAnswersCount;

            // Determine performance level
            if (correctAnswers == 3)
            {
                performance = PlayerPerformance.Perfect;
                currentQuestions = GetPerfectQuestions();
            }
            else if (correctAnswers >= 1)
            {
                performance = PlayerPerformance.Average;
                currentQuestions = GetAverageQuestions();
            }
            else
            {
                performance = PlayerPerformance.Poor;
                currentQuestions = GetPoorQuestions();
            }
        }
        else
        {
            // Default to average if no first statue reference
            performance = PlayerPerformance.Average;
            currentQuestions = GetAverageQuestions();
        }
    }

    IEnumerator AssessmentSequence()
    {
        state = State.StatueTalking;

        string[] assessmentLines = GetAssessmentDialogue();

        foreach (string line in assessmentLines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);
        }

        // Start teaching phase
        StartCoroutine(TeachingSequence());
    }

    string[] GetAssessmentDialogue()
    {
        switch (performance)
        {
            case PlayerPerformance.Perfect:
                return new string[]
                {
                    "Your understanding of output is profound.",
                    "Let us explore advanced print formatting..."
                };

            case PlayerPerformance.Average:
                return new string[]
                {
                    "I sense you understand basic output.",
                    "Let me reinforce print function concepts..."
                };

            case PlayerPerformance.Poor:
                return new string[]
                {
                    "The foundations of output need strengthening.",
                    "Let me teach you how print() works..."
                };

            default:
                return new string[] { "Let us begin with Python output..." };
        }
    }

    // ---------------- TEACHING PHASE ----------------
    IEnumerator TeachingSequence()
    {
        state = State.StatueTalking;

        string[] teachingLines = GetTeachingDialogue();

        foreach (string line in teachingLines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);
        }

        // Start asking questions
        currentQuestionIndex = 0;
        currentAttempt = 0;
        questionsCorrect = 0;

        AskQuestion();
    }

    string[] GetTeachingDialogue()
    {
        switch (performance)
        {
            case PlayerPerformance.Perfect:
                return new string[]
                {
                    "Advanced concept: print() with advanced formatting.",
                    "Examples:",
                    "print(f'Hello {name}') - f-string formatting",
                    "print('Hello', name, sep='|') - custom separator",
                    "print('Line1', end=' ') - prevent newline"
                };

            case PlayerPerformance.Average:
                return new string[]
                {
                    "Let's review: print() basics.",
                    "1. print('text') - prints text",
                    "2. print('Hello', 'World') - adds space automatically",
                    "3. print('Hello' + 'World') - concatenates without space"
                };

            case PlayerPerformance.Poor:
                return new string[]
                {
                    "Let's start from basics:",
                    "1. print() displays text",
                    "2. Quotes define text: 'Hello' or \"World\"",
                    "3. Example: print('Hello World')",
                    "4. Everything inside quotes prints exactly as written"
                };
        }
        return new string[] { "Let's begin..." };
    }

    // ---------------- QUESTION PHASE ----------------
    void AskQuestion()
    {
        if (currentQuestionIndex >= currentQuestions.Length)
        {
            ConcludePuzzle();
            return;
        }

        typedInput = "";
        AdvancedQuestion question = currentQuestions[currentQuestionIndex];

        // Show question with hint in the same panel
        dialogueText.text = question.questionText + "\n(Hint: " + question.hint + ")\n> ";
        state = State.WaitingForAnswer;
    }

    // ---------------- ANSWER HANDLING ----------------
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
            else if (!char.IsControl(c))
            {
                typedInput += c;
            }
        }

        UpdateAnswerDisplay();
    }

    void UpdateAnswerDisplay()
    {
        if (currentQuestionIndex < currentQuestions.Length)
        {
            AdvancedQuestion question = currentQuestions[currentQuestionIndex];
            dialogueText.text = question.questionText + "\n(Hint: " + question.hint + ")\n> " + typedInput;
        }
    }

    void SubmitAnswer()
    {
        if (currentQuestionIndex >= currentQuestions.Length) return;

        string input = typedInput.Trim().ToLower();
        AdvancedQuestion question = currentQuestions[currentQuestionIndex];

        if (string.IsNullOrEmpty(input))
        {
            StartStatueLine("Please provide an answer.");
            return;
        }

        bool isCorrect = CheckAnswer(input, question.correctAnswer, question.acceptableVariations);
        currentAttempt++;

        if (isCorrect)
        {
            HandleCorrectAnswer(question);
        }
        else
        {
            HandleWrongAnswer(question);
        }
    }

    bool CheckAnswer(string userAnswer, string correctAnswer, string[] variations)
    {
        if (userAnswer == correctAnswer) return true;

        if (variations != null)
        {
            foreach (string variation in variations)
            {
                if (userAnswer == variation.ToLower()) return true;
            }
        }

        // Allow partial credit for average/poor performance
        if (performance != PlayerPerformance.Perfect)
        {
            float similarity = CalculateSimilarity(userAnswer, correctAnswer);
            return similarity >= (performance == PlayerPerformance.Average ? 0.85f : 0.7f);
        }

        return false;
    }

    void HandleCorrectAnswer(AdvancedQuestion question)
    {
        questionsCorrect++;

        // Activate platform for this question
        if (currentQuestionIndex < puzzlePlatforms.Length && puzzlePlatforms[currentQuestionIndex] != null)
        {
            puzzlePlatforms[currentQuestionIndex].Resume();
        }

        currentQuestionIndex++;
        currentAttempt = 0;

        string feedback = "Correct! " + question.feedback;
        if (currentQuestionIndex < currentQuestions.Length)
        {
            feedback += "\nPress Enter for next question...";
        }

        StartStatueLine(feedback);
        state = State.ReviewingAnswer;
    }

    void HandleWrongAnswer(AdvancedQuestion question)
    {
        if (currentAttempt < maxAttemptsPerQuestion)
        {
            // Give another attempt with more help
            string hint = GetProgressiveHint(question, currentAttempt);
            StartStatueLine($"Try again. {hint}\n> ");
            typedInput = "";
            state = State.WaitingForAnswer;
        }
        else
        {
            // Max attempts reached
            string explanation = question.explanation;
            StartStatueLine($"The correct answer is: {question.correctAnswer}\n{explanation}\nPress Enter to continue...");
            currentQuestionIndex++;
            currentAttempt = 0;
            state = State.ReviewingAnswer;
        }
    }

    string GetProgressiveHint(AdvancedQuestion question, int attempt)
    {
        switch (attempt)
        {
            case 1: return "Hint: " + question.hint;
            case 2: return "Detailed hint: " + question.detailedHint;
            default: return "Think about Python's print behavior.";
        }
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

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        // Only go to waiting if we're not in reviewing state
        if (state == State.StatueTalking)
        {
            state = State.WaitingForContinue;
        }
    }

    void CheckContinueInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            if (state == State.WaitingForContinue)
            {
                state = State.StatueTalking; // This will trigger the next line in the sequence
            }
        }
    }

    void NextQuestionOrConclude()
    {
        if (currentQuestionIndex < currentQuestions.Length && questionsCorrect < questionsToPass)
        {
            AskQuestion();
        }
        else
        {
            ConcludePuzzle();
        }
    }

    // ---------------- CONCLUSION ----------------
    void ConcludePuzzle()
    {
        string conclusion = GetConclusionDialogue();
        StartStatueLine(conclusion);
        StartCoroutine(HidePanelDelayed());
    }

    string GetConclusionDialogue()
    {
        if (questionsCorrect >= questionsToPass)
        {
            switch (performance)
            {
                case PlayerPerformance.Perfect:
                    return "Masterful! Your understanding of output is complete. All paths awaken.";
                case PlayerPerformance.Average:
                    return "Well done! You've mastered print function basics. Proceed with confidence.";
                case PlayerPerformance.Poor:
                    return "Progress made! Output foundations strengthened. Continue your journey.";
            }
        }

        return "More practice needed. Return when ready to try again.";
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

    // ---------------- QUESTION POOLS ----------------
    AdvancedQuestion[] GetPerfectQuestions()
    {
        return new AdvancedQuestion[]
        {
            new AdvancedQuestion(
                "What does this print: print(f'Result: {5+3}')?",
                "result: 8",
                "f-strings evaluate expressions inside {}",
                "The expression 5+3 is calculated first",
                new string[] { "result:8", "Result: 8" },
                "f-strings evaluate expressions: {5+3} becomes 8, so it prints 'Result: 8'",
                "Excellent! You understand f-string evaluation."
            ),
            new AdvancedQuestion(
                "What prints: print('Line1', end=' '); print('Line2')?",
                "line1 line2",
                "end=' ' prevents newline, adds space instead",
                "Without newline, both print on same line",
                new string[] { "line1 line2" },
                "end=' ' replaces the default newline with a space, so both print on same line",
                "Perfect! You understand print's end parameter."
            ),
            new AdvancedQuestion(
                "What prints: name='Sam'; print(f'Hello {name.upper()}')?",
                "hello SAM",
                ".upper() converts to uppercase",
                "f-strings can call string methods",
                new string[] { "hello sam" },
                "name.upper() returns 'SAM', so f-string becomes 'Hello SAM'",
                "Correct! f-strings can include method calls."
            )
        };
    }

    AdvancedQuestion[] GetAverageQuestions()
    {
        return new AdvancedQuestion[]
        {
            new AdvancedQuestion(
                "What prints: print('Hello' + 'World')?",
                "helloworld",
                "+ concatenates strings without space",
                "Strings join directly: 'Hello'+'World'='HelloWorld'",
                new string[] { "helloworld" },
                "The + operator concatenates strings exactly: 'Hello' + 'World' = 'HelloWorld'",
                "Good! You remember string concatenation."
            ),
            new AdvancedQuestion(
                "What prints: print('Python', 'is', 'fun')?",
                "python is fun",
                "Commas in print() add spaces automatically",
                "Multiple arguments separated by commas",
                new string[] { "python is fun" },
                "print() with commas adds spaces between arguments: 'Python' + ' ' + 'is' + ' ' + 'fun'",
                "Correct! Commas add automatic spacing."
            ),
            new AdvancedQuestion(
                "What prints: x = 5; print('Value:', x)?",
                "value: 5",
                "print() can mix text and variables",
                "Variable value is inserted with space",
                new string[] { "value:5", "Value: 5" },
                "print() displays all arguments separated by spaces: 'Value:' + ' ' + '5'",
                "Right! print() handles mixed types automatically."
            )
        };
    }

    AdvancedQuestion[] GetPoorQuestions()
    {
        return new AdvancedQuestion[]
        {
            new AdvancedQuestion(
                "What prints: print('Hello World')?",
                "hello world",
                "Exactly what's inside quotes",
                "Quotes define the text to print",
                new string[] { "hello world" },
                "print() displays exactly the text between quotes: 'Hello World'",
                "Good! print() shows text exactly as written."
            ),
            new AdvancedQuestion(
                "What prints: print('Hi'); print('There')?",
                "hi\nthere",
                "Each print() starts a new line",
                "print() adds newline automatically",
                new string[] { "hi\nthere", "hi there on two lines" },
                "print() adds a newline character at the end, so 'Hi' and 'There' appear on separate lines",
                "Correct! Each print() creates a new line."
            ),
            new AdvancedQuestion(
                "What prints: print('A','B','C')?",
                "a b c",
                "Multiple items separated by spaces",
                "Commas add spaces between items",
                new string[] { "a b c" },
                "print() with multiple arguments separates them with spaces: 'A' + ' ' + 'B' + ' ' + 'C'",
                "Right! Multiple items print with spaces between them."
            )
        };
    }

    // ---------------- UTILS ----------------
    float CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0f;

        a = Regex.Replace(a.ToLower().Trim(), @"\s+", " ");
        b = Regex.Replace(b.ToLower().Trim(), @"\s+", " ");

        if (a == b) return 1f;

        int[,] dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

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

        int maxLength = Mathf.Max(a.Length, b.Length);
        return 1f - (float)dp[a.Length, b.Length] / maxLength;
    }
}

// ---------------- ADVANCED QUESTION CLASS ----------------
[System.Serializable]
public class AdvancedQuestion
{
    public string questionText;
    public string correctAnswer;
    public string hint;
    public string detailedHint;
    public string[] acceptableVariations;
    public string explanation;
    public string feedback;

    public AdvancedQuestion(string q, string a, string h, string dh, string[] vars, string exp, string fb = "Good understanding!")
    {
        questionText = q;
        correctAnswer = a.ToLower();
        hint = h;
        detailedHint = dh;
        acceptableVariations = vars;
        explanation = exp;
        feedback = fb;
    }
}