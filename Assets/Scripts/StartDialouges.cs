using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class IntroCasualDialogue : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;

    [Header("Portraits")]
    public Sprite abelPortrait;
    public Sprite kuttanPortrait;

    [Header("Typing")]
    public float typeSpeed = 0.035f;

    bool waitingForInput;
    bool isTyping;

    IEnumerator Start()
    {
        // 🔒 SAFETY: wait one frame so UI is ready
        yield return null;

        if (dialoguePanel == null)
        {
            Debug.LogError("Dialogue Panel not assigned!");
            yield break;
        }

        dialoguePanel.SetActive(true);
        dialogueText.text = "";
        speakerText.text = "";

        yield return StartCoroutine(DialogueSequence());
    }

    IEnumerator DialogueSequence()
    {
        yield return Speak("Kuttan", "Hey Abel. How are you?");
        yield return Speak("Abel", "Not good, Kuttan. Something is wrong.");
        yield return Speak("Kuttan", "Wrong how?");
        yield return Speak("Abel", "An entity called NULL is causing problems.");
        yield return Speak("Kuttan", "Is it just a system error?");
        yield return Speak("Abel", "No. It is learning from what we do.");

        dialoguePanel.SetActive(false);
    }

    IEnumerator Speak(string speaker, string line)
    {
        speakerText.text = speaker;
        speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;

        yield return StartCoroutine(TypeLine(line));

        waitingForInput = true;

        while (waitingForInput)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                waitingForInput = false;

            yield return null;
        }

        yield return new WaitForSeconds(0.15f);
    }

    IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";
        isTyping = true;

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }
}
