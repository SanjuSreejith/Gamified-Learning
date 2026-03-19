using System.Collections;
using UnityEngine;

public class ProfilePanelController : MonoBehaviour
{
    [Header("Profile Panel")]
    public GameObject profilePanel;

    [Header("Animation")]
    public Animator profileAnimator;

    bool panelOpen = false;

    void Start()
    {
        if (profilePanel != null)
            profilePanel.SetActive(false);
    }

    void Update()
    {
        // Close with ESC
        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseProfile();
        }
    }

    // OPEN PROFILE PANEL
    public void OpenProfile()
    {
        profilePanel.SetActive(true);
        profileAnimator.SetTrigger("Open");
        panelOpen = true;
    }

    // CLOSE PROFILE PANEL
    public void CloseProfile()
    {
        profileAnimator.SetTrigger("Close");
        StartCoroutine(HideProfileAfterAnim());
    }

    IEnumerator HideProfileAfterAnim()
    {
        yield return new WaitForSeconds(1f); // match animation length
        profilePanel.SetActive(false);
        panelOpen = false;
    }

    // TOGGLE PROFILE PANEL
    public void ToggleProfile()
    {
        if (panelOpen)
            CloseProfile();
        else
            OpenProfile();
    }
}