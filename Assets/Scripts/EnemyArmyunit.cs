using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyArmyUnit : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stopDistance = 1.2f;

    [Header("Health")]
    public int maxHealth = 100;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 3f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public Transform player;

    private int currentHealth;
    public bool isAlive { get; private set; }

    /* ================= INIT ================= */
    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        ResetEnemy();
    }

    void FixedUpdate()
    {
        if (!isAlive || player == null)
            return;

        MoveTowardPlayer();
    }

    /* ================= MOVE ================= */
    void MoveTowardPlayer()
    {
        float distance = Vector2.Distance(rb.position, player.position);

        if (distance <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
            return;
        }

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        animator.SetBool("isMoving", true);
    }

    /* ================= DAMAGE ================= */
    public void TakeDamage(int damage)
    {
        if (!isAlive)
            return;

        currentHealth -= damage;
        animator.SetTrigger("hit");

        if (currentHealth <= 0)
            Die();
    }

    /* ================= DIE ================= */
    void Die()
    {
        isAlive = false;
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("isMoving", false);
        animator.SetTrigger("die");

        // Respawn after delay
        Invoke(nameof(Respawn), respawnDelay);
    }

    /* ================= RESPAWN ================= */
    void Respawn()
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning($"{name} has no Respawn Point assigned!");
            return;
        }

        transform.position = respawnPoint.position;
        ResetEnemy();
    }

    /* ================= RESET ================= */
    void ResetEnemy()
    {
        currentHealth = maxHealth;
        isAlive = true;

        rb.linearVelocity = Vector2.zero;

        animator.Rebind();
        animator.Update(0f);
    }

    /* ================= UTIL ================= */
    public float GetX()
    {
        return transform.position.x;
    }
}