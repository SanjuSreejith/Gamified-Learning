using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 8f;

    Rigidbody2D rb;
    Animator anim;
    bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");

        // Movement
        rb.linearVelocity = new Vector2(x * speed, rb.linearVelocity.y);

        // Running animation
        anim.SetBool("IsMoving", x != 0);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            anim.SetBool("IsJumping", true);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            anim.SetBool("IsJumping", false);
        }
    }
}


