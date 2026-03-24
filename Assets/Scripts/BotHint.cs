using UnityEngine;
using TMPro;
using System.Collections;

public class BotHintSystem : MonoBehaviour
{
    [Header("Hint UI")]
    public TextMeshProUGUI hintText;
    public Animator hintAnimator;

    [Header("Input")]
    public KeyCode hintKey = KeyCode.H;

    [Header("Timing")]
    public float hintVisibleTime = 4f;
    public float cooldownTime = 6f;
    public float refillMessageTime = 2f;

    [Header("Cooldown Message")]
    public string refillMessage = "Hint system recharging...";

    string[] hints;
    int hintIndex = 0;

    bool hintsEnabled = false;
    bool hintActive = false;
    bool cooldownActive = false;

    Coroutine hintRoutine;

    void Update()
    {
        if (!hintsEnabled) return;

        if (Input.GetKeyDown(hintKey))
            TryShowHint();
    }

    public void SetHints(string[] newHints)
    {
        hints = newHints;
        hintIndex = 0;
    }

    void TryShowHint()
    {
        if (hintActive) return;

        if (cooldownActive)
        {
            ShowRefillMessage();
            return;
        }

        ShowHint();
    }

    void ShowHint()
    {
        if (hints == null || hints.Length == 0) return;

        hintActive = true;
        cooldownActive = true;

        hintText.text = hints[hintIndex];

        hintAnimator.ResetTrigger("Close");
        hintAnimator.SetTrigger("Open");

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        hintRoutine = StartCoroutine(HintRoutine(hintVisibleTime));

        hintIndex = (hintIndex + 1) % hints.Length;

        StartCoroutine(CooldownRoutine());
    }

    void ShowRefillMessage()
    {
        hintText.text = refillMessage;

        hintAnimator.SetTrigger("Open");

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        hintRoutine = StartCoroutine(HintRoutine(refillMessageTime));
    }

    IEnumerator HintRoutine(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        hintAnimator.SetTrigger("Close");

        hintActive = false;
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSecondsRealtime(cooldownTime);
        cooldownActive = false;
    }

    public void EnableHints()
    {
        hintsEnabled = true;
    }

    public void DisableHints()
    {
        hintsEnabled = false;
    }
}