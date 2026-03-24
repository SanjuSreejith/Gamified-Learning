using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TypeSpeedController : MonoBehaviour
{
    [Header("UI")]
    public Slider speedSlider;
    public TextMeshProUGUI speedText;

    [Header("Typewriters (Optional)")]
    public TMPTypewriter[] typewriters;

    [Header("Speed Label Typewriter")]
    public TMPTypewriter speedTextTypewriter; // 👈 assign this
    string lastSpeedLabel = "";

    private const string PREF_KEY = "TypeSpeed";

    private const float MIN = 0.001f;
    private const float MAX = 0.06f;
    private const float DEFAULT = 0.035f;

    void Start()
    {
        // Setup slider
        speedSlider.minValue = MIN;
        speedSlider.maxValue = MAX;

        float saved = PlayerPrefs.GetFloat(PREF_KEY, DEFAULT);
        speedSlider.value = saved;

        // Auto get typewriter for label if not assigned
        if (speedTextTypewriter == null && speedText != null)
        {
            speedTextTypewriter = speedText.GetComponent<TMPTypewriter>();

            if (speedTextTypewriter == null)
            {
                speedTextTypewriter = speedText.gameObject.AddComponent<TMPTypewriter>();
            }
        }

        // 🔥 IMPORTANT: force first update
        lastSpeedLabel = "";

        // Apply + show
        ApplySpeed(saved);
        UpdateText(saved);

        speedSlider.onValueChanged.AddListener(OnSliderChanged);

        // Auto find dialogue typewriters
        if (typewriters == null || typewriters.Length == 0)
        {
            typewriters = FindObjectsOfType<TMPTypewriter>();
        }
    }
    public void OnSliderChanged(float value)
    {
        // Save
        PlayerPrefs.SetFloat(PREF_KEY, value);
        PlayerPrefs.Save();

        // Apply speed
        ApplySpeed(value);

        // Update UI with typing effect
        UpdateText(value);
    }

    void ApplySpeed(float value)
    {
        foreach (var t in typewriters)
        {
            if (t != null)
                t.letterDelay = value;
        }
    }

    void UpdateText(float value)
    {
        string newLabel = GetSpeedName(value);

        // ❌ If same category → no typing, just update instantly
        if (newLabel == lastSpeedLabel)
        {
            speedText.text = newLabel;
            return;
        }

        // ✅ Category changed → play typing effect
        lastSpeedLabel = newLabel;

        if (speedTextTypewriter != null)
        {
            if (speedTextTypewriter.IsTyping())
                speedTextTypewriter.Skip();

            speedTextTypewriter.letterDelay = 0.01f; // UI fast typing
            speedTextTypewriter.Play(newLabel);
        }
        else
        {
            speedText.text = newLabel;
        }
    }

    string GetSpeedName(float value)
    {
        if (value >= 0.05f) return "Slow";
        if (value >= 0.03f) return "Medium";
        if (value >= 0.015f) return "Fast";
        return "Instant";
    }
}