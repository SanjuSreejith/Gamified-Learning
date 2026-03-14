using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProfilePictureManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject changeProfilePanel;

    [Header("Profile Display")]
    public Image profileDisplay;

    [Header("Default Profiles")]
    public Sprite[] defaultProfiles;

    [Header("Custom Profile Slots")]
    public GameObject[] customBorders;
    public Button[] customButtons;

    [Header("Selection Ticks")]
    public GameObject[] selectionTicks;
    // Order: Default1, Default2, Default3... Custom1, Custom2

    const string PROFILE_KEY = "SelectedProfile";
    const string CUSTOM1_PATH = "CustomProfile1";
    const string CUSTOM2_PATH = "CustomProfile2";
    const string NEXT_SLOT_KEY = "NextUploadSlot";

    int selectedProfile;

    void Start()
    {
        selectedProfile = PlayerPrefs.GetInt(PROFILE_KEY, 0);

        LoadSlot(0, CUSTOM1_PATH);
        LoadSlot(1, CUSTOM2_PATH);

        if (selectedProfile < 100)
        {
            profileDisplay.sprite = defaultProfiles[selectedProfile];
            UpdateSelectionTick(selectedProfile);
        }
        else
        {
            int slot = selectedProfile - 100;
            UpdateSelectionTick(defaultProfiles.Length + slot);
        }
    }

    public void OpenPanel()
    {
        changeProfilePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        changeProfilePanel.SetActive(false);
    }

    // ================= DEFAULT PROFILE =================

    public void SetDefaultProfile(int index)
    {
        profileDisplay.sprite = defaultProfiles[index];

        PlayerPrefs.SetInt(PROFILE_KEY, index);
        PlayerPrefs.Save();

        UpdateSelectionTick(index);
    }

    // ================= CUSTOM PROFILE =================

    public void SetCustomProfile(int slot)
    {
        profileDisplay.sprite = customButtons[slot].image.sprite;

        PlayerPrefs.SetInt(PROFILE_KEY, 100 + slot);
        PlayerPrefs.Save();

        UpdateSelectionTick(defaultProfiles.Length + slot);
    }

    // ================= TICK SYSTEM =================

    void UpdateSelectionTick(int selectedIndex)
    {
        for (int i = 0; i < selectionTicks.Length; i++)
        {
            selectionTicks[i].SetActive(i == selectedIndex);
        }
    }

    // ================= UPLOAD IMAGE =================

    public void UploadImage()
    {
#if UNITY_ANDROID
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                int slot = PlayerPrefs.GetInt(NEXT_SLOT_KEY, 0);

                StartCoroutine(LoadImage(path, slot, true));

                slot = (slot + 1) % 2;
                PlayerPrefs.SetInt(NEXT_SLOT_KEY, slot);
                PlayerPrefs.Save();
            }
        }, "Select Profile Image");
#endif
    }

    IEnumerator LoadImage(string path, int slot, bool setAsProfile)
    {
        WWW www = new WWW("file://" + path);
        yield return www;

        Texture2D texture = www.texture;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        customBorders[slot].SetActive(true);
        customButtons[slot].image.sprite = sprite;

        if (setAsProfile || selectedProfile == 100 + slot)
        {
            profileDisplay.sprite = sprite;
            UpdateSelectionTick(defaultProfiles.Length + slot);
        }

        if (slot == 0)
            PlayerPrefs.SetString(CUSTOM1_PATH, path);
        else
            PlayerPrefs.SetString(CUSTOM2_PATH, path);

        if (setAsProfile)
            PlayerPrefs.SetInt(PROFILE_KEY, 100 + slot);

        PlayerPrefs.Save();
    }

    void LoadSlot(int slot, string key)
    {
        string path = PlayerPrefs.GetString(key, "");

        if (path != "")
        {
            StartCoroutine(LoadImage(path, slot, false));
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Clear Custom Images (Editor Test)")]
    public void ClearCustomImages()
    {
        PlayerPrefs.DeleteKey(CUSTOM1_PATH);
        PlayerPrefs.DeleteKey(CUSTOM2_PATH);
        PlayerPrefs.DeleteKey(PROFILE_KEY);
        PlayerPrefs.DeleteKey(NEXT_SLOT_KEY);

        customBorders[0].SetActive(false);
        customBorders[1].SetActive(false);

        foreach (var tick in selectionTicks)
            tick.SetActive(false);

        Debug.Log("Custom images cleared.");
    }
#endif
}