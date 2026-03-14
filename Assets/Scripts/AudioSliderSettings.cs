using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderController : MonoBehaviour
{
    public Slider volumeSlider;

    void Start()
    {
        float savedVolume = VolumeManager.Instance.GetVolume();

        volumeSlider.value = savedVolume;

        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    void ChangeVolume(float value)
    {
        VolumeManager.Instance.SetVolume(value);
    }
}