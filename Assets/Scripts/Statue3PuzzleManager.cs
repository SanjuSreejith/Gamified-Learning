using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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
    public string printscenceName = "TutorialHall";

    // ----------------- STATES -----------------
    enum State
    {
        Idle,
        Introduction,
        AskingQuestion,
        ReviewingAnswer,
        Conclusion,
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
            case State.Introduction:
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
            case State.Conclusion:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    DetermineFinalOutcome();
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
        int firstStatueCorrect = (firstStatue != null) ? firstStatue.CorrectAnswersCount : 0;
        int secondStatueCorrect = (secondStatue != null) ? secondStatue.questionsCorrect : 0;

        totalCorrectFromPrevious = firstStatueCorrect + secondStatueCorrect;

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
    }

    // ----------------- INTRODUCTION -----------------
    IEnumerator IntroductionSequence()
    {
        state = State.Introduction;

        // Greeting based on performance
        string[] greetingLines = GetGreetingDialogue();

        foreach (string line in greetingLines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(0.5f);

            // Wait for continue input
            while (!Input.GetKeyDown(KeyCode.Return) && !Input.GetMouseButtonDown(0))
                yield return null;

            // Clear input to prevent immediate next
            yield return null;
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
          

            case OverallPerformance.Competent:
                return new string[]
  {
    "Welcome. You’ve done well to reach this point.",
    "I’ve been watching how you move through this house.",
    "For someone just starting out, your grasp of Python is impressive.",
    "Now comes the final check — input and output.",
    "These are the last fundamentals you need to truly move forward."
  };


            case OverallPerformance.Novice:
                return new string[]
    {
    "Hello there.",
    "I can tell you’ve been learning inside this house for a while.",
    "You’re getting there, but understanding matters more than speed.",
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
            StartCoroutine(TypeLine("Please type an answer."));
            return;
        }

        bool isCorrect = CheckAnswer(input, question);

        if (isCorrect)
        {
            questionsCorrect++;
            StartCoroutine(TypeLine($"Correct! {question.feedback}\nPress Enter to continue..."));
        }
        else
        {
            StartCoroutine(TypeLine($"Not quite. {question.explanation}\nPress Enter to continue..."));
        }

        currentQuestionIndex++;
        state = State.ReviewingAnswer;
    }

    bool CheckAnswer(string userAnswer, FinalQuestion question)
    {
        userAnswer = userAnswer.ToLower().Replace(" ", "");
        string correctAnswer = question.correctAnswer.ToLower().Replace(" ", "");

        if (userAnswer == correctAnswer) return true;

        if (question.acceptableVariations != null)
        {
            foreach (string variation in question.acceptableVariations)
            {
                if (userAnswer == variation.ToLower().Replace(" ", ""))
                    return true;
            }
        }

        // Allow for minor syntax variations for novice
        if (overallPerformance == OverallPerformance.Novice)
        {
            // Check if they have the right idea but minor syntax issues
            return CheckNoviceAnswer(userAnswer, correctAnswer);
        }

        return false;
    }

    bool CheckNoviceAnswer(string userAnswer, string correctAnswer)
    {
        // Remove common novice mistakes
        userAnswer = userAnswer.Replace(";", "").Replace(":", "");
        correctAnswer = correctAnswer.Replace(";", "").Replace(":", "");

        // Check for presence of key elements
        if (correctAnswer.Contains("input(") && userAnswer.Contains("input("))
            return true;
        if (correctAnswer.Contains("print(") && userAnswer.Contains("print("))
            return true;

        return userAnswer == correctAnswer;
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
        StartCoroutine(TypeLine(conclusion));
    }

    string GetConclusionDialogue()
    {
        float successRate = (float)questionsCorrect / currentQuestions.Length;

        switch (overallPerformance)
        {
            case OverallPerformance.Master:
                if (successRate >= 0.8f)
                {
                    return "Magnificent... truly magnificent.\n" +
                           "Your answers resonate with clarity and confidence.\n" +
                           "Beyond this chamber, someone awaits you.\n\n" +
                           "Abel — a wanderer of forgotten code — wishes to meet you.\n" +
                           "Press Enter to teleport and face what lies ahead...";
                }
                else if (successRate >= 0.6f)
                {
                    return "Well done.\n" +
                           "Your understanding is strong, though not yet flawless.\n" +
                           "Still, the path forward opens for you.\n\n" +
                           "A man named Abel has been watching your progress.\n" +
                           "Press Enter to teleport and meet him...";
                }
                break;

            case OverallPerformance.Competent:
                if (successRate >= 0.7f)
                {
                    return "You stand steady on the path of knowledge.\n" +
                           "Not perfect — but prepared.\n\n" +
                           "Ahead, you will meet Abel.\n" +
                           "He walks where logic and instinct collide.\n\n" +
                           "Press Enter to teleport...";
                }
                else if (successRate >= 0.5f)
                {
                    return "You have grasped the foundations.\n" +
                           "Mistakes remain, but fear does not define you.\n\n" +
                           "Abel waits beyond this hall.\n" +
                           "He does not judge — he observes.\n\n" +
                           "Press Enter to proceed...";
                }
                break;

            case OverallPerformance.Novice:
                if (successRate >= 0.6f)
                {
                    return "You move forward — slowly, but honestly.\n" +
                           "Every step matters more than speed.\n\n" +
                           "Someone wishes to speak with you.\n" +
                           "Abel has helped many who doubted themselves.\n\n" +
                           "Press Enter to continue...";
                }
                break;
        }

        // ❗ DO NOT CHANGE (as requested)
        return "You need more practice with input and output operations.\n" +
               "Let me guide you through a focused tutorial...";
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
        yield return StartCoroutine(TypeLine(finalMessage));
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

        yield return new WaitForSeconds(0.5f);

        // Teleport player
        if (player != null && teleportDestination != null)
        {
            player.transform.position = teleportDestination.position;
            player.transform.rotation = teleportDestination.rotation;
        }

        yield return new WaitForSeconds(0.5f);

        // Fade in
        if (fadePanel != null)
        {
            float timer = 0;
            while (timer < 1)
            {
                fadePanel.alpha = Mathf.Lerp(1, 0, timer);
                timer += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            fadePanel.alpha = 0;
            fadePanel.gameObject.SetActive(false);
        }

        // Hide dialogue
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        state = State.Idle;
    }

    string GetFinalTeleportMessage()
    {
        switch (overallPerformance)
        {
            case OverallPerformance.Master:
                return "Your programming journey has just begun.\n" +
                       "Beyond this temple lies infinite possibilities.\n" +
                       "Remember: every expert was once a beginner.\n" +
                       "Continue learning, keep coding, and create wonders!";

            case OverallPerformance.Competent:
                return "You've proven yourself worthy to continue.\n" +
                       "The path of programming requires persistence.\n" +
                       "Take what you've learned and build upon it.";

            case OverallPerformance.Novice:
                return "You've taken your first steps into programming.\n" +
                       "The road is long, but every journey begins with a single step.\n" +
                       "Keep practicing, and you'll unlock great potential.";

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
        yield return StartCoroutine(TypeLine("Let's visit the Tutorial Hall for focused practice..."));
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
        SceneManager.LoadScene(printscenceName);
    }

    // ----------------- HELPER METHODS -----------------
    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    void ContinueDialogue()
    {
        // Only continue if not currently typing
        if (isTyping) return;

        // This is handled in the coroutine wait
    }

    // ----------------- EDITOR SETUP -----------------
    // This method can be called from the inspector to set up default questions
    [ContextMenu("Setup Default Questions")]
    void SetupDefaultQuestions()
    {
        // Master Questions (Advanced input/output)
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

        // Competent Questions (Intermediate)
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

        // Novice Questions (Basic)
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