using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrintSceneTutorialController : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;

    [Header("Portrait")]
    public Sprite kuttanPortrait;

    TMPTypewriter typewriter;

    [Header("Control UI (Separate Icons)")]
    public GameObject moveLeftUI;
    public GameObject moveRightUI;
    public GameObject jumpUI;
    public GameObject interactUI;
    public GameObject hintUI;
    public GameObject backlogUI;
    public GameObject pauseUI;
    public GameObject enterUI;

    [Header("Interaction Success UI")]
    public GameObject interactionSuccessPanel;
    public TextMeshProUGUI interactionSuccessText;
    public float interactionFeedbackTime = 1.5f;

    [Header("Hint System")]
    public BotHintSystem hintSystem;

    [Header("Blocker")]
    public BoxCollider2D blockerCollider;

    [Header("Door System")]
    public DoorPrintf_TerminalSystem doorSystem;

    void Awake()
    {
        typewriter = dialogueText.GetComponent<TMPTypewriter>();

        dialoguePanel.SetActive(true);

        DisableAllControls();

        if (interactionSuccessPanel)
            interactionSuccessPanel.SetActive(false);

        if (blockerCollider)
            blockerCollider.enabled = true;

        if (doorSystem)
            doorSystem.tutorialActive = true;
    }

    void Start()
    {
        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
        yield return new WaitForSeconds(0.4f);

        /* ===== INTRO DIALOGUE ===== */

        Speak("Kuttan", "Before we begin, let me show you the basic controls.");
        yield return WaitForEnter();

        Speak("Kuttan", "Watch the icons and try them when they appear.");
        yield return WaitForEnter();

        dialoguePanel.SetActive(false);

        /* ===== MOVEMENT ===== */

        yield return HoldKey(moveLeftUI, KeyCode.A, 0.4f);
        yield return HoldKey(moveRightUI, KeyCode.D, 0.4f);

        /* ===== JUMP ===== */

        yield return PressKey(jumpUI, KeyCode.Space);

        /* ===== INTERACT ===== */

        yield return PressKey(interactUI, KeyCode.E);

        /* ===== HINT SYSTEM ===== */

        hintUI.SetActive(true);

        if (hintSystem)
        {
            hintSystem.SetHints(new string[]
            {
                "Hints appear here.",
                "Use them if you get stuck."
            });

            hintSystem.EnableHints();
        }

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.F1));

        hintUI.SetActive(false);

        if (hintSystem)
            hintSystem.DisableHints();

        /* ===== BACKLOG ===== */

        yield return PressTwice(backlogUI, KeyCode.Tab);

        /* ===== PAUSE ===== */

        yield return PressTwice(pauseUI, KeyCode.Escape);

        /* ===== ENTER ===== */

        yield return PressKey(enterUI, KeyCode.Return);

        /* ===== FINAL DIALOGUE ===== */

        dialoguePanel.SetActive(true);

        Speak("Kuttan", "Good. You now know the controls.");
        yield return WaitForEnter();

        Speak("Kuttan", "Let's begin the lesson.");
        yield return WaitForEnter();

        EndTutorial();
    }

    /* ========================= */
    /* CONTROL METHODS           */
    /* ========================= */

    IEnumerator PressKey(GameObject ui, KeyCode key)
    {
        ui.SetActive(true);

        yield return new WaitUntil(() => Input.GetKeyDown(key));

        ui.SetActive(false);

        if (key == KeyCode.E)
        {
            yield return ShowInteractionSuccess();
        }

        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator PressTwice(GameObject ui, KeyCode key)
    {
        ui.SetActive(true);

        // First press (open)
        yield return new WaitUntil(() => Input.GetKeyDown(key));
        yield return new WaitUntil(() => Input.GetKeyUp(key));

        // Second press (close)
        yield return new WaitUntil(() => Input.GetKeyDown(key));

        ui.SetActive(false);

        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator HoldKey(GameObject ui, KeyCode key, float holdTime)
    {
        ui.SetActive(true);

        float timer = 0f;

        while (timer < holdTime)
        {
            if (Input.GetKey(key))
                timer += Time.deltaTime;
            else
                timer = 0f;

            yield return null;
        }

        ui.SetActive(false);

        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator ShowInteractionSuccess()
    {
        if (interactionSuccessPanel == null) yield break;

        interactionSuccessPanel.SetActive(true);

        if (interactionSuccessText)
            interactionSuccessText.text = " Interaction Successful";

        yield return new WaitForSeconds(interactionFeedbackTime);

        interactionSuccessPanel.SetActive(false);
    }

    /* ========================= */

    void DisableAllControls()
    {
        moveLeftUI.SetActive(false);
        moveRightUI.SetActive(false);
        jumpUI.SetActive(false);
        interactUI.SetActive(false);
        hintUI.SetActive(false);
        backlogUI.SetActive(false);
        pauseUI.SetActive(false);
        enterUI.SetActive(false);
    }

    void EndTutorial()
    {
        dialoguePanel.SetActive(false);

        if (blockerCollider)
            blockerCollider.enabled = false;

        if (doorSystem)
            doorSystem.tutorialActive = false;
    }

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

    IEnumerator WaitForEnter()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
    }
}