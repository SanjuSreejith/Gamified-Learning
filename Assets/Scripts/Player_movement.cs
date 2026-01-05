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

    [Header("Audio Clips")]
    public AudioClip walkClip;
    public AudioClip jumpClip;

    [Header("Audio Sources")]
    public AudioSource footstepSource;   // looping footsteps
    public AudioSource actionSource;     // jump, land, etc.

    // Collision based ground detection
    HashSet<Collider2D> groundContactSet = new HashSet<Collider2D>();
    public float groundNormalMinY = 0.65f;

    Rigidbody2D rb;
    Animator anim;

    float moveInput;
    float coyoteCounter;
    float jumpBufferCounter;
    bool isGrounded;

    bool isJumpAnimActive;
    bool jumpLocked;
    bool facingRight = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Safety checks
        if (footstepSource != null)
        {
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
            footstepSource.volume = 1f;
            footstepSource.spatialBlend = 0f; // 2D sound
        }

        if (actionSource != null)
        {
            actionSource.loop = false;
            actionSource.playOnAwake = false;
            actionSource.volume = 1f;
            actionSource.spatialBlend = 0f; // 2D sound
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Flip
        if (moveInput > 0.01f && !facingRight) Flip();
        else if (moveInput < -0.01f && facingRight) Flip();

        // Jump buffer
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Ground check
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
            isGrounded = groundContactSet.Count > 0;
        }

        // Reset jump lock on land
        if (isGrounded && jumpLocked)
        {
            isJumpAnimActive = false;
            jumpLocked = false;
        }

        // Coyote time
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // Jump
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            jumpBufferCounter = 0;
            coyoteCounter = 0;

            isJumpAnimActive = true;
            jumpLocked = true;

            PlayJumpSound();
        }

        // Animations
        if (anim != null)
        {
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isJumpAnimActive;
            anim.SetBool("IsMoving", isMoving);
            anim.SetBool("IsJumping", isJumpAnimActive);
        }

        HandleFootstepSound();
    }

    void FixedUpdate()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f
            ? acceleration
            : deceleration;

        rb.AddForce(Vector2.right * speedDiff * accelRate, ForceMode2D.Force);
    }

    // ================= SOUND =================

    void HandleFootstepSound()
    {
        if (footstepSource == null || walkClip == null) return;

        bool shouldPlay =
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.x) > 0.1f &&
            !isJumpAnimActive;

        if (shouldPlay)
        {
            if (!footstepSource.isPlaying)
            {
                footstepSource.clip = walkClip;
                footstepSource.Play();
            }
        }
        else
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }
    }

    void PlayJumpSound()
    {
        if (actionSource == null || jumpClip == null) return;
        actionSource.PlayOneShot(jumpClip, 1f);
    }

    // ================= COLLISION =================

    void OnCollisionEnter2D(Collision2D collision) => EvaluateCollisionContacts(collision);
    void OnCollisionStay2D(Collision2D collision) => EvaluateCollisionContacts(collision);

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider != null)
            groundContactSet.Remove(collision.collider);
    }

    void EvaluateCollisionContacts(Collision2D collision)
    {
        if (collision == null || collision.contacts == null) return;

        foreach (ContactPoint2D cp in collision.contacts)
        {
            if (cp.normal.y >= groundNormalMinY)
            {
                groundContactSet.Add(collision.collider);
                return;
            }
        }

        groundContactSet.Remove(collision.collider);
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
