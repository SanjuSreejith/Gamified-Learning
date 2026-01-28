using UnityEngine;

public class PlayerJetpackAnimator2D : MonoBehaviour
{
    public Animator animator;

    public void SetJetpack(bool enabled)
    {
        animator.SetBool("HasJetpack", enabled);
    }

    public void UpdateMovement(float speedY, float speedX)
    {
        animator.SetFloat("SpeedX", Mathf.Abs(speedX));
        animator.SetFloat("SpeedY", speedY);
    }
}
