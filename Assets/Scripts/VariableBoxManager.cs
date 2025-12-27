using UnityEngine;
using TMPro;
using System.Collections;

public class VariableInteractionBox : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 2f;
    public LayerMask playerLayer;

    [Header("Visuals")]
    public MagicParticleEffect magicParticles; // Connect your magic particle effect here
    public SpriteRenderer boxSprite;
    public Color activeColor = Color.cyan;
    public int defaultSortingOrder = 0;
    public int activeSortingOrder = 2;

    [Header("Text On Box")]
    public TextMeshPro boxText;
    public float writeSpeed = 0.05f;

    [Header("UI")]
    public GameObject inputPanel;
    public TMP_InputField nameInput;
    public TMP_InputField valueInput;

    [Header("Variable Data")]
    public string variableName;
    public int variableValue;

    [Header("Magic Effects")]
    public bool useMagicEffects = true;
    public float magicEffectDuration = 1.5f;
    public bool hideParticlesAfterEffect = true;

    bool playerNear;
    bool boxActivated;
    bool nameSet;
    bool valueSet;
    int interactionStep = 0; // 0: inactive, 1: change layer, 2: name input, 3: value input
    int originalSortingOrder;

    void Start()
    {
        inputPanel.SetActive(false);

        // Store original sorting order
        originalSortingOrder = boxSprite.sortingOrder;

        // Initialize particles
        if (useMagicEffects && magicParticles != null)
        {
            // Particles are hidden by default, will show when magic happens
        }
        else
        {
            // Fallback to old particle system
            var oldParticles = GetComponent<ParticleSystem>();
            if (oldParticles != null)
            {
                oldParticles.Stop();
                var renderer = oldParticles.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        boxText.text = "";
    }

    void Update()
    {
        DetectPlayer();

        // E key cycles through interaction steps
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            HandleInteractionStep();
        }

        // ENTER = submit name
        if (interactionStep == 2 && !nameSet && Input.GetKeyDown(KeyCode.Return))
        {
            SetVariableName();
        }

        // ENTER = submit value
        else if (interactionStep == 3 && nameSet && !valueSet && Input.GetKeyDown(KeyCode.Return))
        {
            SetVariableValue();
        }

        // Update visual feedback
        UpdateVisualFeedback();
    }

    // ================= PLAYER CHECK =================
    void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            interactDistance,
            playerLayer
        );

        playerNear = hit != null;
    }

    // ================= HANDLE INTERACTION STEPS =================
    void HandleInteractionStep()
    {
        interactionStep++;

        switch (interactionStep)
        {
            case 1: // First E press - Change layer and show magic effect
                ChangeSortingLayerWithMagic();
                break;

            case 2: // Second E press - Show name input
                ShowNameInput();
                break;

            case 3: // Third E press - Show value input
                if (nameSet)
                {
                    ShowValueInput();
                }
                else
                {
                    interactionStep = 2; // Stay on name input until name is set
                }
                break;

            default:
                interactionStep = 0; // Reset
                ResetBox();
                break;
        }
    }

    // ================= STEP 1 : CHANGE SORTING LAYER WITH MAGIC =================
    void ChangeSortingLayerWithMagic()
    {
        // Play magical particle effect BEFORE changing layer
        if (useMagicEffects && magicParticles != null)
        {
            // Show and play the magic effect
            magicParticles.PlayMagicEffect();

            // Wait a moment before changing layer for dramatic effect
            StartCoroutine(ChangeLayerAfterDelay(0.3f));
        }
        else
        {
            // Immediate layer change without magic
            ChangeLayerImmediate();
        }

        boxSprite.color = activeColor;
        boxActivated = true;

        // No UI panel shown yet
        inputPanel.SetActive(false);
    }

    IEnumerator ChangeLayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Change sorting order in layer with magical flourish
        boxSprite.sortingOrder = activeSortingOrder;

        // Optional: Add a scale animation for more magical feel
        StartCoroutine(ScaleAnimation());
    }

    void ChangeLayerImmediate()
    {
        // Change sorting order in layer
        boxSprite.sortingOrder = activeSortingOrder;

        // Fallback to old particle system
        var oldParticles = GetComponent<ParticleSystem>();
        if (oldParticles != null)
        {
            var renderer = oldParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.enabled = true;

            oldParticles.Play();
            StartCoroutine(StopParticlesAfterDelay(magicEffectDuration));
        }
    }

    IEnumerator ScaleAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;

        float duration = 0.2f;
        float timer = 0f;

        // Scale up
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }

        // Scale back down
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // ================= STEP 2 : SHOW NAME INPUT =================
    void ShowNameInput()
    {
        inputPanel.SetActive(true);
        nameInput.gameObject.SetActive(true);
        valueInput.gameObject.SetActive(false);

        nameInput.text = "";
        nameInput.ActivateInputField();

        boxText.text = "Enter Variable Name";
    }

    // ================= STEP 3 : SET VARIABLE NAME =================
    void SetVariableName()
    {
        if (string.IsNullOrEmpty(nameInput.text))
            return;

        variableName = nameInput.text;
        nameSet = true;

        inputPanel.SetActive(false); // Close panel after name entry

        // Show name on box
        StopAllCoroutines();
        StartCoroutine(WriteNameAnimation(variableName));

        // Show instruction for next step
        StartCoroutine(ShowNextInstruction());

        // Optional: Play a small magical sparkle when name is set
        if (useMagicEffects && magicParticles != null)
        {
            StartCoroutine(PlayQuickSparkle());
        }
    }

    IEnumerator ShowNextInstruction()
    {
        yield return new WaitForSeconds(1f);
        boxText.text = "Press E to set value";
    }

    IEnumerator PlayQuickSparkle()
    {
        yield return new WaitForSeconds(0.5f);

        // Play a quick, small magical effect
        if (magicParticles != null)
        {
            // Store original settings
            int originalBurst = magicParticles.burstCount;
            float originalDuration = magicParticles.effectDuration;

            // Configure for quick sparkle
            magicParticles.burstCount = 20;
            magicParticles.effectDuration = 0.5f;

            // Play effect
            magicParticles.PlayMagicEffect();

            // Restore original settings
            yield return new WaitForSeconds(0.6f);
            magicParticles.burstCount = originalBurst;
            magicParticles.effectDuration = originalDuration;
        }
    }

    // ================= STEP 4 : SHOW VALUE INPUT =================
    void ShowValueInput()
    {
        inputPanel.SetActive(true);
        nameInput.gameObject.SetActive(false);
        valueInput.gameObject.SetActive(true);

        valueInput.text = "";
        valueInput.ActivateInputField();

        boxText.text = "Enter Value";
    }

    // ================= STEP 5 : SET VARIABLE VALUE =================
    void SetVariableValue()
    {
        if (string.IsNullOrEmpty(valueInput.text))
            return;

        int.TryParse(valueInput.text, out variableValue);
        valueSet = true;

        inputPanel.SetActive(false); // Close panel after value entry

        // Show final result with magical flourish
        StopAllCoroutines();
        StartCoroutine(ShowValueAnimation(variableValue));

        // Play completion magic effect
        StartCoroutine(PlayCompletionEffect());

        // Reset interaction for next time
        interactionStep = 0;
    }

    // ================= ANIMATIONS =================
    IEnumerator WriteNameAnimation(string text)
    {
        boxText.text = "";

        foreach (char c in text)
        {
            boxText.text += c;
            // Add a small magical sparkle for each character
            if (useMagicEffects && magicParticles != null && Random.value > 0.7f)
            {
                PlayCharacterSparkle();
            }
            yield return new WaitForSeconds(writeSpeed);
        }
    }

    void PlayCharacterSparkle()
    {
        // Create a quick sparkle at the text position
        if (magicParticles != null)
        {
            // Small effect for typing
            var originalBurst = magicParticles.burstCount;
            magicParticles.burstCount = 5;
            magicParticles.PlayMagicEffect();
            magicParticles.burstCount = originalBurst;
        }
    }

    IEnumerator ShowValueAnimation(int value)
    {
        string fullText = $"{variableName} = {value}";
        boxText.text = "";

        foreach (char c in fullText)
        {
            boxText.text += c;
            yield return new WaitForSeconds(writeSpeed * 0.8f);
        }
    }

    IEnumerator PlayCompletionEffect()
    {
        yield return new WaitForSeconds(0.5f);

        // Play a grand magical completion effect
        if (useMagicEffects && magicParticles != null)
        {
            // Configure for completion effect
            var originalBurst = magicParticles.burstCount;
            var originalSize = magicParticles.endSize;

            magicParticles.burstCount = 100;
            magicParticles.endSize = 2f;
            magicParticles.PlayMagicEffect();

            // Change box color briefly
            Color originalColor = boxSprite.color;
            boxSprite.color = Color.green;

            yield return new WaitForSeconds(0.5f);

            boxSprite.color = originalColor;

            // Restore original settings
            magicParticles.burstCount = originalBurst;
            magicParticles.endSize = originalSize;
        }
    }

    // ================= PARTICLE SYSTEM CONTROL =================
    IEnumerator StopParticlesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        var oldParticles = GetComponent<ParticleSystem>();
        if (oldParticles != null)
        {
            var emission = oldParticles.emission;
            float rate = emission.rateOverTime.constant;

            while (rate > 0)
            {
                rate -= Time.deltaTime * 10f;
                emission.rateOverTime = rate;
                yield return null;
            }

            oldParticles.Stop();

            // Hide renderer
            var renderer = oldParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.enabled = false;
        }
    }

    // ================= RESET BOX =================
    void ResetBox()
    {
        // Reset sorting order
        boxSprite.sortingOrder = originalSortingOrder;
        boxSprite.color = Color.white;
        boxActivated = false;
        nameSet = false;
        valueSet = false;
        inputPanel.SetActive(false);
        boxText.text = "";

        // Stop and hide particle effects
        if (useMagicEffects && magicParticles != null)
        {
            magicParticles.StopEffect();
            magicParticles.HideImmediate();
        }
        else
        {
            var oldParticles = GetComponent<ParticleSystem>();
            if (oldParticles != null)
            {
                oldParticles.Stop();
                var renderer = oldParticles.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        // Reset scale
        transform.localScale = Vector3.one;
    }

    // ================= VISUAL FEEDBACK =================
    void UpdateVisualFeedback()
    {
        // Pulse the box when player is near and not activated
        if (playerNear && !boxActivated)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
            boxSprite.color = Color.Lerp(Color.white, activeColor * 0.3f, pulse);
        }
        else if (!boxActivated)
        {
            boxSprite.color = Color.white;
        }
    }

    // ================= DEBUG =================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}