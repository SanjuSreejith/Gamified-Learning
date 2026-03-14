using UnityEngine;
using UnityEngine.UI;

public class BannerEditor : MonoBehaviour
{
    [Header("Panel")]
    public GameObject bannerPanel;

    [Header("Banner Display")]
    public Image bannerDisplay;

    [Header("Available Banners")]
    public Sprite[] bannerSprites;

    [Header("Selection Ticks")]
    public GameObject[] selectionTicks;
    // One tick for each banner button

    private const string BANNER_KEY = "SelectedBanner";

    void Start()
    {
        LoadBanner();
    }

    // ================= PANEL =================

    public void OpenBannerPanel()
    {
        bannerPanel.SetActive(true);
    }

    public void CloseBannerPanel()
    {
        bannerPanel.SetActive(false);
    }

    // ================= CHANGE BANNER =================

    public void SetBanner(int index)
    {
        if (index < 0 || index >= bannerSprites.Length) return;

        bannerDisplay.sprite = bannerSprites[index];

        PlayerPrefs.SetInt(BANNER_KEY, index);
        PlayerPrefs.Save();

        UpdateSelectionTick(index);
    }

    // ================= TICK SYSTEM =================

    void UpdateSelectionTick(int selectedIndex)
    {
        for (int i = 0; i < selectionTicks.Length; i++)
        {
            selectionTicks[i].SetActive(i == selectedIndex);
        }
    }

    // ================= LOAD SAVED =================

    void LoadBanner()
    {
        int savedBanner = PlayerPrefs.GetInt(BANNER_KEY, 0);

        if (savedBanner >= 0 && savedBanner < bannerSprites.Length)
        {
            bannerDisplay.sprite = bannerSprites[savedBanner];
            UpdateSelectionTick(savedBanner);
        }
    }
}