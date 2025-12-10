

using UnityEngine;
using UnityEngine.InputSystem;

// Minimal 2D platformer player movement.
// - Horizontal movement
// - Single jump when grounded
// Works with keyboard (Horizontal axis + Jump button) and gamepad (left stick + A button).
public class Player_movement : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 6f;
	public float jumpForce = 12f;

	[Header("Ground Check")]
	public Transform groundCheckPoint;
	public float groundCheckRadius = 0.1f;
	public LayerMask groundLayer;

	Rigidbody2D rb;
	bool facingRight = true;
	bool isJumping = false;
	float jumpDirection = 0f; // 1 = right, -1 = left

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
		rb.freezeRotation = true;

		if (groundCheckPoint == null)
		{
			GameObject go = new GameObject("GroundCheck");
			go.transform.parent = transform;
			go.transform.localPosition = new Vector3(0, -0.5f, 0);
			groundCheckPoint = go.transform;
		}
	}

	void Update()
	{
		// Read horizontal input (keyboard) and fallback to gamepad stick
		float h = Input.GetAxisRaw("Horizontal");
		if (Mathf.Abs(h) < 0.01f && Gamepad.current != null)
			h = Gamepad.current.leftStick.ReadValue().x;

		// If jumping, lock direction - do not read horizontal input
		if (isJumping)
		{
			horizontalInput = jumpDirection;
		}
		else
		{
			horizontalInput = Mathf.Clamp(h, -1f, 1f);
		}

		// Jump: keyboard or gamepad
		bool jumpPressed = Input.GetButtonDown("Jump") || (Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame);
		if (jumpPressed && IsGrounded())
		{
			isJumping = true;
			jumpDirection = Mathf.Clamp(horizontalInput, -1f, 1f);
			if (Mathf.Abs(jumpDirection) < 0.1f)
				jumpDirection = facingRight ? 1f : -1f; // jump in current facing direction if no input
			rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
		}

		// Check if grounded to stop jump lock
		if (IsGrounded())
		{
			isJumping = false;
		}

		// Flip sprite if needed
		if (horizontalInput > 0.01f && !facingRight) Flip();
		else if (horizontalInput < -0.01f && facingRight) Flip();
	}

	float horizontalInput = 0f;

	void FixedUpdate()
	{
		rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
	}

	bool IsGrounded()
	{
		return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer) != null;
	}

	void Flip()
	{
		facingRight = !facingRight;
		Vector3 s = transform.localScale;
		s.x *= -1f;
		transform.localScale = s;
	}

	void OnDrawGizmosSelected()
	{
		if (groundCheckPoint != null)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
		}
	}
}

