using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ForLoopTerminal : MonoBehaviour
{
    public GameObject terminalUI;
    public TMP_Text terminalText;
    public ScrollRect scrollRect;
    public GameObject[] steps;

    public float typingSpeed = 0.02f;
    public float createDelay = 0.5f;
    public float disappearDelay = 5f;

    bool playerNear;
    bool terminalOpen;
    bool terminalBusy;
    bool waitingAfterError;

    string playerInput = "";
    bool ignoreFirstKey;

    void Start()
    {
        terminalUI.SetActive(false);

        if (scrollRect == null)
            scrollRect = terminalUI.GetComponentInChildren<ScrollRect>();

        ResetSteps();
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E) && !terminalOpen)
        {
            OpenTerminal();
        }

        if (terminalOpen && !terminalBusy)
        {
            ReadKeyboard();
        }
    }

    void OpenTerminal()
    {
        terminalOpen = true;
        terminalBusy = true;

        Time.timeScale = 0f;

        terminalUI.SetActive(true);

        playerInput = "";
        ignoreFirstKey = true;
        waitingAfterError = false;

        terminalText.text = "";

        ResetSteps();

        StartCoroutine(PrintIntro());
    }

    IEnumerator PrintIntro()
    {
        int stepCount = steps.Length;

        yield return TypeText("This cliff seems to be " + stepCount + " steps long.\n\n");

        yield return TypeText("To activate the steps use a Python for loop.\n\n");

        yield return TypeText("Structure:\n");
        yield return TypeText("for i in range(start, end, step)\n\n");

        yield return TypeText("start → where the loop begins\n");
        yield return TypeText("end → stopping point (NOT included)\n");
        yield return TypeText("step → how much i increases each loop\n\n");

        yield return TypeText("Example:\n");
        yield return TypeText("for i in range(1," + (stepCount + 1) + ",1)\n\n");

        terminalText.text += "> ";
        AutoScroll();

        terminalBusy = false;
    }

    IEnumerator TypeText(string text)
    {
        foreach (char c in text)
        {
            terminalText.text += c;
            AutoScroll();
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
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

            if (waitingAfterError)
            {
                if (c == '\n' || c == '\r')
                {
                    OpenTerminal();
                }
                return;
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

        UpdateInputLine();
    }

    void UpdateInputLine()
    {
        int promptIndex = terminalText.text.LastIndexOf(">");

        if (promptIndex >= 0)
        {
            terminalText.text =
                terminalText.text.Substring(0, promptIndex + 1) + " " + playerInput;
        }

        AutoScroll();
    }

    void CheckCode()
    {
        if (!playerInput.Contains("range"))
        {
            StartCoroutine(ShowError("Invalid code."));
            return;
        }

        int a = playerInput.IndexOf("(");
        int b = playerInput.IndexOf(")");

        if (a == -1 || b == -1)
        {
            StartCoroutine(ShowError("Syntax error."));
            return;
        }

        string inside = playerInput.Substring(a + 1, b - a - 1);

        string[] nums = inside.Split(',');

        if (nums.Length < 3)
        {
            StartCoroutine(ShowError("Range requires start,end,step."));
            return;
        }

        int start = int.Parse(nums[0]);
        int end = int.Parse(nums[1]);
        int step = int.Parse(nums[2]);

        if (start < 1)
        {
            StartCoroutine(ShowError("Sorry digging is not possible."));
            return;
        }

        if (end > steps.Length + 1)
        {
            StartCoroutine(ShowError("The wall is not high enough."));
            return;
        }

        if (step <= 0)
        {
            StartCoroutine(ShowError("Interval must be greater than 0."));
            return;
        }

        CloseTerminal();

        StartCoroutine(CreateSteps(start, end, step));
    }

    IEnumerator ShowError(string message)
    {
        terminalBusy = true;

        terminalText.text += "\n\nERROR: " + message;
        AutoScroll();

        yield return new WaitForSecondsRealtime(1f);

        terminalText.text += "\nPress Enter to try again.";
        AutoScroll();

        waitingAfterError = true;
        terminalBusy = false;
    }

    IEnumerator CreateSteps(int start, int end, int step)
    {
        for (int i = start; i < end; i += step)
        {
            GameObject s = steps[i - 1];

            s.SetActive(true);

            StartCoroutine(RemoveStepAfterTime(s));

            yield return new WaitForSeconds(createDelay);
        }
    }

    IEnumerator RemoveStepAfterTime(GameObject step)
    {
        yield return new WaitForSeconds(disappearDelay);

        if (step.activeSelf)
            step.SetActive(false);
    }

    void ResetSteps()
    {
        foreach (GameObject s in steps)
            s.SetActive(false);
    }

    void CloseTerminal()
    {
        terminalUI.SetActive(false);
        Time.timeScale = 1f;
        terminalOpen = false;
    }

    void AutoScroll()
    {
        if (scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            playerNear = true;
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            playerNear = false;
    }
}