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

    void Start()
    {
        inputPanel.SetActive(false);
        variableParticles.Stop();
        boxText.text = "";
    }

    void Update()
    {
        DetectPlayer();

        // E is only for interaction switching
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (!boxActivated)
                ActivateBox();
        }

        // ENTER = submit name
        if (boxActivated && !nameSet && Input.GetKeyDown(KeyCode.Return))
        {
            SetVariableName();
        }

        // ENTER = submit value
        else if (nameSet && !valueSet && Input.GetKeyDown(KeyCode.Return))
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

    // ================= STEP 1 : ACTIVATE =================
    void ActivateBox()
    {
        boxActivated = true;

        // 🔥 Change layer to 2
        gameObject.layer = 2;

        variableParticles.Play();
        StartCoroutine(StopParticlesSlowly());

        boxSprite.color = activeColor;

        inputPanel.SetActive(true);
        nameInput.gameObject.SetActive(true);
        valueInput.gameObject.SetActive(false);

        nameInput.ActivateInputField();
    }

    // ================= STEP 2 : NAME =================
    void SetVariableName()
    {
        if (string.IsNullOrEmpty(nameInput.text))
            return;

        variableName = nameInput.text;
        nameSet = true;

        nameInput.gameObject.SetActive(false);
        valueInput.gameObject.SetActive(true);
        valueInput.ActivateInputField();

        StopAllCoroutines();
        StartCoroutine(WriteNameAnimation(variableName));
    }

    IEnumerator WriteNameAnimation(string text)
    {
        boxText.text = "";

        foreach (char c in text)
        {
            boxText.text += c;
            yield return new WaitForSeconds(writeSpeed);
        }
    }

    // ================= STEP 3 : VALUE =================
    void SetVariableValue()
    {
        if (string.IsNullOrEmpty(valueInput.text))
            return;

        int.TryParse(valueInput.text, out variableValue);
        valueSet = true;

        inputPanel.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(ShowValueAnimation(variableValue));
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

    // ================= PARTICLE SLOW STOP =================
    IEnumerator StopParticlesSlowly()
    {
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

    // ================= DEBUG =================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
