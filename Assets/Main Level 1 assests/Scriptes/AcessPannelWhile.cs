using UnityEngine;
using TMPro;
using System.Collections;

public class AccessPanelWhile : MonoBehaviour
{
    public GameObject terminalUI;
    public TMP_Text terminalText;
    public Animator waterAnimator;

    [Header("Error Settings")]
    public float errorDisplayTime = 3f;

    bool playerNear;
    bool terminalOpen;

    string playerInput = "";

    int WL = 1;

    string lastErrorLine = "";
    bool errorShowing = false;

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
        terminalUI.SetActive(true);
        Time.timeScale = 0;

        waterAnimator.SetBool("WaterDown", false);

        playerInput = "";

        terminalText.text =
        "Water Control Terminal\n\n" +
        "Drain the tank using a while loop.\n\n" +
        "WL = Water Level and it's safe to decrease the water level up to 5\n\n" +
        "Example:\n" +
        "while(WL<=2)\n\n> ";
    }

    void ReadKeyboard()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && playerInput.Length > 0)
            {
                playerInput = playerInput.Substring(0, playerInput.Length - 1);
            }
            else if (c == '\n' || c == '\r')
            {
                CheckWhileCode();
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
        int p = terminalText.text.LastIndexOf(">");

        if (p >= 0)
        {
            terminalText.text = terminalText.text.Substring(0, p + 1) + " " + playerInput;
        }
    }

    void CheckWhileCode()
    {
        string code = playerInput.Replace(" ", "");

        if (!code.StartsWith("while"))
        {
            ShowError("Unknown keyword. Did you mean 'while'?");
            return;
        }

        if (!code.Contains("(") || !code.Contains(")"))
        {
            ShowError("Missing parentheses ()");
            return;
        }

        if (!code.Contains("WL<="))
        {
            ShowError("Condition must use WL<=number");
            return;
        }

        int start = code.IndexOf("<=") + 2;
        int end = code.IndexOf(")");

        string num = code.Substring(start, end - start);

        int max;

        if (!int.TryParse(num, out max))
        {
            ShowError("Invalid number in condition");
            return;
        }

        if (max <= 0)
        {
            ShowError("WL must be greater than 0");
            return;
        }

        if (max < 5)
        {
            ShowError("Sorry, water level is too much for you to cross safely. Only 5 is correct.");
            return;
        }

        if (max > 5)
        {
            ShowError("Water level decreased too much. Tank safety exceeded.");
            return;
        }

        CloseTerminal();
        StartCoroutine(DrainWater(max));
    }

    IEnumerator DrainWater(int max)
    {
        WL = 1;

        waterAnimator.SetBool("WaterDown", true);

        while (WL <= max)
        {
            Debug.Log("Drain cycle " + WL);

            yield return new WaitForSecondsRealtime(1f);

            WL++;
        }
    }

    void ShowError(string msg)
    {
        if (errorShowing) return;

        lastErrorLine = "\n<color=red>ERROR: " + msg + "</color>";

        terminalText.text += lastErrorLine;

        StartCoroutine(RemoveError());
    }

    IEnumerator RemoveError()
    {
        errorShowing = true;

        yield return new WaitForSecondsRealtime(errorDisplayTime);

        terminalText.text = terminalText.text.Replace(lastErrorLine, "");

        lastErrorLine = "";
        errorShowing = false;
    }

    void CloseTerminal()
    {
        terminalUI.SetActive(false);
        Time.timeScale = 1;
        terminalOpen = false;
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