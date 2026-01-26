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
    public int questionsCorrect = 0;

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
                    "Excellent work! You have a good grasp of Python basics.",
                    "Let's build on that foundation with more practice..."
                };

            case PlayerPerformance.Average:
                return new string[]
                {
                    "You understand some concepts but need more practice.",
                    "Pay attention - these next questions are crucial.",
                    "If you don't get these right, we'll need to revisit the basics."
                };

            case PlayerPerformance.Poor:
                return new string[]
                {
                    "Let me explain the fundamentals clearly before we begin.",
                    "These are the building blocks of programming.",
                    "If you don't understand these, we must go back to square one."
                };

            default:
                return new string[] { "Let's begin with Python fundamentals..." };
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

        // Explain topics specific to each performance level
        yield return StartCoroutine(ExplainTopics());

        // Start asking questions
        currentQuestionIndex = 0;
        currentAttempt = 0;
        questionsCorrect = 0;

        AskQuestion();
    }

    IEnumerator ExplainTopics()
    {
        state = State.StatueTalking;

        if (performance == PlayerPerformance.Poor)
        {
            yield return StartCoroutine(TypeLine("Let me explain each concept carefully:"));
            yield return new WaitForSeconds(0.5f);

            // Explain print()
            yield return StartCoroutine(TypeLine("1. print() - displays text on screen"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("   Example: print('Hello') shows: Hello"));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);

            // Explain variables
            yield return StartCoroutine(TypeLine("2. Variables - store information"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("   Format: name = value"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("   Example: score = 10"));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);

            // Explain printing variables
            yield return StartCoroutine(TypeLine("3. Printing variables"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("   Example: x = 5; print(x) shows: 5"));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);
        }
        else if (performance == PlayerPerformance.Average)
        {
            yield return StartCoroutine(TypeLine("Now I'll explain what we'll be testing:"));
            yield return new WaitForSeconds(0.5f);

            yield return StartCoroutine(TypeLine("We'll work with variables and print()"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("Remember: print() shows output"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("Variables store data: name = value"));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);

            yield return StartCoroutine(TypeLine("Important: print() can show both text and variables"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("Example: print('Score:', score)"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(TypeLine("This prints: Score: [value of score]"));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);
        }
        else // Perfect
        {
            yield return StartCoroutine(TypeLine("You're ready for more advanced practice."));
            yield return new WaitForSeconds(0.5f);
            state = State.WaitingForContinue;
            yield return new WaitUntil(() => state == State.StatueTalking);
        }

        yield return StartCoroutine(TypeLine("Ready? Let's begin the questions."));
        yield return new WaitForSeconds(0.5f);
        state = State.WaitingForContinue;
        yield return new WaitUntil(() => state == State.StatueTalking);
    }

    string[] GetTeachingDialogue()
    {
        switch (performance)
        {
            case PlayerPerformance.Perfect:
                return new string[]
                {
                    "Great job on the basics!",
                    "Now let's practice variables and output more deeply."
                };

            case PlayerPerformance.Average:
                return new string[]
                {
                    "I see you've learned some Python.",
                    "Let's strengthen your understanding of core concepts."
                };

            case PlayerPerformance.Poor:
                return new string[]
                {
                    "Let's start from the beginning.",
                    "I'll teach you the absolute fundamentals of Python."
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
            default: return "Think about the Python syntax.";
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
                    return "Excellent! You've mastered variables and output. Keep progressing!";
                case PlayerPerformance.Average:
                    return "Good work! You understand the basics. Practice will make you perfect.";
                case PlayerPerformance.Poor:
                    return "Progress made! Remember: print() shows output, variables store data. Keep practicing!";
            }
        }

        return "You need more practice with these fundamentals. Return when you're ready.";
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
                "What prints: speed = 4; print('Platform speed:', speed)?",
                "platform speed: 4",
                "print() can combine text and variables",
                "The comma adds space automatically",
                new string[] { "platform speed:4", "platform speed: 4" },
                "print('text', variable) shows the text, then a space, then the variable value",
                "Perfect! You understand how to display variables with labels."
            ),
            new AdvancedQuestion(
                "What prints: x = 10; y = 5; print('Total:', x + y)?",
                "total: 15",
                "print() can show calculation results",
                "x + y equals 15, print shows it with label",
                new string[] { "total:15", "total: 15" },
                "The expression x + y is calculated first (10 + 5 = 15), then printed with the text",
                "Excellent! You can combine calculations with print()."
            ),
            new AdvancedQuestion(
                "What prints: is_active = True; print('Active status:', is_active)?",
                "active status: true",
                "Boolean values can be printed too",
                "True prints as 'True' (capital T)",
                new string[] { "active status:true", "active status: true" },
                "Boolean variables store True/False values, and print() displays them as text",
                "Correct! You understand printing boolean variables."
            )
        };
    }

    AdvancedQuestion[] GetAverageQuestions() 
    {
        return new AdvancedQuestion[]
        {
            new AdvancedQuestion(
                "What prints: print('Platform', 7, 'enabled')?",
                "platform 7 enabled",
                "print() can show multiple items",
                "Each item separated by space",
                new string[] { "platform 7 enabled" },
                "print() with multiple arguments shows each one with spaces in between",
                "Good! You understand print() with multiple items."
            ),
            new AdvancedQuestion(
                "What prints: score = 100; print('Score:', score)?",
                "score: 100",
                "print() shows variable values",
                "Variable 'score' has value 100",
                new string[] { "score:100", "score: 100" },
                "The variable 'score' contains 100, so print() shows 'Score: 100'",
                "Correct! You can display variable values with labels."
            ),
            new AdvancedQuestion(
                "How would you create a variable 'health' with value 50?",
                "health = 50",
                "Use = to assign values",
                "No quotes around numbers",
                new string[] { "health=50", "health =50", "health= 50" },
                "To create a variable: variable_name = value. For numbers, don't use quotes.",
                "Right! That's how you store numerical values."
            )
        };
    }

    AdvancedQuestion[] GetPoorQuestions()
    {
        return new AdvancedQuestion[]
        {
            new AdvancedQuestion(
                "What prints: print('Game Started')?",
                "game started",
                "print() shows exactly what's in quotes",
                "Text inside quotes is displayed as-is",
                new string[] { "game started" },
                "print('text') displays the exact text between the quotation marks",
                "Good! You understand the basic print() function."
            ),
            new AdvancedQuestion(
                "How do you create a variable called 'lives' with value 3?",
                "lives = 3",
                "variable_name = value",
                "Use = to assign, no quotes for numbers",
                new string[] { "lives=3", "lives =3", "lives= 3" },
                "Variables are created by writing the name, then =, then the value",
                "Correct! That's how you store data in variables."
            ),
            new AdvancedQuestion(
                "What prints: x = 5; print(x)?",
                "5",
                "print(variable) shows its value",
                "Variable x contains 5",
                new string[] { "5" },
                "When you print a variable, it shows the value stored in that variable",
                "Right! Printing variables shows their stored values."
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