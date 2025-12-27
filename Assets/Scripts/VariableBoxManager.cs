using UnityEngine;
using TMPro;
using System.Collections;

public class VariableInteractionBox : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 2f;
    public LayerMask playerLayer;

    [Header("Visuals")]
    public ParticleSystem variableParticles;
    public SpriteRenderer boxSprite;
    public Color activeColor = Color.cyan;

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

    bool playerNear;
    bool boxActivated;
    bool nameSet;
    bool valueSet;
    int interactionStep = 0; // 0: inactive, 1: change layer, 2: name input, 3: value input

    void Start()
    {
        inputPanel.SetActive(false);
        variableParticles.Stop();
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
            case 1: // First E press - Change layer and show particles
                ChangeSortingLayer();
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

    // ================= STEP 1 : CHANGE SORTING LAYER =================
    void ChangeSortingLayer()
    {
        // Change sorting order in layer
        boxSprite.sortingOrder = 2;

        // Activate particles
        variableParticles.Play();
        StartCoroutine(StopParticlesAfterDelay(1.5f)); // Hide particles after delay

        boxSprite.color = activeColor;
        boxActivated = true;

        // No UI panel shown yet
        inputPanel.SetActive(false);
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
    }

    IEnumerator ShowNextInstruction()
    {
        yield return new WaitForSeconds(1f);
        boxText.text = "Press E to set value";
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

        // Show final result
        StopAllCoroutines();
        StartCoroutine(ShowValueAnimation(variableValue));

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
            yield return new WaitForSeconds(writeSpeed);
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

    // ================= PARTICLE SYSTEM CONTROL =================
    IEnumerator StopParticlesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        var emission = variableParticles.emission;
        float rate = emission.rateOverTime.constant;

        while (rate > 0)
        {
            rate -= Time.deltaTime * 10f;
            emission.rateOverTime = rate;
            yield return null;
        }

        variableParticles.Stop();
    }

    // ================= RESET BOX =================
    void ResetBox()
    {
        boxSprite.sortingOrder = 0;
        boxSprite.color = Color.white;
        boxActivated = false;
        nameSet = false;
        valueSet = false;
        inputPanel.SetActive(false);
        boxText.text = "";
        variableParticles.Stop();
    }

    // ================= DEBUG =================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}