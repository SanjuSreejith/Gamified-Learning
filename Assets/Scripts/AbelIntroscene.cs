using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AbelIntroNPC : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Typing")]
    public float typeSpeed = 0.035f;

    [Header("Jetpack UI (ONLY UI, NO CONTROLLER)")]
    public CanvasGroup jetpackUI;
    public float jetpackFadeSpeed = 2f;

    [Header("Scene Transition")]
    public CanvasGroup fadeCanvas;
    public float fadeSpeed = 1.5f;
    public string nextSceneName = "OutsideWorld";

    [Header("Player Performance")]
    public OverallPerformance playerPerformance;

    bool waitingForContinue;

    public enum OverallPerformance
    {
        Master,
        Competent,
        Novice,
        Struggling
    }

    void Start()
    {
        dialoguePanel.SetActive(false);

        if (jetpackUI != null)
        {
            jetpackUI.alpha = 0;
            jetpackUI.gameObject.SetActive(false);
        }

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0;
            fadeCanvas.gameObject.SetActive(false);
        }
    }

    public void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        speakerText.text = "???";
        StartCoroutine(DialogueSequence());
    }

    IEnumerator DialogueSequence()
    {
        yield return Speak("…So, you made it.");

        speakerText.text = "Abel";

        yield return Speak("My name is Abel.");
        yield return Speak("I observe worlds when they begin to fracture.");

        foreach (string line in GetPerformanceDialogue())
            yield return Speak(line);

        yield return Speak(
            "NULL is attacking this world.\n" +
            "It doesn’t destroy.\n" +
            "It erases."
        );

        yield return Speak(
            "Rules vanish.\n" +
            "Logic collapses.\n" +
            "Only fragments remain."
        );

        yield return Speak(
            "You won’t face NULL here.\n" +
            "But soon… you will."
        );

        yield return Speak(
            "I’m granting you access.\n" +
            "Not now.\n" +
            "Later."
        );

        yield return ShowJetpackUI();

        yield return Speak(
            "This will let you move beyond its reach.\n" +
            "When the time comes."
        );

        yield return Speak("Come.");
        yield return Speak("Let’s go out.");

        yield return FadeAndLoad();
    }

    string[] GetPerformanceDialogue()
    {
        switch (playerPerformance)
        {
            case OverallPerformance.Master:
                return new string[]
                {
                    "You didn’t just survive the house.",
                    "You understood it."
                };

            case OverallPerformance.Competent:
                return new string[]
                {
                    "You pushed through uncertainty.",
                    "That matters."
                };

            case OverallPerformance.Novice:
                return new string[]
                {
                    "You hesitated.",
                    "But you never stopped."
                };

            case OverallPerformance.Struggling:
                return new string[]
                {
                    "You struggled.",
                    "Yet NULL failed to break you."
                };
        }
        return new string[0];
    }

    IEnumerator ShowJetpackUI()
    {
        if (jetpackUI == null) yield break;

        jetpackUI.gameObject.SetActive(true);

        float t = 0;
        while (t < 1)
        {
            jetpackUI.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * jetpackFadeSpeed;
            yield return null;
        }

        jetpackUI.alpha = 1;
    }

    IEnumerator Speak(string line)
    {
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        dialogueText.text += "\n\n[Press Enter]";
        waitingForContinue = true;

        while (waitingForContinue)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                waitingForContinue = false;

            yield return null;
        }
    }

    IEnumerator FadeAndLoad()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            float t = 0;
            while (t < 1)
            {
                fadeCanvas.alpha = Mathf.Lerp(0, 1, t);
                t += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            fadeCanvas.alpha = 1;
        }

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(nextSceneName);
    }
}
