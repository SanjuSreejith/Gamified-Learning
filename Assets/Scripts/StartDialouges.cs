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
  
    TMPTypewriter typewriter;

    bool waitingForInput;


    IEnumerator Start()
    {
        yield return null;

        if (dialoguePanel == null)
        {
            Debug.LogError("Dialogue Panel not assigned!");
            yield break;
        }

        typewriter = dialogueText.GetComponent<TMPTypewriter>();

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

        if (typewriter != null)
            typewriter.Play(line);
        else
            dialogueText.text = line;

        // Wait for input
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                // First input → finish typing
                if (typewriter != null && typewriter.IsTyping())
                {
                    typewriter.Skip();
                }
                // Second input → continue
                else
                {
                    break;
                }
            }

            yield return null;
        }

        // Add to backlog AFTER line fully shown
        DialogueBacklogManager.Instance?.AddLine(speaker, line);

        yield return new WaitForSeconds(0.15f);
    }



}
