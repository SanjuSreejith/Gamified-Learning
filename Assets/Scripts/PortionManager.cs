using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PythonOperatorChallengeController : MonoBehaviour
{
    /* ================= UI ================= */

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;
    public Sprite kuttanPortrait;

    TMPTypewriter typewriter;

    [Header("Terminal")]
    public GameObject terminalPanel;
    public TextMeshProUGUI terminalText;

    [Header("Hints")]
    public BotHintSystem hintSystem;

    [Header("Scene")]
    public string menuScene = "GameMenu";

    /* ================= TERMINAL ================= */

    string inputLine = "";
    bool editing;

    int challengeStep = 0;

    /* ================= ANSWERS ================= */

    const string MULTIPLY_CODE = "power=water*fire";
    const string ADD_CODE = "energy=water+wind";
    const string SUB_CODE = "remaining=energy-cost";

    /* ================= INITIALIZATION ================= */

    void Awake()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (terminalPanel) terminalPanel.SetActive(false);
    }

    void Start()
    {
        typewriter = dialogueText.GetComponent<TMPTypewriter>();
        StartCoroutine(IntroDialogue());
    }

    /* ================= INTRO ================= */

    IEnumerator IntroDialogue()
    {
        Speak("Kuttan", "Let's try a small programming challenge.");
        yield return Wait();

        Speak("Kuttan", "Python can perform mathematical operations.");
        yield return Wait();

        Speak("Kuttan", "We'll try three operators.");
        yield return Wait();

        Speak("Kuttan", "Multiplication, addition, and subtraction.");
        yield return Wait();

        StartMultiplyChallenge();
    }

    /* ================= MULTIPLICATION ================= */

    void StartMultiplyChallenge()
    {
        challengeStep = 1;

        Speak("Kuttan", "First challenge: multiplication.");
        StartCoroutine(WaitAndOpenMultiply());
    }

    IEnumerator WaitAndOpenMultiply()
    {
        yield return Wait();

        Speak("Kuttan", "Multiplication uses the * symbol.");
        yield return Wait();

        OpenTerminal(
            "MULTIPLICATION CHALLENGE\n\n" +
            "water = 5\n" +
            "fire = 3\n\n" +
            "Create variable 'power'\n"
        );

        if (hintSystem)
        {
            hintSystem.SetHints(new string[]
            {
                "Use * operator",
                "Multiply water and fire",
                "Example: a * b"
            });

            hintSystem.EnableHints();
        }
    }

    /* ================= ADDITION ================= */

    void StartAdditionChallenge()
    {
        challengeStep = 2;

        Speak("Kuttan", "Good. Now let's try addition.");
        StartCoroutine(WaitAndOpenAddition());
    }

    IEnumerator WaitAndOpenAddition()
    {
        yield return Wait();

        Speak("Kuttan", "Addition uses the + symbol.");
        yield return Wait();

        OpenTerminal(
            "ADDITION CHALLENGE\n\n" +
            "water = 5\n" +
            "wind = 2\n\n" +
            "Create variable 'energy'\n"
        );

        if (hintSystem)
        {
            hintSystem.SetHints(new string[]
            {
                "Use + operator",
                "Add water and wind",
                "Example: a + b"
            });
        }
    }

    /* ================= SUBTRACTION ================= */

    void StartSubtractionChallenge()
    {
        challengeStep = 3;

        Speak("Kuttan", "Last challenge: subtraction.");
        StartCoroutine(WaitAndOpenSubtraction());
    }

    IEnumerator WaitAndOpenSubtraction()
    {
        yield return Wait();

        Speak("Kuttan", "Subtraction uses the - symbol.");
        yield return Wait();

        OpenTerminal(
            "SUBTRACTION CHALLENGE\n\n" +
            "energy = 20\n" +
            "cost = 6\n\n" +
            "Create variable 'remaining'\n"
        );

        if (hintSystem)
        {
            hintSystem.SetHints(new string[]
            {
                "Use - operator",
                "Subtract cost from energy",
                "Example: a - b"
            });
        }
    }

    /* ================= TERMINAL ================= */

    void OpenTerminal(string text)
    {
        terminalPanel.SetActive(true);

        inputLine = "";
        editing = true;

        terminalText.text = text + "\n> ";
    }

    void Update()
    {
        if (!editing) return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b' && inputLine.Length > 0)
                inputLine = inputLine.Substring(0, inputLine.Length - 1);

            else if (c == '\n' || c == '\r')
                Submit();

            else if (!char.IsControl(c))
                inputLine += c;
        }

        terminalText.text = terminalText.text.Split('>')[0] + "> " + inputLine + "_";
    }

    /* ================= VALIDATION ================= */

    void Submit()
    {
        editing = false;
        terminalPanel.SetActive(false);

        string cleaned = inputLine.Replace(" ", "").ToLower();

        if (challengeStep == 1)
        {
            if (cleaned == MULTIPLY_CODE)
                StartCoroutine(MultiplySuccess());
            else
                Retry("Remember to multiply water and fire.");
        }
        else if (challengeStep == 2)
        {
            if (cleaned == ADD_CODE)
                StartCoroutine(AdditionSuccess());
            else
                Retry("You must add water and wind.");
        }
        else if (challengeStep == 3)
        {
            if (cleaned == SUB_CODE)
                StartCoroutine(SubtractionSuccess());
            else
                Retry("Subtract cost from energy.");
        }
    }

    /* ================= SUCCESS SEQUENCES ================= */

    IEnumerator MultiplySuccess()
    {
        if (hintSystem) hintSystem.DisableHints();

        Speak("Kuttan", "Correct.");
        yield return Wait();

        Speak("Kuttan", "5 multiplied by 3 equals 15.");
        yield return Wait();

        StartAdditionChallenge();
    }

    IEnumerator AdditionSuccess()
    {
        Speak("Kuttan", "Nice.");
        yield return Wait();

        Speak("Kuttan", "5 plus 2 equals 7.");
        yield return Wait();

        StartSubtractionChallenge();
    }

    IEnumerator SubtractionSuccess()
    {
        Speak("Kuttan", "Perfect.");
        yield return Wait();

        Speak("Kuttan", "20 minus 6 equals 14.");
        yield return Wait();

        Speak("Kuttan", "You understand Python operators.");
        yield return Wait();

        MarkSceneCompleted();

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(menuScene);
    }

    /* ================= RETRY ================= */

    void Retry(string message)
    {
        StartCoroutine(RetryRoutine(message));
    }

    IEnumerator RetryRoutine(string message)
    {
        Speak("Kuttan", message);
        yield return Wait();

        if (challengeStep == 1)
            StartMultiplyChallenge();
        else if (challengeStep == 2)
            StartAdditionChallenge();
        else
            StartSubtractionChallenge();
    }

    /* ================= DIALOGUE ================= */

    void Speak(string who, string text)
    {
        dialoguePanel.SetActive(true);

        speakerText.text = who;
        speakerImage.sprite = kuttanPortrait;

        if (typewriter)
            typewriter.Play(text);
        else
            dialogueText.text = text;

        DialogueBacklogManager.Instance?.AddLine(who, text);
    }

    IEnumerator Wait()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (typewriter && typewriter.IsTyping())
                    typewriter.Skip();
                else
                    break;
            }

            yield return null;
        }

        dialoguePanel.SetActive(false);
    }

    /* ================= SAVE ================= */

    void MarkSceneCompleted()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("Scene_" + sceneName + "_Completed", 1);
        PlayerPrefs.Save();
    }
}