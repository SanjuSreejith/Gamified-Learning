using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ShapeChallengeController : MonoBehaviour
{
    [Header("Shape Prefabs")]
    public GameObject circlePrefab;
    public GameObject squarePrefab;
    public GameObject cylinderPrefab;

    [Header("Spawn Parent")]
    public Transform spawnParent;

    [Header("Spawn Settings")]
    public float spacing = 2f;           // Horizontal spacing between shapes
    public float verticalSpacing = 2f;    // Vertical spacing between rows
    public float baseHeight = 1.2f;       // Starting Y position for first row
    public int shapesPerRow = 5;          // Number of shapes per row

    [Header("Terminal UI")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    [Header("Scene")]
    public string menuScene = "GameMenu";

    [Header("Question Animation")]
    public float typewriterSpeed = 0.05f;

    [Header("Hint System")]
    public BotHintSystem hintSystem;

    [Header("Rewards")]
    public int coinReward = 10;            // Coins awarded per new question
    [Header("Coin UI")]
    public CoinUIController coinUI;

    /* ================= POOLS ================= */

    List<GameObject> circlePool = new List<GameObject>();
    List<GameObject> squarePool = new List<GameObject>();
    List<GameObject> cylinderPool = new List<GameObject>();

    /* ================= TERMINAL ================= */

    string inputLine = "";
    bool editing = false;
    bool isAnimating = false;

    int challengeStep = 0;
    int savedCompletedStep = 0;            // Highest step completed (0 = none)

    // Store current challenge data for retry
    int currentCircles = 0;
    int currentSquares = 0;
    int currentCylinders = 0;
    string currentQuestionText = "";
    string[] currentHints = new string[3];
    int currentCorrectAnswer = 0;

    /* ================= START ================= */

    void Start()
    {
        LoadProgress();
        challengeStep = savedCompletedStep;

        // Add this debug check
        if (CoinManager.Instance == null)
        {
            Debug.LogError("CoinManager.Instance is NULL! Make sure CoinManager is in the scene and properly initialized.");
        }
        else
        {
            Debug.Log("CoinManager found. Current coins: " + CoinManager.Instance.coins);
        }

        NextChallenge();
    }

    /* ================= PROGRESS SAVE/LOAD ================= */

    void LoadProgress()
    {
        string key = "ShapeChallenge_" + SceneManager.GetActiveScene().name + "_CompletedStep";
        savedCompletedStep = PlayerPrefs.GetInt(key, 0);
        Debug.Log("Loaded completed step: " + savedCompletedStep);
    }

    void SaveProgress()
    {
        string key = "ShapeChallenge_" + SceneManager.GetActiveScene().name + "_CompletedStep";
        PlayerPrefs.SetInt(key, savedCompletedStep);
        PlayerPrefs.Save();
        Debug.Log("Saved completed step: " + savedCompletedStep);
    }

    /* ================= POOL SYSTEM ================= */

    GameObject GetShape(List<GameObject> pool, GameObject prefab)
    {
        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab, spawnParent);
        pool.Add(newObj);
        return newObj;
    }

    void ClearShapes()
    {
        foreach (Transform child in spawnParent)
            child.gameObject.SetActive(false);
    }

    void SpawnShapes(List<GameObject> pool, GameObject prefab, int count, Color color, ref int totalPlaced)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = GetShape(pool, prefab);

            int row = totalPlaced / shapesPerRow;
            int col = totalPlaced % shapesPerRow;

            float x = col * spacing;
            float y = baseHeight + row * verticalSpacing;

            obj.transform.position = new Vector3(x, y, 0);

            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
                r.material.color = color;

            totalPlaced++;
        }
    }

    void SpawnShapeRow(int circles, int squares, int cylinders)
    {
        ClearShapes();

        int totalPlaced = 0;

        SpawnShapes(circlePool, circlePrefab, circles, Color.blue, ref totalPlaced);
        SpawnShapes(squarePool, squarePrefab, squares, Color.red, ref totalPlaced);
        SpawnShapes(cylinderPool, cylinderPrefab, cylinders, Color.green, ref totalPlaced);
    }

    /* ================= QUESTION ANIMATION ================= */

    IEnumerator TypewriterAnimation(string fullText)
    {
        isAnimating = true;
        terminalText.text = "";

        foreach (char c in fullText)
        {
            terminalText.text += c;
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        terminalText.text += "\n> ";
        isAnimating = false;
    }

    void AnimateNewQuestion(string questionText)
    {
        StartCoroutine(TypewriterAnimation(questionText));
    }

    /* ================= CHALLENGE SYSTEM ================= */

    void NextChallenge()
    {
        challengeStep++;
        StartChallenge();
    }

    void StartChallenge()
    {
        // If we've already completed all 20, finish immediately
        if (challengeStep > 20)
        {
            Finish();
            return;
        }

        switch (challengeStep)
        {
            case 1:
                SetupChallenge(3, 0, 0, 3,
                    "==================================\n" +
                    "        CHALLENGE 1 OF 20\n" +
                    "==================================\n\n" +
                    "Count the circles.\n\n" +
                    "Create variable 'circles'",
                    new string[] {
                        "Hint 1: Look at the blue shapes - those are circles",
                        "Hint 2: Count all the blue circles on the platform",
                        "Hint 3: There are 3 circles. Write: circles = 3"
                    });
                break;

            case 2:
                SetupChallenge(2, 3, 0, 5,
                    "==================================\n" +
                    "        CHALLENGE 2 OF 20\n" +
                    "==================================\n\n" +
                    "Add circles and squares.\n\n" +
                    "Create variable 'total'",
                    new string[] {
                        "Hint 1: Count the blue circles (2) and red squares (3)",
                        "Hint 2: Add them together: 2 + 3",
                        "Hint 3: The answer is 5. Write: total = 5"
                    });
                break;

            case 3:
                SetupChallenge(1, 2, 3, 6,
                    "==================================\n" +
                    "        CHALLENGE 3 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes.\n\n" +
                    "Create variable 'total'",
                    new string[] {
                        "Hint 1: Count circles (1), squares (2), and cylinders (3)",
                        "Hint 2: Add all three numbers together",
                        "Hint 3: 1 + 2 + 3 = 6. Write: total = 6"
                    });
                break;

            case 4:
                SetupChallenge(4, 1, 0, 3,
                    "==================================\n" +
                    "        CHALLENGE 4 OF 20\n" +
                    "==================================\n\n" +
                    "Subtract squares from circles.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Count circles (4) and squares (1)",
                        "Hint 2: Subtract squares from circles: 4 - 1",
                        "Hint 3: The answer is 3. Write: result = 3"
                    });
                break;

            case 5:
                SetupChallenge(2, 2, 2, 6,
                    "==================================\n" +
                    "        CHALLENGE 5 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes.\n\n" +
                    "Create variable 'total'",
                    new string[] {
                        "Hint 1: Count all shapes: 2 circles, 2 squares, 2 cylinders",
                        "Hint 2: Add them: 2 + 2 + 2",
                        "Hint 3: The answer is 6. Write: total = 6"
                    });
                break;

            case 6:
                SetupChallenge(5, 3, 0, 8,
                    "==================================\n" +
                    "        CHALLENGE 6 OF 20\n" +
                    "==================================\n\n" +
                    "Add circles and squares.\n\n" +
                    "Create variable 'sum'",
                    new string[] {
                        "Hint 1: Count circles (5) and squares (3)",
                        "Hint 2: Add them: 5 + 3",
                        "Hint 3: The answer is 8. Write: sum = 8"
                    });
                break;

            case 7:
                SetupChallenge(0, 4, 2, 6,
                    "==================================\n" +
                    "        CHALLENGE 7 OF 20\n" +
                    "==================================\n\n" +
                    "Add squares and cylinders.\n\n" +
                    "Create variable 'total'",
                    new string[] {
                        "Hint 1: Count squares (4) and cylinders (2)",
                        "Hint 2: Add them: 4 + 2",
                        "Hint 3: The answer is 6. Write: total = 6"
                    });
                break;

            case 8:
                SetupChallenge(3, 3, 3, 9,
                    "==================================\n" +
                    "        CHALLENGE 8 OF 20\n" +
                    "==================================\n\n" +
                    "Multiply circles by squares.\n\n" +
                    "Create variable 'product'",
                    new string[] {
                        "Hint 1: Count circles (3) and squares (3)",
                        "Hint 2: Multiply: 3 x 3",
                        "Hint 3: The answer is 9. Write: product = 9"
                    });
                break;

            case 9:
                SetupChallenge(6, 2, 0, 3,
                    "==================================\n" +
                    "        CHALLENGE 9 OF 20\n" +
                    "==================================\n\n" +
                    "Divide circles by squares.\n\n" +
                    "Create variable 'quotient'",
                    new string[] {
                        "Hint 1: Count circles (6) and squares (2)",
                        "Hint 2: Divide: 6 / 2",
                        "Hint 3: The answer is 3. Write: quotient = 3"
                    });
                break;

            case 10:
                SetupChallenge(4, 4, 4, 24,
                    "==================================\n" +
                    "        CHALLENGE 10 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes, then multiply by 2.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Add all shapes: 4 + 4 + 4 = 12",
                        "Hint 2: Multiply the total by 2: 12 x 2",
                        "Hint 3: The answer is 24. Write: result = 24"
                    });
                break;

            case 11:
                SetupChallenge(7, 1, 0, 6,
                    "==================================\n" +
                    "        CHALLENGE 11 OF 20\n" +
                    "==================================\n\n" +
                    "Subtract squares from circles.\n\n" +
                    "Create variable 'difference'",
                    new string[] {
                        "Hint 1: Count circles (7) and squares (1)",
                        "Hint 2: Subtract: 7 - 1",
                        "Hint 3: The answer is 6. Write: difference = 6"
                    });
                break;

            case 12:
                SetupChallenge(2, 5, 3, 10,
                    "==================================\n" +
                    "        CHALLENGE 12 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes.\n\n" +
                    "Create variable 'total'",
                    new string[] {
                        "Hint 1: Add circles (2) + squares (5) + cylinders (3)",
                        "Hint 2: 2 + 5 + 3",
                        "Hint 3: The answer is 10. Write: total = 10"
                    });
                break;

            case 13:
                SetupChallenge(8, 0, 4, 12,
                    "==================================\n" +
                    "        CHALLENGE 13 OF 20\n" +
                    "==================================\n\n" +
                    "Add circles and cylinders.\n\n" +
                    "Create variable 'sum'",
                    new string[] {
                        "Hint 1: Count circles (8) and cylinders (4)",
                        "Hint 2: Add them: 8 + 4",
                        "Hint 3: The answer is 12. Write: sum = 12"
                    });
                break;

            case 14:
                SetupChallenge(0, 6, 6, 6,
                    "==================================\n" +
                    "        CHALLENGE 14 OF 20\n" +
                    "==================================\n\n" +
                    "Add squares and cylinders, then divide by 2.\n\n" +
                    "Create variable 'average'",
                    new string[] {
                        "Hint 1: Add squares (6) + cylinders (6) = 12",
                        "Hint 2: Divide the total by 2: 12 / 2",
                        "Hint 3: The answer is 6. Write: average = 6"
                    });
                break;

            case 15:
                SetupChallenge(5, 5, 5, 45,
                    "==================================\n" +
                    "        CHALLENGE 15 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes, then multiply by 3.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Add all shapes: 5 + 5 + 5 = 15",
                        "Hint 2: Multiply by 3: 15 x 3",
                        "Hint 3: The answer is 45. Write: result = 45"
                    });
                break;

            case 16:
                SetupChallenge(9, 3, 0, 3,
                    "==================================\n" +
                    "        CHALLENGE 16 OF 20\n" +
                    "==================================\n\n" +
                    "Subtract squares from circles, then divide by 2.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Subtract: 9 - 3 = 6",
                        "Hint 2: Divide by 2: 6 / 2",
                        "Hint 3: The answer is 3. Write: result = 3"
                    });
                break;

            case 17:
                SetupChallenge(4, 2, 6, 0,
                    "==================================\n" +
                    "        CHALLENGE 17 OF 20\n" +
                    "==================================\n\n" +
                    "Add circles and squares, then subtract cylinders.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Add circles (4) + squares (2) = 6",
                        "Hint 2: Subtract cylinders: 6 - 6",
                        "Hint 3: The answer is 0. Write: result = 0"
                    });
                break;

            case 18:
                SetupChallenge(10, 5, 5, 4,
                    "==================================\n" +
                    "        CHALLENGE 18 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes, then divide by 5.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Add all shapes: 10 + 5 + 5 = 20",
                        "Hint 2: Divide by 5: 20 / 5",
                        "Hint 3: The answer is 4. Write: result = 4"
                    });
                break;

            case 19:
                SetupChallenge(6, 6, 6, 42,
                    "==================================\n" +
                    "        CHALLENGE 19 OF 20\n" +
                    "==================================\n\n" +
                    "Multiply circles by squares, then add cylinders.\n\n" +
                    "Create variable 'result'",
                    new string[] {
                        "Hint 1: Multiply circles (6) x squares (6) = 36",
                        "Hint 2: Add cylinders: 36 + 6",
                        "Hint 3: The answer is 42. Write: result = 42"
                    });
                break;

            case 20:
                SetupChallenge(8, 4, 2, 112,
                    "==================================\n" +
                    "        CHALLENGE 20 OF 20\n" +
                    "==================================\n\n" +
                    "Add all shapes, then multiply by the number of circles.\n\n" +
                    "Create variable 'finalResult'",
                    new string[] {
                        "Hint 1: Add all shapes: 8 + 4 + 2 = 14",
                        "Hint 2: Multiply by circles (8): 14 x 8",
                        "Hint 3: The answer is 112. Write: finalResult = 112"
                    });
                break;

            default:
                Finish();
                break;
        }
    }

    void SetupChallenge(int circles, int squares, int cylinders, int correctAnswer, string questionText, string[] hints)
    {
        currentCircles = circles;
        currentSquares = squares;
        currentCylinders = cylinders;
        currentQuestionText = questionText;
        currentHints = hints;
        currentCorrectAnswer = correctAnswer;

        SpawnShapeRow(circles, squares, cylinders);

        if (hintSystem != null)
        {
            hintSystem.SetHints(hints);
            hintSystem.EnableHints();
        }

        OpenTerminalWithAnimation(questionText);
    }

    void RetryCurrentChallenge()
    {
        SpawnShapeRow(currentCircles, currentSquares, currentCylinders);

        if (hintSystem != null)
        {
            hintSystem.SetHints(currentHints);
            hintSystem.EnableHints();
        }

        OpenTerminalWithAnimation(currentQuestionText);
    }

    /* ================= TERMINAL ================= */

    void OpenTerminalWithAnimation(string text)
    {
        terminalPanel.SetActive(true);
        inputLine = "";

        AnimateNewQuestion(text);

        StartCoroutine(WaitForAnimationAndEnableInput());
    }

    IEnumerator WaitForAnimationAndEnableInput()
    {
        yield return new WaitUntil(() => !isAnimating);
        editing = true;
    }

    void Update()
    {
        if (!editing || isAnimating) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b' && inputLine.Length > 0)
                inputLine = inputLine.Substring(0, inputLine.Length - 1);

            else if (c == '\n' || c == '\r')
                Submit();

            else if (!char.IsControl(c))
                inputLine += c;
        }

        string currentDisplayText = terminalText.text;
        if (currentDisplayText.Contains(">"))
        {
            string questionPart = currentDisplayText.Split('>')[0];
            terminalText.text = questionPart + "> " + inputLine + "_";
        }
    }

    /* ================= ANSWER CHECK ================= */

    void Submit()
    {
        editing = false;
        terminalPanel.SetActive(false);

        string code = inputLine.Replace(" ", "").ToLower();

        bool correct = false;

        if (code.Contains("="))
        {
            string[] parts = code.Split('=');
            if (parts.Length == 2)
            {
                int userAnswer;
                if (int.TryParse(parts[1], out userAnswer))
                {
                    correct = (userAnswer == currentCorrectAnswer);
                }
                else
                {
                    string expression = parts[1];
                    try
                    {
                        if (expression.Contains("+"))
                        {
                            string[] nums = expression.Split('+');
                            int sum = 0;
                            foreach (string num in nums) sum += int.Parse(num);
                            correct = (sum == currentCorrectAnswer);
                        }
                        else if (expression.Contains("-"))
                        {
                            string[] nums = expression.Split('-');
                            int result = int.Parse(nums[0]);
                            for (int i = 1; i < nums.Length; i++) result -= int.Parse(nums[i]);
                            correct = (result == currentCorrectAnswer);
                        }
                        else if (expression.Contains("*"))
                        {
                            string[] nums = expression.Split('*');
                            int product = 1;
                            foreach (string num in nums) product *= int.Parse(num);
                            correct = (product == currentCorrectAnswer);
                        }
                        else if (expression.Contains("/"))
                        {
                            string[] nums = expression.Split('/');
                            int quotient = int.Parse(nums[0]) / int.Parse(nums[1]);
                            correct = (quotient == currentCorrectAnswer);
                        }
                    }
                    catch { correct = false; }
                }
            }
        }

        if (correct)
        {
            terminalPanel.SetActive(true);
            StartCoroutine(ShowSuccessAndNext());
        }
        else
        {
            terminalPanel.SetActive(true);
            StartCoroutine(ShowWrongAndRetry());
        }
    }
    IEnumerator ShowSuccessAndNext()
    {
        terminalText.text = "";

        string successMsg = "CORRECT! Well done!";
        foreach (char c in successMsg)
        {
            terminalText.text += c;
            yield return new WaitForSecondsRealtime(0.05f);
        }

        // Award coins only if this question hasn't been completed before
        if (challengeStep > savedCompletedStep)
        {
            // Update saved progress first
            savedCompletedStep = challengeStep;
            SaveProgress();

            // Award coins via CoinManager with better error handling
            if (CoinManager.Instance != null)
            {
                Debug.Log($"Awarding {coinReward} coins for challenge {challengeStep}");
                CoinManager.Instance.AddCoins(coinReward);

                if (coinUI != null)
                {
                    coinUI.ShowAndAdd(coinReward);
                }
                else
                {
                    Debug.LogWarning("CoinUI is null, but coins were still added.");
                }

                terminalText.text += $"\n\n+{coinReward} coins!";
            }
            else
            {
                Debug.LogError("CoinManager.Instance is NULL! Cannot award coins.");
                terminalText.text += "\n\n[ERROR] Coin system not found!";
            }
        }
        else
        {
            // This question was already completed (shouldn't happen in normal flow, but just in case)
            terminalText.text += "\n\n(Question already completed, no coins awarded.)";
            Debug.Log($"Challenge {challengeStep} already completed. No coins awarded.");
        }

        yield return new WaitForSecondsRealtime(0.5f);

        terminalText.text += "\n\nMoving to next challenge...";
        yield return new WaitForSecondsRealtime(1f);

        terminalPanel.SetActive(false);
        NextChallenge();
    }

    IEnumerator ShowWrongAndRetry()
    {
        terminalText.text = "";

        string wrongMsg = "WRONG ANSWER!";
        foreach (char c in wrongMsg)
        {
            terminalText.text += c;
            yield return new WaitForSecondsRealtime(0.05f);
        }

        Vector3 originalPos = terminalText.transform.localPosition;
        for (int i = 0; i < 3; i++)
        {
            terminalText.transform.localPosition = originalPos + new Vector3(5, 0, 0);
            yield return new WaitForSecondsRealtime(0.03f);
            terminalText.transform.localPosition = originalPos + new Vector3(-5, 0, 0);
            yield return new WaitForSecondsRealtime(0.03f);
        }
        terminalText.transform.localPosition = originalPos;

        terminalText.text += $"\n\nYour answer: {inputLine}\n\nTry again! Press H for hints.";
        yield return new WaitForSecondsRealtime(2f);

        terminalText.text += "\n\nRetrying same question...";
        yield return new WaitForSecondsRealtime(1f);

        terminalPanel.SetActive(false);
        RetryCurrentChallenge();
    }

    /* ================= FINISH ================= */

    void Finish()
    {
        if (hintSystem != null) hintSystem.DisableHints();

        terminalPanel.SetActive(true);
        StartCoroutine(ShowFinishCelebration());
    }

    IEnumerator ShowFinishCelebration()
    {
        terminalText.text = "";

        string[] celebration = {
            "CONGRATULATIONS!",
            "",
            "You've completed all 20 challenges!",
            "",
            "You are a coding master!",
            "",
            "Returning to menu..."
        };

        foreach (string line in celebration)
        {
            terminalText.text += line + "\n";
            yield return new WaitForSecondsRealtime(0.3f);
        }

        yield return new WaitForSecondsRealtime(2f);
        ReturnToMenu();
    }

    void ReturnToMenu()
    {
        MarkSceneCompleted();
        SceneManager.LoadScene(menuScene);
    }

    /* ================= SAVE ================= */

    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        PlayerPrefs.Save();
    }
}