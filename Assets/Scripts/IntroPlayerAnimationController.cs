using UnityEngine;

public class IntroAutoWalkUI : MonoBehaviour
{
    [Header("UI Movement")]
    public RectTransform targetPoint;
    public float walkSpeed = 300f; // UI units (pixels per second)

    [Header("Timings")]
    public float startAnimationDuration = 0.6f;

    [Header("Audio")]
    public AudioSource walkAudioSource;
    public AudioClip walkClip;

    RectTransform rectTransform;
    Animator animator;
    bool canMove = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        animator = GetComponent<Animator>();

        // Start animation plays automatically
        Invoke(nameof(BeginWalk), startAnimationDuration);
    }

    void BeginWalk()
    {
        animator.SetBool("isWalking", true);
        canMove = true;

        // ▶ Start walking sound
        if (walkAudioSource && walkClip)
        {
            walkAudioSource.clip = walkClip;
            walkAudioSource.loop = true;
            walkAudioSource.Play();
        }
    }

    void Update()
    {
        if (!canMove) return;

        rectTransform.anchoredPosition = Vector2.MoveTowards(
            rectTransform.anchoredPosition,
            targetPoint.anchoredPosition,
            walkSpeed * Time.deltaTime
        );

        if (Vector2.Distance(rectTransform.anchoredPosition, targetPoint.anchoredPosition) < 5f)
        {
            canMove = false;

            animator.SetBool("isWalking", false);
            animator.SetBool("reachedTarget", true);

            // ⏹ Stop walking sound
            if (walkAudioSource && walkAudioSource.isPlaying)
                walkAudioSource.Stop();
        }
    }
}
