using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueBacklogManager : MonoBehaviour
{
    public static DialogueBacklogManager Instance;

    [Header("UI")]
    public GameObject backlogPanel;
    public TextMeshProUGUI backlogText;

    [Header("Settings")]
    public int maxLines = 100;

    [Header("Color Options")]
    public bool useDifferentSpeakerColors = true;

    [Header("Common Colors (Used if toggle is OFF)")]
    public Color commonSpeakerColor = Color.white;
    public Color commonTextColor = new Color32(220, 220, 220, 255);

    [Header("Speaker Colors (Used if toggle is ON)")]
    public Color kuttanColor = new Color32(71, 123, 99, 255);
    public Color abelColor = new Color32(137, 196, 193, 255);
    public Color guideColor = new Color32(141, 191, 141, 255);
    public Color nullColor = new Color32(155, 123, 184, 255);

    List<string> dialogueHistory = new List<string>();
    bool isOpen;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        backlogPanel.SetActive(false);
    }

    void Update()
    {
        // 🔥 Works even when game is paused (timeScale = 0)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleBacklog();
        }
    }

    public void AddLine(string speaker, string text)
    {
        Color speakerCol = GetSpeakerColor(speaker);
        Color textCol = GetTextColor();

        string line =
            $"<b><color={ColorToHex(speakerCol)}>{speaker}:</color></b> " +
            $"<color={ColorToHex(textCol)}>{text}</color>";

        dialogueHistory.Add(line);

        if (dialogueHistory.Count > maxLines)
            dialogueHistory.RemoveAt(0);

        RefreshUI();
    }

    Color GetSpeakerColor(string speaker)
    {
        if (!useDifferentSpeakerColors)
            return commonSpeakerColor;

        switch (speaker)
        {
            case "Kuttan": return kuttanColor;
            case "Abel": return abelColor;
            case "Guide": return guideColor;
            case "NULL": return nullColor;
            default: return commonSpeakerColor;
        }
    }

    Color GetTextColor()
    {
        return commonTextColor;
    }

    string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }

    void RefreshUI()
    {
        backlogText.text = string.Join("\n\n", dialogueHistory);
        Canvas.ForceUpdateCanvases();
    }

    public void ToggleBacklog()
    {
        isOpen = !isOpen;
        backlogPanel.SetActive(isOpen);

        // ❌ DO NOT touch Time.timeScale here
        // Let other systems control pause
    }
}