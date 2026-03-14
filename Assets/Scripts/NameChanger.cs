using UnityEngine;
using TMPro;

public class PlayerProfileManager : MonoBehaviour
{
    [Header("Display UI")]
    public TextMeshProUGUI nameDisplay;
    public TextMeshProUGUI ageDisplay;

    [Header("Edit Panel")]
    public GameObject editPanel;
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;

    const string NAME_KEY = "PlayerName";
    const string AGE_KEY = "PlayerAge";

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
    }

    void InitializePlayer()
    {
        // Give random name if first time
        if (!PlayerPrefs.HasKey(NAME_KEY))
        {
            string randomName = randomNames[Random.Range(0, randomNames.Length)];
            PlayerPrefs.SetString(NAME_KEY, randomName);
        }

        // Default age if first time
        if (!PlayerPrefs.HasKey(AGE_KEY))
        {
            PlayerPrefs.SetInt(AGE_KEY, 18);
        }

        PlayerPrefs.Save();
    }

    // Made this method public so it can be called from other scripts
    public void RefreshUI()
    {
        string playerName = PlayerPrefs.GetString(NAME_KEY);
        int age = PlayerPrefs.GetInt(AGE_KEY);

        nameDisplay.text = playerName;
        ageDisplay.text = "Age: " + age;
    }

    // OPEN EDIT PANEL
    public void OpenEditPanel()
    {
        editPanel.SetActive(true);

        nameInput.text = PlayerPrefs.GetString(NAME_KEY);
        ageInput.text = PlayerPrefs.GetInt(AGE_KEY).ToString();
    }

    // CLOSE PANEL
    public void CloseEditPanel()
    {
        editPanel.SetActive(false);
    }

    // SAVE PROFILE
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
        editPanel.SetActive(false);
    }
}