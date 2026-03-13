using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class CameraIntroManager : MonoBehaviour
{
    [Header("Camera")]
    public CinemachineCamera introCamera;
    public float startDelay = 3f;
    public float introDuration = 3f;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;

    public Sprite abelPortrait;
    public Sprite kuttanPortrait;

    TMPTypewriter typewriter;

    void Start()
    {
        if (dialogueText != null)
            typewriter = dialogueText.GetComponent<TMPTypewriter>();

        StartCoroutine(CameraSequence());
    }

    IEnumerator CameraSequence()
    {
        yield return new WaitForSeconds(startDelay);

        // Activate cinematic camera
        introCamera.Priority = 41;

        // Play intro dialogue
        yield return StartCoroutine(PlayDialogue());

        yield return new WaitForSeconds(introDuration);

        // Return to gameplay camera
        introCamera.Priority = 0;
    }

    IEnumerator PlayDialogue()
    {
        string[][] dialogue = new string[][]
        {
            new string[]{"Abel","Look around... these towers power the entire defense system."},
            new string[]{"Kuttan","Each one has an energy beacon at its base."},
            new string[]{"Abel","Find the glowing beacon of every tower."},
            new string[]{"Kuttan","Access the terminal there and configure the defense protocols."},
            new string[]{"Abel","Once every tower is configured, the system will protect this place."}
        };

        dialoguePanel.SetActive(true);

        foreach (var line in dialogue)
        {
            string speaker = line[0];
            string text = line[1];

            // UI
            speakerText.text = speaker;
            speakerImage.sprite = speaker == "Abel" ? abelPortrait : kuttanPortrait;

            // Add to backlog
            if (DialogueBacklogManager.Instance != null)
                DialogueBacklogManager.Instance.AddLine(speaker, text);

            if (typewriter != null)
                typewriter.Play(text);
            else
                dialogueText.text = text;

            bool waiting = true;

            while (waiting)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                {
                    if (typewriter != null && typewriter.IsTyping())
                    {
                        typewriter.Skip();
                    }
                    else
                    {
                        waiting = false;
                    }
                }

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }

        dialoguePanel.SetActive(false);
    }
}