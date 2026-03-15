using UnityEngine;
using TMPro;
using System.Collections;

public class ForLoopTerminal : MonoBehaviour
{
    public GameObject terminalUI;
    public TMP_Text terminalText;

    public GameObject[] steps;

    public float createDelay = 0.5f;
    public float disappearDelay = 5f;

    bool playerNear = false;
    bool terminalOpen = false;

    string playerInput = "";
    bool ignoreFirstKey = false;

    void Start()
    {
        terminalUI.SetActive(false);
        ResetSteps();
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E) && !terminalOpen)
        {
            OpenTerminal();
        }

        if (terminalOpen)
        {
            ReadKeyboard();
        }
    }

    void OpenTerminal()
    {
        terminalOpen = true;
        Time.timeScale = 0f;

        terminalUI.SetActive(true);

        playerInput = "";
        ignoreFirstKey = true;

        ResetSteps();

        int stepCount = steps.Length;

        terminalText.text =
        "This cliff seems to be " + stepCount + " steps long.\n\n" +
        "To activate steps use a for loop.\n\n" +
        "Example:\nfor i in range(1," + (stepCount + 1) + ",1)\n\n> ";
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

        terminalText.text =
        "This cliff seems to be " + steps.Length + " steps long.\n\n" +
        "To activate steps use a for loop.\n\n> " +"range(start, end, intervals)\n"+
        "Example:\nfor i in range(1," + (steps.Length + 1) + ",1)\n\n> "+
        playerInput+"\n{\n StepsGen();\n}";
    }

    void CheckCode()
    {
        if (!playerInput.Contains("range"))
        {
            terminalText.text += "\nInvalid code.";
            return;
        }

        int startBracket = playerInput.IndexOf("(");
        int endBracket = playerInput.IndexOf(")");

        if (startBracket == -1 || endBracket == -1)
            return;

        string inside = playerInput.Substring(startBracket + 1, endBracket - startBracket - 1);

        string[] nums = inside.Split(',');

        int start = int.Parse(nums[0]);
        int end = int.Parse(nums[1]);
        int interval = int.Parse(nums[2]);

        if (start < 1)
        {
            terminalText.text += "\nSorry digging is not possible.";
            return;
        }

        if (end > steps.Length + 1)
        {
            terminalText.text += "\nThe cliff only supports " + steps.Length + " steps.";
            return;
        }

        if (interval <= 0)
        {
            terminalText.text += "\nInterval must be greater than 0.";
            return;
        }

        CloseTerminal();

        StartCoroutine(CreateSteps(start, end, interval));
    }

    IEnumerator CreateSteps(int start, int end, int interval)
    {
        for (int i = start; i < end; i += interval)
        {
            GameObject step = steps[i - 1];

            step.SetActive(true);

            StartCoroutine(RemoveStepAfterTime(step));

            yield return new WaitForSeconds(createDelay);
        }
    }

    IEnumerator RemoveStepAfterTime(GameObject step)
    {
        yield return new WaitForSeconds(disappearDelay);

        if (step.activeSelf)
        {
            step.SetActive(false);
        }
    }

    void ResetSteps()
    {
        foreach (GameObject s in steps)
        {
            s.SetActive(false);
        }
    }

    void CloseTerminal()
    {
        terminalUI.SetActive(false);
        Time.timeScale = 1f;
        terminalOpen = false;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}