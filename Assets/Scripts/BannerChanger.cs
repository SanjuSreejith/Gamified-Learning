using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BannerEditor : MonoBehaviour
{
    [Header("Panel")]
    public GameObject bannerPanel;

    [Header("Animation")]
    public Animator bannerAnimator;
    bool panelOpen = false;

    [Header("Banner Display (Editor Panel)")]
    public Image bannerDisplay;

    [Header("Banner Display (Menu)")]
    public Image menuBannerDisplay;

    [Header("Available Banners")]
    public Sprite[] bannerSprites;

    [Header("Selection Ticks")]
    public GameObject[] selectionTicks;

    private const string BANNER_KEY = "SelectedBanner";

    void Start()
    {
        LoadBanner();

        if (bannerPanel != null)
            bannerPanel.SetActive(false);
    }

    void Update()
    {
        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseBannerPanel();
        }
    }

    // ================= PANEL =================

    public void OpenBannerPanel()
    {
        bannerPanel.SetActive(true);
        bannerAnimator.SetTrigger("Open");
        panelOpen = true;
    }

    public void CloseBannerPanel()
    {
        bannerAnimator.SetTrigger("Close");
        StartCoroutine(HideBannerAfterAnim());
    }

    IEnumerator HideBannerAfterAnim()
    {
        yield return new WaitForSeconds(1f); // match animation length
        bannerPanel.SetActive(false);
        panelOpen = false;
    }

    // ================= CHANGE BANNER =================

    public void SetBanner(int index)
    {
        if (index < 0 || index >= bannerSprites.Length) return;

        Sprite selected = bannerSprites[index];

        // Update editor preview
        if (bannerDisplay != null)
            bannerDisplay.sprite = selected;

        // Update menu banner
        if (menuBannerDisplay != null)
            menuBannerDisplay.sprite = selected;

        PlayerPrefs.SetInt(BANNER_KEY, index);
        PlayerPrefs.Save();

        UpdateSelectionTick(index);
    }

    // ================= TICK SYSTEM =================

    void UpdateSelectionTick(int selectedIndex)
    {
        for (int i = 0; i < selectionTicks.Length; i++)
        {
            if (selectionTicks[i] != null)
                selectionTicks[i].SetActive(i == selectedIndex);
        }
    }

    // ================= LOAD SAVED =================

    void LoadBanner()
    {
        int savedBanner = PlayerPrefs.GetInt(BANNER_KEY, 0);

        if (savedBanner >= 0 && savedBanner < bannerSprites.Length)
        {
            Sprite saved = bannerSprites[savedBanner];

            if (bannerDisplay != null)
                bannerDisplay.sprite = saved;

            if (menuBannerDisplay != null)
                menuBannerDisplay.sprite = saved;

            UpdateSelectionTick(savedBanner);
        }
    }
}