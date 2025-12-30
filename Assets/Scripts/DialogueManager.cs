using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public string[] lines;
    public float textSpeed = 0.04f;

    int index;
    bool isTyping;

    void Start()
    {
        dialogueText.text = "";
        StartDialogue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = lines[index];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    void StartDialogue()
    {
        index = 0;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in lines[index])
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            StartCoroutine(TypeLine());
        }
        else
        {
            Debug.Log("Intro Finished");
            // Load next scene here
        }
    }
}
