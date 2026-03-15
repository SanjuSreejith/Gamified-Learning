using UnityEngine;
using TMPro;
using System.Collections;

public class AccessPanelWhile : MonoBehaviour
{
    public GameObject terminalUI;
    public TMP_Text terminalText;

    public Animator waterAnimator;

    bool playerNear;
    bool terminalOpen;
    bool terminalBusy;

    string playerInput = "";

    int WL = 1;

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
        terminalUI.SetActive(true);
        Time.timeScale = 0;
        waterAnimator.SetBool("WaterDown", false);
        terminalText.text =
        "Water Control Terminal\n\n" +
        "Drain the tank using a while loop.\n\n" +
        "Example:\n" +
        "while(WL<=5){drain();}\n\n> ";
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
        terminalText.text = terminalText.text.Substring(0, p + 1) + " " + playerInput;
    }

    void CheckWhileCode()
    {
        string code = playerInput.Replace(" ", "");

        if (!code.StartsWith("while"))
        {
            ShowError("Unknown keyword. Did you mean 'while'?");
            return;
        }

        if (!code.Contains("WL<="))
        {
            ShowError("Condition must use WL<=number");
            return;
        }

        if (!code.Contains("{") || !code.Contains("}"))
        {
            ShowError("Missing { } block");
            return;
        }

        if (!code.Contains("drain();"))
        {
            ShowError("Missing drain(); command");
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

        if (max > 10)
        {
            ShowError("Water tank too large to drain");
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

            yield return new WaitForSeconds(1f);

            WL++;
        }

        waterAnimator.SetBool("WaterDown", false);
    }

    void ShowError(string msg)
    {
        terminalText.text += "\nERROR: " + msg + "\nPress Enter to try again.";
        playerInput = "";
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
