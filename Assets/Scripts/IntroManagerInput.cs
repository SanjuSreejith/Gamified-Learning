using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class IntroBotGuideController2D : MonoBehaviour
{
    /* ================= REFERENCES ================= */
    [Header("References")]
    public Transform groundCheck;
    public Transform frontCheck;
    public Transform teleportTarget;
    public LayerMask groundLayer;

    /* ================= UI ================= */
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Headlight")]
    public GameObject headlightObject;

    /* ================= MOVEMENT ================= */
    [Header("Movement")]
    public float moveSpeed = 4.5f;

    /* ================= JUMP ================= */
    [Header("Jump")]
    public float jumpForce = 7.5f;
    public float wallCheckDistance = 0.4f;
    public float groundRadius = 0.15f;

    [Header("Timing")]
    public float teleportDelay = 1.2f;

    Rigidbody2D rb;
    Animator anim;
    bool isGrounded;
    bool moving;
    bool facingRight = true;
    bool dialogueFinished;


    int dialogueIndex;

    string[] introDialogue =
    {
        "Wait.",
        "You need to wear your headlight.",
        "This place isn’t safe in the dark.",
        "I’ll go ahead.",
        "You follow me."
    };

    /* ================= INIT ================= */

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        dialoguePanel.SetActive(true);
        dialogueIndex = 0;
        dialogueText.text = introDialogue[dialogueIndex];

        if (headlightObject)
            headlightObject.SetActive(false);
    }

    void Update()
    {
        CheckGround();

        if (!moving && Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceDialogue();
        }

        anim.SetBool("isGrounded", isGrounded);
    }

    /* ================= DIALOGUE ================= */

    void AdvanceDialogue()
    {
        if (dialogueFinished) return;

        dialogueIndex++;

        if (dialogueIndex >= introDialogue.Length)
        {
            dialogueFinished = true;

            // ✅ Hide panel ONCE when dialogue is over
            dialoguePanel.SetActive(false);

            StartCoroutine(MoveSequence());
            return;
        }

        dialogueText.text = introDialogue[dialogueIndex];

        // 🔦 Turn ON headlight at correct line
        if (dialogueIndex == 1 && headlightObject)
        {
            headlightObject.SetActive(true);
        }
    }


    /* ================= MOVE SEQUENCE ================= */
    IEnumerator MoveSequence()
    {
        moving = true;
        anim.SetBool("isWalking", true);

        float moveTime = 2f;
        float timer = 0f;
        float direction = 1f; // moving right (change if needed)

        Flip(direction);

        while (timer < moveTime)
        {
            // ✅ Correct physics movement
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

            // ✅ EnemyAI-style smart jump
            TrySmartJump(direction);

            timer += Time.deltaTime;
            yield return null;
        }

        // ✅ Stop movement
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("isWalking", false);

        // ⏳ Small story pause (feels intentional)
        yield return new WaitForSeconds(teleportDelay);

        // ✅ REAL teleport (no hiding, no disabling)
        rb.position = teleportTarget.position;

        moving = false;
    }

    /* ================= SMART JUMP (EnemyAI style) ================= */

    void TrySmartJump(float direction)
    {
        if (!isGrounded) return;

        RaycastHit2D wallHit = Physics2D.Raycast(
            frontCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

        if (wallHit.collider != null)
        {
            Jump();
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        anim.SetTrigger("Jump");
    }

    /* ================= GROUND CHECK ================= */

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
    }

    /* ================= FLIP ================= */

    void Flip(float direction)
    {
        if (direction > 0 && !facingRight)
            FlipSprite();
        else if (direction < 0 && facingRight)
            FlipSprite();
    }

    void FlipSprite()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    /* ================= DEBUG ================= */

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        if (frontCheck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(frontCheck.position, Vector2.right * wallCheckDistance);
            Gizmos.DrawRay(frontCheck.position, Vector2.left * wallCheckDistance);
        }
    }
}
