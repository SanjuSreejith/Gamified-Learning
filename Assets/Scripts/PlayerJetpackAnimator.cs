using UnityEngine;

public class PlayerJetpackAnimator2D : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Animator Params")]
    [SerializeField] string hasJetpackParam = "HasJetpack";
    [SerializeField] string speedXParam = "SpeedX";
    [SerializeField] string flyTriggerParam = "Fly";

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Enable / disable jetpack visuals/state
    /// </summary>
    public void SetJetpack(bool enabled)
    {
        if (!animator) return;
        animator.SetBool(hasJetpackParam, enabled);
    }

    /// <summary>
    /// Call ONCE when flight starts
    /// </summary>
    public void PlayFly()
    {
        if (!animator) return;

        // Reset first to avoid stuck trigger
        animator.ResetTrigger(flyTriggerParam);
        animator.SetTrigger(flyTriggerParam);
    }

    /// <summary>
    /// Update horizontal movement speed
    /// </summary>
    public void UpdateXSpeed(float xSpeed)
    {
        if (!animator) return;
        animator.SetFloat(speedXParam, Mathf.Abs(xSpeed));
    }

    /// <summary>
    /// Call when flight ENDS (landing / fail)
    /// </summary>
    public void ResetMovement()
    {
        if (!animator) return;

        animator.ResetTrigger(flyTriggerParam);
        animator.SetFloat(speedXParam, 0f);
    }
}
