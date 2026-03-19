using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerProfileManager : MonoBehaviour
{
    [Header("Display UI (Profile Page)")]
    public TextMeshProUGUI nameDisplay;
    public TextMeshProUGUI ageDisplay;

    [Header("Menu Display")]
    public TextMeshProUGUI menuNameDisplay;

    [Header("Edit Panel")]
    public GameObject editPanel;
    public Animator editPanelAnimator;

    [Header("Input Fields")]
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;

    const string NAME_KEY = "PlayerName";
    const string AGE_KEY = "PlayerAge";

    bool panelOpen = false;

    [Header("Random Names")]
    public string[] randomNames =
    {
        "Nova","Pixel","Echo","Rex","Luna",
        "Kai","Orion","Blaze","Zara","Neo",
        "Atlas","Axel","Riven","Skye","Juno"
    };

    void Start()
    {
        InitializePlayer();
        RefreshUI();

        if (editPanel != null)
            editPanel.SetActive(false);
    }

    void Update()
    {
        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseEditPanel();
        }
    }

    void InitializePlayer()
    {
        if (!PlayerPrefs.HasKey(NAME_KEY))
        {
            string randomName = randomNames[Random.Range(0, randomNames.Length)];
            PlayerPrefs.SetString(NAME_KEY, randomName);
        }

        if (!PlayerPrefs.HasKey(AGE_KEY))
        {
            PlayerPrefs.SetInt(AGE_KEY, 18);
        }

        PlayerPrefs.Save();
    }

    // ================= REFRESH UI =================

    public void RefreshUI()
    {
        string playerName = PlayerPrefs.GetString(NAME_KEY);
        int age = PlayerPrefs.GetInt(AGE_KEY);

        if (nameDisplay != null)
            nameDisplay.text = playerName;

        if (ageDisplay != null)
            ageDisplay.text = "Age: " + age;

        if (menuNameDisplay != null)
            menuNameDisplay.text = playerName;
    }

    // ================= OPEN PANEL =================

    public void OpenEditPanel()
    {
        editPanel.SetActive(true);
        editPanelAnimator.SetTrigger("Open");

        nameInput.text = PlayerPrefs.GetString(NAME_KEY);
        ageInput.text = PlayerPrefs.GetInt(AGE_KEY).ToString();

        panelOpen = true;
    }

    // ================= CLOSE PANEL =================

    public void CloseEditPanel()
    {
        editPanelAnimator.SetTrigger("Close");
        StartCoroutine(HidePanelAfterAnim());
    }

    IEnumerator HidePanelAfterAnim()
    {
        yield return new WaitForSeconds(1f);
        editPanel.SetActive(false);
        panelOpen = false;
    }

    // ================= SAVE PROFILE =================

    public void SaveProfile()
    {
        string newName = nameInput.text.Trim();

        if (!string.IsNullOrEmpty(newName))
        {
            PlayerPrefs.SetString(NAME_KEY, newName);
        }

        int age;

        if (int.TryParse(ageInput.text, out age))
        {
            if (age >= 0 && age <= 100)
            {
                PlayerPrefs.SetInt(AGE_KEY, age);
            }
            else
            {
                Debug.Log("Age must be between 0 and 100.");
                return;
            }
        }

        PlayerPrefs.Save();

        RefreshUI();
        CloseEditPanel();
    }
}