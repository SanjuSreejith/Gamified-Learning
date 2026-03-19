using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProfilePictureManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject changeProfilePanel;
    public Animator panelAnimator;

    bool panelOpen = false;

    [Header("Profile Display (Profile Page)")]
    public Image profileDisplay;

    [Header("Menu Profile Images")]
    public Image menuProfileDisplay;      // Final menu image
    public Image menuProfileDummyImage;   // Dummy animation image
    public Animator menuProfileAnimator;

    [Header("Default Profiles")]
    public Sprite[] defaultProfiles;

    [Header("Custom Profile Slots")]
    public GameObject[] customBorders;
    public Button[] customButtons;

    [Header("Selection Ticks")]
    public GameObject[] selectionTicks;

    const string PROFILE_KEY = "SelectedProfile";
    const string CUSTOM1_PATH = "CustomProfile1";
    const string CUSTOM2_PATH = "CustomProfile2";
    const string NEXT_SLOT_KEY = "NextUploadSlot";

    int selectedProfile;

    void Start()
    {
        changeProfilePanel.SetActive(false);

        selectedProfile = PlayerPrefs.GetInt(PROFILE_KEY, 0);

        LoadSlot(0, CUSTOM1_PATH);
        LoadSlot(1, CUSTOM2_PATH);

        // Load profile WITHOUT animation
        if (selectedProfile < 100)
        {
            SetProfileSpriteInstant(defaultProfiles[selectedProfile]);
            UpdateSelectionTick(selectedProfile);
        }
        else
        {
            int slot = selectedProfile - 100;
            SetProfileSpriteInstant(customButtons[slot].image.sprite);
            UpdateSelectionTick(defaultProfiles.Length + slot);
        }
    }

    void Update()
    {
        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    // ================= PANEL =================

    public void OpenPanel()
    {
        changeProfilePanel.SetActive(true);
        panelAnimator.SetTrigger("Open");
        panelOpen = true;
    }

    public void ClosePanel()
    {
        panelAnimator.SetTrigger("Close");
        StartCoroutine(HidePanelAfterAnim());
    }

    IEnumerator HidePanelAfterAnim()
    {
        yield return new WaitForSeconds(0.35f);
        changeProfilePanel.SetActive(false);
        panelOpen = false;
    }

    // ================= PROFILE SETTER =================

    void SetProfileSpriteInstant(Sprite sprite)
    {
        if (profileDisplay != null)
            profileDisplay.sprite = sprite;

        if (menuProfileDisplay != null)
            menuProfileDisplay.sprite = sprite;

        if (menuProfileDummyImage != null)
            menuProfileDummyImage.sprite = sprite;
    }

    void SetProfileSpriteAnimated(Sprite sprite)
    {
        // Update profile page instantly
        if (profileDisplay != null)
            profileDisplay.sprite = sprite;

        StartCoroutine(MenuProfileChange(sprite));
    }
    IEnumerator AnimateMenuProfileChange(Sprite sprite)
    {
        // Set dummy sprite first
        if (menuProfileDummyImage != null)
            menuProfileDummyImage.sprite = sprite;

        yield return new WaitForSeconds(0.55f);

        // Trigger animation
        if (menuProfileAnimator != null)
            menuProfileAnimator.SetTrigger("Change");

        // Apply to real image
        if (menuProfileDisplay != null)
            menuProfileDisplay.sprite = sprite;
    }
    IEnumerator MenuProfileChange(Sprite sprite)
    {
        // Step 1: Apply sprite to dummy image
        if (menuProfileDummyImage != null)
            menuProfileDummyImage.sprite = sprite;

        // Step 2: Trigger animation immediately
        if (menuProfileAnimator != null)
            menuProfileAnimator.SetTrigger("Change");

        // Step 3: Wait for animation reveal
        yield return new WaitForSeconds(0.55f);

        // Step 4: Apply sprite to real menu profile
        if (menuProfileDisplay != null)
            menuProfileDisplay.sprite = sprite;
    }
    // ================= DEFAULT PROFILE =================

    public void SetDefaultProfile(int index)
    {
        SetProfileSpriteAnimated(defaultProfiles[index]);

        PlayerPrefs.SetInt(PROFILE_KEY, index);
        PlayerPrefs.Save();

        UpdateSelectionTick(index);
    }

    // ================= CUSTOM PROFILE =================

    public void SetCustomProfile(int slot)
    {
        Sprite sprite = customButtons[slot].image.sprite;

        SetProfileSpriteAnimated(sprite);

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
            SetProfileSpriteAnimated(sprite);
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
}