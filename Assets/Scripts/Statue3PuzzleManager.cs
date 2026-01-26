using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalStatuePuzzle2D : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public CanvasGroup fadePanel;

    [Header("References")]
    public AdaptiveStatuePuzzle2D secondStatue;
    public StatueDialogueTriggerSystem2D firstStatue;
    public Transform teleportDestination;
    public GameObject player;

    [Header("Typing")]
    public float typeSpeed = 0.035f;
    public float fadeSpeed = 1.5f;

    [Header("Puzzle Parameters")]
    [Range(1, 4)] public int questionsToPass = 3;
    public int perfectThreshold = 5; // Combined correct answers from both statues
    public int averageThreshold = 3; // Minimum to proceed
    public string tutorialSceneName = "TutorialHall";
    [Header("Camera")]
    public followingcamera followCam;

    // ----------------- STATES -----------------
    enum State
    {
        Idle,
        Introduction,
        IntroductionWaiting,
        AskingQuestion,
        ReviewingAnswer,
        Conclusion,
        ConclusionWaiting,
        Teleporting,
        TeachingScene
    }

    enum OverallPerformance
    {
        Master,      // Excellent in both statues
        Competent,   // Good enough to proceed
        Novice,      // Needs more practice
        Struggling   // Needs to go back
    }

    State state = State.Idle;
    OverallPerformance overallPerformance;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private List<string> currentDialogueChunks = new List<string>();
    private int currentChunkIndex = 0;
    private bool waitForContinue = false;

    // ----------------- QUESTION DATA -----------------
    [System.Serializable]
    public class FinalQuestion
    {
        public string questionText;
        public string correctAnswer;
        public string hint;
        public string explanation;
        public string[] acceptableVariations;
        public string feedback;
    }

    [Header("Questions")]
    public FinalQuestion[] masterQuestions;
    public FinalQuestion[] competentQuestions;
    public FinalQuestion[] noviceQuestions;

    private FinalQuestion[] currentQuestions;
    private int currentQuestionIndex = 0;
    private int questionsCorrect = 0;
    private string typedInput = "";
    private int totalCorrectFromPrevious = 0;

    // ----------------- UNITY -----------------
    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (fadePanel != null)
        {
            fadePanel.alpha = 0;
            fadePanel.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        switch (state)
        {
            case State.IntroductionWaiting:
            case State.ConclusionWaiting:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    ContinueDialogue();
                }
                break;
            case State.AskingQuestion:
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

    // ----------------- PUZZLE START -----------------
    public void StartPuzzle()
    {
        if (state != State.Idle) return;
        if (dialoguePanel == null) return;

        dialoguePanel.SetActive(true);
        speakerText.text = "Final Guardian";

        // Calculate overall performance
        CalculateOverallPerformance();

        // Start introduction
        state = State.Introduction;
        StartCoroutine(IntroductionSequence());
    }

    void CalculateOverallPerformance()
    {
        // Get correct answers from both statues using safe methods
        int firstStatueCorrect = GetFirstStatueCorrectAnswers();
        int secondStatueCorrect = GetSecondStatueCorrectAnswers();

        totalCorrectFromPrevious = firstStatueCorrect + secondStatueCorrect;

        Debug.Log($"First Statue Correct: {firstStatueCorrect}, Second Statue Correct: {secondStatueCorrect}, Total: {totalCorrectFromPrevious}");

        if (totalCorrectFromPrevious >= perfectThreshold)
        {
            overallPerformance = OverallPerformance.Master;
            currentQuestions = masterQuestions;
        }
        else if (totalCorrectFromPrevious >= averageThreshold)
        {
            overallPerformance = OverallPerformance.Competent;
            currentQuestions = competentQuestions;
        }
        else if (totalCorrectFromPrevious >= 1)
        {
            overallPerformance = OverallPerformance.Novice;
            currentQuestions = noviceQuestions;
        }
        else
        {
            overallPerformance = OverallPerformance.Struggling;
        }

        Debug.Log($"Overall Performance: {overallPerformance}, Total Correct from Previous: {totalCorrectFromPrevious}");
    }

    // ----------------- NEW METHODS TO GET CORRECT ANSWERS -----------------
    int GetFirstStatueCorrectAnswers()
    {
        if (firstStatue == null)
        {
            Debug.LogWarning("First Statue reference is null!");
            return 0;
        }

        // Try different methods to get the correct answer count
        System.Type type = firstStatue.GetType();

        // Try public getter method first
        var getCorrectAnswersMethod = type.GetMethod("GetCorrectAnswersCount");
        if (getCorrectAnswersMethod != null)
        {
            return (int)getCorrectAnswersMethod.Invoke(firstStatue, null);
        }

        // Try public property
        var correctAnswersProperty = type.GetProperty("CorrectAnswersCount");
        if (correctAnswersProperty != null)
        {
            return (int)correctAnswersProperty.GetValue(firstStatue);
        }

        // Try public field
        var correctAnswersField = type.GetField("CorrectAnswersCount");
        if (correctAnswersField != null)
        {
            return (int)correctAnswersField.GetValue(firstStatue);
        }

        // Try platform count
        var platformsActivatedField = type.GetField("platformsActivated");
        if (platformsActivatedField != null)
        {
            return (int)platformsActivatedField.GetValue(firstStatue);
        }

        Debug.LogWarning("Could not find correct answer count in first statue!");
        return 0;
    }

    int GetSecondStatueCorrectAnswers()
    {
        if (secondStatue == null)
        {
            Debug.LogWarning("Second Statue reference is null!");
            return 0;
        }

        System.Type type = secondStatue.GetType();

        // Try public getter method first
        var getQuestionsCorrectMethod = type.GetMethod("GetQuestionsCorrect");
        if (getQuestionsCorrectMethod != null)
        {
            return (int)getQuestionsCorrectMethod.Invoke(secondStatue, null);
        }

        // Try public field
        var questionsCorrectField = type.GetField("questionsCorrect");
        if (questionsCorrectField != null)
        {
            return (int)questionsCorrectField.GetValue(secondStatue);
        }

        Debug.LogWarning("Could not find correct answer count in second statue!");
        return 0;
    }

    // ----------------- INTRODUCTION -----------------
    IEnumerator IntroductionSequence()
    {
        state = State.Introduction;

        // Greeting based on performance
        string[] greetingLines = GetGreetingDialogue();

        foreach (string line in greetingLines)
        {
            yield return StartCoroutine(TypeLineWithContinue(line));
            yield return new WaitForSeconds(0.1f);

            // Wait for continue input
            state = State.IntroductionWaiting;
            waitForContinue = true;
            yield return new WaitUntil(() => !waitForContinue);

            yield return new WaitForSeconds(0.1f);
        }

        // Start asking questions
        currentQuestionIndex = 0;
        questionsCorrect = 0;
        AskQuestion();
    }

    string[] GetGreetingDialogue()
    {
        switch (overallPerformance)
        {
            case OverallPerformance.Master:
                return new string[]
                {
                    "Excellent work. Your progress is remarkable.",
                    "You've shown deep understanding of Python fundamentals.",
                    "This final test will challenge your input/output skills.",
                    "Show me what you've truly mastered."
                };

            case OverallPerformance.Competent:
                return new string[]
                {
                    "Welcome. You've done well to reach this point.",
                    "I've been watching how you move through this house.",
                    "For someone just starting out, your grasp of Python is impressive.",
                    "Now comes the final check — input and output.",
                    "These are the last fundamentals you need to truly move forward."
                };

            case OverallPerformance.Novice:
                return new string[]
                {
                    "Hello there.",
                    "I can tell you've been learning inside this house for a while.",
                    "You're getting there, but understanding matters more than speed.",
                    "This final test focuses on input and output — simple, but important.",
                    "Take your time. Details matter here."
                };

            case OverallPerformance.Struggling:
                return new string[]
                {
                    "Ah... I see you're having difficulties.",
                    "The previous concepts seemed challenging for you.",
                    "Let's go back to the very basics one more time.",
                    "I'll explain input and output clearly.",
                    "Pay attention - this is fundamental."
                };

            default:
                return new string[] { "Welcome to the final challenge." };
        }
    }

    // ----------------- QUESTION HANDLING -----------------
    void AskQuestion()
    {
        if (currentQuestionIndex >= currentQuestions.Length)
        {
            ShowConclusion();
            return;
        }

        if (overallPerformance == OverallPerformance.Struggling)
        {
            // For struggling players, show teaching instead
            StartTeachingScene();
            return;
        }

        typedInput = "";
        FinalQuestion question = currentQuestions[currentQuestionIndex];

        // Display question with hint
        dialogueText.text = question.questionText + "\n> ";
        state = State.AskingQuestion;
    }

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
            FinalQuestion question = currentQuestions[currentQuestionIndex];
            dialogueText.text = question.questionText + "\n> " + typedInput;
        }
    }

    void SubmitAnswer()
    {
        if (currentQuestionIndex >= currentQuestions.Length) return;

        string input = typedInput.Trim();
        FinalQuestion question = currentQuestions[currentQuestionIndex];

        if (string.IsNullOrEmpty(input))
        {
            StartCoroutine(TypeLineWithPagination("Please type an answer."));
            return;
        }

        bool isCorrect = CheckAnswer(input, question);

        if (isCorrect)
        {
            questionsCorrect++;
            string feedback = $"Correct! {question.feedback}\nPress Enter to continue...";
            StartCoroutine(TypeLineWithPagination(feedback));
        }
        else
        {
            string response = $"Not quite. {question.explanation}\nPress Enter to continue...";
            StartCoroutine(TypeLineWithPagination(response));
        }

        currentQuestionIndex++;
        state = State.ReviewingAnswer;
    }

    bool CheckAnswer(string userAnswer, FinalQuestion question)
    {
        userAnswer = NormalizeAnswer(userAnswer);
        string correctAnswer = NormalizeAnswer(question.correctAnswer);

        // Direct match
        if (userAnswer == correctAnswer) return true;

        // Check acceptable variations
        if (question.acceptableVariations != null && question.acceptableVariations.Length > 0)
        {
            foreach (string variation in question.acceptableVariations)
            {
                if (userAnswer == NormalizeAnswer(variation))
                    return true;
            }
        }

        // Performance-based leniency
        switch (overallPerformance)
        {
            case OverallPerformance.Novice:
                return CheckNoviceAnswer(userAnswer, correctAnswer, question);

            case OverallPerformance.Competent:
                return CheckCompetentAnswer(userAnswer, correctAnswer);

            case OverallPerformance.Master:
                return CheckMasterAnswer(userAnswer, correctAnswer, question);
        }

        return false;
    }

    string NormalizeAnswer(string answer)
    {
        if (string.IsNullOrEmpty(answer)) return "";

        // Convert to lowercase
        answer = answer.ToLower();

        // Remove all whitespace
        answer = System.Text.RegularExpressions.Regex.Replace(answer, @"\s+", "");

        // Remove common Python punctuation that doesn't affect functionality
        answer = answer.Replace(";", "").Replace(":", "");

        // Standardize quotes
        answer = answer.Replace("'", "\"");

        // Remove trailing/leading spaces
        return answer.Trim();
    }

    bool CheckNoviceAnswer(string userAnswer, string correctAnswer, FinalQuestion question)
    {
        // For novice, check for presence of key concepts

        // Check if answer is about input
        if (correctAnswer.Contains("input(") || question.questionText.ToLower().Contains("input"))
        {
            if (userAnswer.Contains("input") || userAnswer.Contains("input("))
                return true;
        }

        // Check if answer is about print
        if (correctAnswer.Contains("print(") || question.questionText.ToLower().Contains("print"))
        {
            if (userAnswer.Contains("print") || userAnswer.Contains("print("))
                return true;
        }

        // Check for variable assignment
        if (correctAnswer.Contains("="))
        {
            if (userAnswer.Contains("="))
            {
                // Extract variable names
                string userVar = ExtractVariableName(userAnswer);
                string correctVar = ExtractVariableName(correctAnswer);

                if (!string.IsNullOrEmpty(userVar) && !string.IsNullOrEmpty(correctVar))
                {
                    if (userVar == correctVar) return true;
                }
                else if (!string.IsNullOrEmpty(userVar) && correctAnswer.Contains(userVar))
                {
                    return true;
                }
            }
        }

        // Check if they have the right value (for boolean questions)
        if (correctAnswer.Contains("true") || correctAnswer.Contains("false"))
        {
            if (userAnswer.Contains("true") || userAnswer.Contains("false"))
            {
                // Check if they have the right boolean value
                bool correctHasTrue = correctAnswer.Contains("true");
                bool userHasTrue = userAnswer.Contains("true");

                return correctHasTrue == userHasTrue;
            }
        }

        // Check for numeric values
        if (correctAnswer.Contains("100") || correctAnswer.Contains("0") || correctAnswer.Contains("1") ||
            correctAnswer.Contains("2") || correctAnswer.Contains("5") || correctAnswer.Contains("10"))
        {
            // Extract numbers from both answers
            string correctNumbers = ExtractNumbers(correctAnswer);
            string userNumbers = ExtractNumbers(userAnswer);

            if (!string.IsNullOrEmpty(correctNumbers) && !string.IsNullOrEmpty(userNumbers))
            {
                return correctNumbers == userNumbers;
            }
        }

        return false;
    }

    bool CheckCompetentAnswer(string userAnswer, string correctAnswer)
    {
        // For competent players, require correct structure but allow minor variations

        // Check for required keywords
        string[] requiredKeywords = ExtractKeywords(correctAnswer);
        int matches = 0;

        foreach (string keyword in requiredKeywords)
        {
            if (userAnswer.Contains(keyword)) matches++;
        }

        // Require at least 80% of key concepts
        float matchPercentage = (float)matches / requiredKeywords.Length;
        return matchPercentage >= 0.8f;
    }

    bool CheckMasterAnswer(string userAnswer, string correctAnswer, FinalQuestion question)
    {
        // For masters, be strict but allow advanced alternatives
        if (userAnswer == correctAnswer) return true;

        // Check for f-string alternatives
        if (correctAnswer.Contains("print(") && question.questionText.Contains("print"))
        {
            bool hasFString = userAnswer.Contains("f\"") || userAnswer.Contains("f'");
            bool hasFormat = userAnswer.Contains(".format(");
            bool hasConcat = userAnswer.Contains("+");

            if (hasFString || hasFormat || hasConcat)
            {
                // Check if they're printing the right variables
                string[] keyVariables = ExtractVariables(correctAnswer);
                int correctVars = 0;

                foreach (string variable in keyVariables)
                {
                    if (userAnswer.Contains(variable)) correctVars++;
                }

                return correctVars >= keyVariables.Length * 0.8f;
            }
        }

        // Check for correct function nesting
        if (correctAnswer.Contains("int(input("))
        {
            if (userAnswer.Contains("int(") && userAnswer.Contains("input("))
            {
                // Check if int wraps input
                int inputIndex = userAnswer.IndexOf("input(");
                int intIndex = userAnswer.IndexOf("int(");

                if (intIndex < inputIndex) // int( comes before input(
                {
                    // Check if they're doing the right operation
                    if (correctAnswer.Contains("*2") || correctAnswer.Contains("* 2"))
                    {
                        return userAnswer.Contains("*2") || userAnswer.Contains("* 2");
                    }
                    return true;
                }
            }
        }

        return false;
    }

    string ExtractVariableName(string code)
    {
        // Simple extraction of variable name before '='
        int equalsIndex = code.IndexOf('=');
        if (equalsIndex > 0)
        {
            string beforeEquals = code.Substring(0, equalsIndex);
            beforeEquals = beforeEquals.Replace("(", "").Replace(")", "").Trim();

            string[] parts = beforeEquals.Split(' ');
            if (parts.Length > 0)
            {
                string variable = parts[parts.Length - 1];
                if (!IsPythonKeyword(variable))
                {
                    return variable;
                }
            }
        }
        return "";
    }

    string[] ExtractKeywords(string code)
    {
        List<string> keywords = new List<string>();

        if (code.Contains("input(")) keywords.Add("input(");
        if (code.Contains("print(")) keywords.Add("print(");
        if (code.Contains("int(")) keywords.Add("int(");
        if (code.Contains("str(")) keywords.Add("str(");
        if (code.Contains("=")) keywords.Add("=");
        if (code.Contains("true")) keywords.Add("true");
        if (code.Contains("false")) keywords.Add("false");

        return keywords.ToArray();
    }

    string[] ExtractVariables(string code)
    {
        List<string> variables = new List<string>();

        string[] tokens = code.Split(new char[] { ' ', '=', '(', ')', ',', ';', '+', '*' },
                                   StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            string cleanToken = token.ToLower();
            if (!IsPythonKeyword(cleanToken) &&
                !cleanToken.StartsWith("\"") &&
                !cleanToken.StartsWith("'") &&
                !IsNumeric(cleanToken) &&
                cleanToken.Length > 1)
            {
                if (char.IsLetter(cleanToken[0]))
                {
                    variables.Add(cleanToken);
                }
            }
        }

        List<string> uniqueVars = new List<string>();
        foreach (string var in variables)
        {
            if (!uniqueVars.Contains(var))
                uniqueVars.Add(var);
        }

        return uniqueVars.ToArray();
    }

    bool IsPythonKeyword(string word)
    {
        string[] keywords = {
            "input", "print", "int", "str", "float", "bool",
            "true", "false", "none", "and", "or", "not",
            "if", "else", "elif", "for", "while", "def",
            "return", "import", "from", "as", "in", "is"
        };
        return System.Array.Exists(keywords, kw => kw == word.ToLower());
    }

    bool IsNumeric(string text)
    {
        return int.TryParse(text, out _) || float.TryParse(text, out _);
    }

    string ExtractNumbers(string text)
    {
        System.Text.StringBuilder numbers = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if (char.IsDigit(c))
                numbers.Append(c);
        }
        return numbers.ToString();
    }

    void NextQuestionOrConclude()
    {
        if (currentQuestionIndex < currentQuestions.Length)
        {
            AskQuestion();
        }
        else
        {
            ShowConclusion();
        }
    }

    // ----------------- CONCLUSION -----------------
    void ShowConclusion()
    {
        state = State.Conclusion;
        string conclusion = GetConclusionDialogue();
        StartCoroutine(TypeLineWithContinue(conclusion));
    }

    string GetConclusionDialogue()
    {
        float successRate = (float)questionsCorrect / currentQuestions.Length;

        switch (overallPerformance)
        {
            case OverallPerformance.Master:
                if (successRate >= 0.8f)
                {
                    return "Magnificent... truly magnificent.\nYour answers resonate with clarity and confidence.\nBeyond this chamber, someone awaits you.\n\nAbel — a wanderer of forgotten code — wishes to meet you.\nPress Enter to teleport and face what lies ahead...";
                }
                else if (successRate >= 0.6f)
                {
                    return "Well done.\nYour understanding is strong, though not yet flawless.\nStill, the path forward opens for you.\n\nA man named Abel has been watching your progress.\nPress Enter to teleport and meet him...";
                }
                break;

            case OverallPerformance.Competent:
                if (successRate >= 0.7f)
                {
                    return "You stand steady on the path of knowledge.\nNot perfect — but prepared.\nAhead, you will meet Abel.\nHe walks where logic and instinct collide.\n\nPress Enter to teleport...";
                }
                else if (successRate >= 0.5f)
                {
                    return "You have grasped the foundations.\nMistakes remain, but fear does not define you.\nAbel waits beyond this hall.\nHe does not judge — he observes.\n\nPress Enter to proceed...";
                }
                break;

            case OverallPerformance.Novice:
                if (successRate >= 0.6f)
                {
                    return "You move forward — slowly, but honestly.\nEvery step matters more than speed.\nSomeone wishes to speak with you.\nAbel has helped many who doubted themselves.\n\nPress Enter to continue...";
                }
                break;
        }

        // Default fallback
        return "You need more practice with input and output operations.\nLet me guide you through a focused tutorial...";
    }

    void DetermineFinalOutcome()
    {
        float successRate = (float)questionsCorrect / currentQuestions.Length;

        switch (overallPerformance)
        {
            case OverallPerformance.Master:
                if (successRate >= 0.6f)
                {
                    StartTeleport();
                }
                else
                {
                    StartTeachingScene();
                }
                break;

            case OverallPerformance.Competent:
                if (successRate >= 0.5f)
                {
                    StartTeleport();
                }
                else
                {
                    StartTeachingScene();
                }
                break;

            case OverallPerformance.Novice:
                if (successRate >= 0.6f)
                {
                    StartTeleport();
                }
                else
                {
                    StartTeachingScene();
                }
                break;

            case OverallPerformance.Struggling:
            default:
                StartTeachingScene();
                break;
        }
    }

    // ----------------- TELEPORT -----------------
    void StartTeleport()
    {
        state = State.Teleporting;
        StartCoroutine(TeleportSequence());
    }

    IEnumerator TeleportSequence()
    {
        // Show final message
        string finalMessage = GetFinalTeleportMessage();
        yield return StartCoroutine(TypeLineWithContinue(finalMessage));
        yield return new WaitForSeconds(1.2f);

        // Fade out
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float t = 0f;
            while (t < 1f)
            {
                fadePanel.alpha = Mathf.Lerp(0f, 1f, t);
                t += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            fadePanel.alpha = 1f;
        }

        yield return new WaitForSeconds(0.2f);

        // Camera unlock
        if (followCam != null)
        {
            followCam.UnlockX();
        }

        // Teleport player
        if (player != null && teleportDestination != null)
        {
            player.transform.position = teleportDestination.position;
            player.transform.rotation = teleportDestination.rotation;
        }

        yield return null;

        // Camera snap & lock
        if (followCam != null)
        {
            followCam.SnapToTarget();
            followCam.LockXAtCurrentPosition();
        }

        yield return new WaitForSeconds(0.25f);

        // Fade in
        if (fadePanel != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                fadePanel.alpha = Mathf.Lerp(1f, 0f, t);
                t += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }

        // Cleanup
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        state = State.Idle;
    }

    string GetFinalTeleportMessage()
    {
        switch (overallPerformance)
        {
            case OverallPerformance.Master:
                return "Your programming journey has just begun.\nBeyond this temple lies infinite possibilities.\nRemember: every expert was once a beginner.\nContinue learning, keep coding, and create wonders!";

            case OverallPerformance.Competent:
                return "You've proven yourself worthy to continue.\nThe path of programming requires persistence.\nTake what you've learned and build upon it.";

            case OverallPerformance.Novice:
                return "You've taken your first steps into programming.\nThe road is long, but every journey begins with a single step.\nKeep practicing, and you'll unlock great potential.";

            default:
                return "Proceeding to the next area...";
        }
    }

    // ----------------- TEACHING SCENE -----------------
    void StartTeachingScene()
    {
        state = State.TeachingScene;
        StartCoroutine(TransitionToTeaching());
    }

    IEnumerator TransitionToTeaching()
    {
        yield return StartCoroutine(TypeLineWithContinue("Let's visit the Tutorial Hall for focused practice..."));
        yield return new WaitForSeconds(1.5f);

        // Fade out
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float timer = 0;
            while (timer < 1)
            {
                fadePanel.alpha = Mathf.Lerp(0, 1, timer);
                timer += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            fadePanel.alpha = 1;
        }

        yield return new WaitForSeconds(1f);

        // Load teaching scene
        SceneManager.LoadScene(tutorialSceneName);
    }

    // ----------------- IMPROVED TYPING SYSTEM -----------------
    IEnumerator TypeLineWithContinue(string fullText)
    {
        isTyping = true;
        currentDialogueChunks.Clear();
        currentChunkIndex = 0;

        // Split text into lines
        string[] lines = fullText.Split('\n');
        List<string> chunk = new List<string>();

        // Create chunks of max 3 lines
        for (int i = 0; i < lines.Length; i++)
        {
            chunk.Add(lines[i]);

            if (chunk.Count == 3 || i == lines.Length - 1)
            {
                currentDialogueChunks.Add(string.Join("\n", chunk));
                chunk.Clear();
            }
        }

        // Safety
        if (currentDialogueChunks.Count == 0)
            currentDialogueChunks.Add(fullText);

        // Show first chunk
        yield return StartCoroutine(TypeChunkLimited(currentDialogueChunks[0]));

        isTyping = false;

        // Enter waiting state
        if (state == State.Introduction)
            state = State.IntroductionWaiting;
        else if (state == State.Conclusion)
            state = State.ConclusionWaiting;
    }


    IEnumerator TypeLineWithPagination(string line)
    {
        isTyping = true;
        currentDialogueChunks.Clear();
        currentChunkIndex = 0;

        // Split into chunks of max 3 lines
        string[] lines = line.Split('\n');
        List<string> chunk = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            chunk.Add(lines[i]);

            if (chunk.Count >= 3 || i == lines.Length - 1)
            {
                currentDialogueChunks.Add(string.Join("\n", chunk));
                chunk.Clear();
            }
        }

        if (currentDialogueChunks.Count == 0)
        {
            currentDialogueChunks.Add(line);
        }

        // Display first chunk
        yield return StartCoroutine(TypeChunk(currentDialogueChunks[0]));

        // If there are more chunks, wait for continue and show them
        for (int i = 1; i < currentDialogueChunks.Count; i++)
        {
            state = State.IntroductionWaiting;
            waitForContinue = true;
            yield return new WaitUntil(() => !waitForContinue);
            yield return StartCoroutine(TypeChunk(currentDialogueChunks[i]));
        }

        isTyping = false;
    }

    IEnumerator TypeChunk(string chunk)
    {
        dialogueText.text = "";

        foreach (char c in chunk)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Add continue prompt if there are more chunks
        if (currentChunkIndex < currentDialogueChunks.Count - 1)
        {
            dialogueText.text += "\n\n[Press Enter to continue...]";
        }

        currentChunkIndex++;
    }

    void ContinueDialogue()
    {
        // If currently typing, skip to end
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;

            // Show the full text
            if (currentDialogueChunks.Count > 0 && currentChunkIndex > 0)
            {
                dialogueText.text = currentDialogueChunks[currentChunkIndex - 1];
            }

            if (state == State.IntroductionWaiting)
            {
                dialogueText.text += "\n\n[Press Enter to continue...]";
            }
        }

        // Handle the continue input
        if (state == State.IntroductionWaiting)
        {
            waitForContinue = false;
            state = State.Introduction;
        }
        else if (state == State.ConclusionWaiting)
        {
            DetermineFinalOutcome();
        }
    }
    IEnumerator TypeChunkLimited(string chunk)
    {
        dialogueText.text = "";

        foreach (char c in chunk)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Show continue prompt ONLY if more chunks exist
        if (currentChunkIndex < currentDialogueChunks.Count - 1)
        {
            dialogueText.text += "\n\n[Press Enter to continue...]";
        }

        currentChunkIndex++;
    }

    // ----------------- EDITOR SETUP -----------------
    [ContextMenu("Setup Default Questions")]
    void SetupDefaultQuestions()
    {
        masterQuestions = new FinalQuestion[]
        {
            new FinalQuestion()
            {
                questionText = "Write code that asks for user's name and age, then prints a greeting with both",
                correctAnswer = "name = input('Enter name: '); age = input('Enter age: '); print('Hello', name, 'you are', age, 'years old')",
                hint = "Use two input() calls and one print() with multiple arguments",
                explanation = "input() gets text from user, print() can show multiple values separated by spaces",
                acceptableVariations = new string[] {
                    "name=input('Enter name: ')\nage=input('Enter age: ')\nprint('Hello',name,'you are',age,'years old')",
                    "name = input('Enter name: ')\nage = input('Enter age: ')\nprint(f'Hello {name} you are {age} years old')"
                },
                feedback = "Excellent! You understand complex input/output operations."
            },
            new FinalQuestion()
            {
                questionText = "Make the statue happy: hint - bool statueHappy;",
                correctAnswer = "statueHappy = True",
                hint = "Assign True to the boolean variable",
                explanation = "Boolean variables store True or False values",
                acceptableVariations = new string[] { "statueHappy=True", "statueHappy = true" },
                feedback = "Perfect! The statue is now happy."
            },
            new FinalQuestion()
            {
                questionText = "Convert input to integer and double it: x = input('Number: ')",
                correctAnswer = "x = int(input('Number: ')) * 2",
                hint = "Wrap input() with int() to convert, then multiply",
                explanation = "input() returns a string, int() converts it to a number for calculations",
                acceptableVariations = new string[] {
                    "x=int(input('Number: '))*2",
                    "num = int(input('Number: '))\nx = num * 2"
                },
                feedback = "Great! You know how to process numerical input."
            }
        };

        competentQuestions = new FinalQuestion[]
        {
            new FinalQuestion()
            {
                questionText = "Ask for user's name and print it",
                correctAnswer = "name = input('What is your name? '); print(name)",
                hint = "Use input() then print()",
                explanation = "input() gets user input, print() displays it",
                acceptableVariations = new string[] {
                    "name=input('What is your name? ')\nprint(name)",
                    "print(input('What is your name? '))"
                },
                feedback = "Good! You understand basic input/output."
            },
            new FinalQuestion()
            {
                questionText = "Make a variable 'gameActive' and set it to True",
                correctAnswer = "gameActive = True",
                hint = "Use = to assign True to the variable",
                explanation = "Variable assignment uses = operator",
                acceptableVariations = new string[] { "gameActive=True", "gameActive=true" },
                feedback = "Correct! You can set boolean variables."
            },
            new FinalQuestion()
            {
                questionText = "Print both text and a variable: score = 100",
                correctAnswer = "print('Score:', score)",
                hint = "print() can show multiple items separated by commas",
                explanation = "The comma in print() adds a space between items",
                acceptableVariations = new string[] { "print('Score:',score)", "print('Score: '+str(score))" },
                feedback = "Well done! You can combine text and variables in output."
            }
        };

        noviceQuestions = new FinalQuestion[]
        {
            new FinalQuestion()
            {
                questionText = "Get input from user and store in variable 'answer'",
                correctAnswer = "answer = input()",
                hint = "input() gets text, = stores it",
                explanation = "input() waits for user to type something and press Enter",
                acceptableVariations = new string[] { "answer=input()", "answer = input('')" },
                feedback = "Good start! That's how you get user input."
            },
            new FinalQuestion()
            {
                questionText = "Print 'Hello World' to the screen",
                correctAnswer = "print('Hello World')",
                hint = "Use print() with quotes",
                explanation = "Text must be in quotes for print() to display it literally",
                acceptableVariations = new string[] { "print(\"Hello World\")", "print('Hello World')" },
                feedback = "Correct! That's the basic output command."
            },
            new FinalQuestion()
            {
                questionText = "Create a variable 'health' with value 100",
                correctAnswer = "health = 100",
                hint = "variable_name = value",
                explanation = "Variables store values for later use in the program",
                acceptableVariations = new string[] { "health=100", "health =100", "health= 100" },
                feedback = "Right! Variables store data in your program."
            }
        };
    }
}