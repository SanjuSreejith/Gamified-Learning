using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject panel;
    public Image displayImage;

    public Sprite[] images;   // Put 4 images in Inspector

    void Start()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && panel.activeSelf)
        {
            panel.SetActive(false);
        }
    }

    public void OpenPanel()
    {
        panel.SetActive(true);
    }

    public void ChangeImage(int index)
    {
        if (index >= 0 && index < images.Length)
        {
            displayImage.sprite = images[index];
        }
    }
}