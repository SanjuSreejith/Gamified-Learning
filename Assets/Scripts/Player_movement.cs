using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float acceleration = 12f;
    public float deceleration = 16f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    // Collision-based ground detection (works without layers)
    HashSet<Collider2D> groundContactSet = new HashSet<Collider2D>();
    [Tooltip("Minimum Y component of a contact normal to count as 'ground' (0..1). Increase to ignore shallow slopes.")]
    public float groundNormalMinY = 0.65f;

    Rigidbody2D rb;
    Animator anim;

    float moveInput;
    float coyoteCounter;
    float jumpBufferCounter;
    bool isGrounded;

    // When true, jump animation is playing and movement animation should be suppressed.
    bool isJumpAnimActive = false;
    // Locks further jumps/clearing until player lands on a collider.
    bool jumpLocked = false;
    // Current facing direction. True when facing right.
    bool facingRight = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // INPUT
        moveInput = Input.GetAxisRaw("Horizontal");

        // FLIP: flip sprite when input direction changes
        if (moveInput > 0.01f && !facingRight)
            Flip();
        else if (moveInput < -0.01f && facingRight)
            Flip();

        // JUMP BUFFER
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // GROUND CHECK
        if (groundCheck != null)
        {
            int mask = groundLayer.value == 0 ? ~0 : groundLayer.value;
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundRadius,
                mask
            );
        }
        else
        {
            // No groundCheck assigned: use collision-contact normals to decide grounded state.
            // This requires no layer setup and works with moving/floating platforms.
            isGrounded = groundContactSet.Count > 0;
        }

        // If we were locked in the jump animation and we've landed, clear the lock
        if (isGrounded && jumpLocked)
        {
            isJumpAnimActive = false;
            jumpLocked = false;
        }

        // COYOTE TIME
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // JUMP
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteCounter = 0;
            isJumpAnimActive = true;
            jumpLocked = true;
        }

        // ✅ ANIMATIONS (FIXED)
        if (anim != null)
        {
            // Suppress moving animation while the jump animation is active.
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isJumpAnimActive;

            anim.SetBool("IsMoving", isMoving);
            anim.SetBool("IsJumping", isJumpAnimActive);
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f
            ? acceleration
            : deceleration;

        float movement = speedDiff * accelRate;
        rb.AddForce(Vector2.right * movement, ForceMode2D.Force);
    }

    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluateCollisionContacts(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateCollisionContacts(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // remove the collider from the contact set when it stops colliding
        if (collision.collider != null && groundContactSet.Contains(collision.collider))
            groundContactSet.Remove(collision.collider);
    }

    void EvaluateCollisionContacts(Collision2D collision)
    {
        if (collision == null || collision.contacts == null) return;

        bool added = false;
        foreach (ContactPoint2D cp in collision.contacts)
        {
            if (cp.normal.y >= groundNormalMinY)
            {
                if (collision.collider != null && !groundContactSet.Contains(collision.collider))
                {
                    groundContactSet.Add(collision.collider);
                    added = true;
                }
                break;
            }
        }

        // If no contact met the threshold, ensure collider isn't in the set
        if (!added && collision.collider != null && groundContactSet.Contains(collision.collider))
            groundContactSet.Remove(collision.collider);
    }
}
