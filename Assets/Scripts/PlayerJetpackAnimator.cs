using UnityEngine;

public class PlayerJetpackAnimator2D : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Animator Params")]
    [SerializeField] string hasJetpackParam = "HasJetpack";
    [SerializeField] string speedXParam = "SpeedX";
    [SerializeField] string isFlyingParam = "IsFlying";

    bool isFlying;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    // =========================
    // Jetpack Visual Toggle
    // =========================
    public void SetJetpack(bool enabled)
    {
        if (!animator) return;
        animator.SetBool(hasJetpackParam, enabled);
    }

    // =========================
    // Start Flying (Manual / Auto)
    // =========================
    public void StartFlying()
    {
        if (!animator) return;

        if (!isFlying)
        {
            isFlying = true;
            animator.SetBool(isFlyingParam, true);
        }
    }

    // =========================
    // Stop Flying (Normal Stop)
    // =========================
    public void StopFlying()
    {
        if (!animator) return;

        if (isFlying)
        {
            isFlying = false;
            animator.SetBool(isFlyingParam, false);
        }

        animator.SetFloat(speedXParam, 0f);
    }

    // =========================
    // Horizontal Speed Update
    // =========================
    public void UpdateXSpeed(float xSpeed)
    {
        if (!animator) return;

        animator.SetFloat(speedXParam, Mathf.Abs(xSpeed));
    }

    // =========================
    // Full Reset (Fail / Equip / Scene Reset)
    // =========================
    public void ResetMovement()
    {
        if (!animator) return;

        isFlying = false;

        animator.SetBool(isFlyingParam, false);
        animator.SetBool(hasJetpackParam, false);
        animator.SetFloat(speedXParam, 0f);
    }

    // =========================
    // Helper (Optional)
    // =========================
    public bool IsFlying()
    {
        return isFlying;
    }
}
