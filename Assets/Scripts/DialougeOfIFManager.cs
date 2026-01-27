using UnityEngine;
using UnityEngine.UI;
using TMPro;


[RequireComponent(typeof(Collider2D))]
public class BridgeDialogueSequenceController : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;

    [Header("Portraits")]
    public Sprite abelPortrait;
    public Sprite kuttanPortrait;

    [Header("After Dialogue")]
    public GameObject terminalPanel;

    [Header("Dialogue (Auto-filled if empty)")]
    public DialogueLine[] lines;

    int index;
    bool active;
    bool waitingForInput;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        // 🔹 Auto-create default dialogues if none provided
        if (lines == null || lines.Length == 0)
        {
            lines = new DialogueLine[]
            {
            // KUTTAN — STRONG BELIEF
            new DialogueLine("Kuttan", "No… this shouldn’t be happening."),
            new DialogueLine("Kuttan", "NULL can’t enter this world."),
            new DialogueLine("Kuttan", "It never could."),
            new DialogueLine("Kuttan", "This place was isolated from it."),

            // ABEL — CALM, INFORMED
            new DialogueLine("Abel", "I know."),
            new DialogueLine("Abel", "NULL hasn’t entered."),

            // KUTTAN — CONFUSION
            new DialogueLine("Kuttan", "Then explain this."),
            new DialogueLine("Kuttan", "The enemies."),
            new DialogueLine("Kuttan", "The corruption."),

            // ABEL — TRUTH
            new DialogueLine("Abel", "NULL controls the Game Core."),
            new DialogueLine("Abel", "But this world runs on the Learning Core."),
            new DialogueLine("Abel", "And that’s the weakness."),

            // KUTTAN — REALIZATION
            new DialogueLine("Kuttan", "So it didn’t enter…"),
            new DialogueLine("Kuttan", "It interfered."),

            // ABEL — CONFIRMATION
            new DialogueLine("Abel", "Yes."),
            new DialogueLine("Abel", "By corrupting the Learning Core."),
            new DialogueLine("Abel", "By executing broken logic."),

            // KUTTAN — SHAKEN
            new DialogueLine("Kuttan", "I still believed this world was safe."),
            new DialogueLine("Kuttan", "That NULL couldn’t touch it."),

            // ABEL — DECISION
            new DialogueLine("Abel", "Belief won’t fix this."),
            new DialogueLine("Abel", "Arguing won’t either."),
            new DialogueLine("Abel", "We act."),

            // ABEL — PLAYER INSTRUCTION
            new DialogueLine("Abel", "Open the terminal."),
            new DialogueLine("Abel", "Observe the bridge."),
            new DialogueLine("Abel", "Look for the rule."),
            new DialogueLine("Abel", "This world still obeys logic.")
            };
        }

        // Assign portraits automatically
        foreach (var line in lines)
        {
            if (line.speaker == "Abel")
                line.portrait = abelPortrait;
            else if (line.speaker == "Kuttan")
                line.portrait = kuttanPortrait;
        }
    }


     
    void OnTriggerEnter2D(Collider2D other)
    {
        if (active) return;
        if (!other.CompareTag("Player")) return;

        active = true;
        dialoguePanel.SetActive(true);
        index = 0;
        ShowLine();
    }

    void Update()
    {
        if (!waitingForInput) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            NextLine();
        }
    }

    void ShowLine()
    {
        if (index >= lines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines[index];

        speakerText.text = line.speaker;
        dialogueText.text = line.text;
        speakerImage.sprite = line.portrait;

        waitingForInput = true;
    }

    void NextLine()
    {
        waitingForInput = false;
        index++;
        ShowLine();
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        if (terminalPanel != null)
            terminalPanel.SetActive(true);
    }
}
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 4)]
    public string text;
    public Sprite portrait;

    public DialogueLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
        this.portrait = null;
    }
}
