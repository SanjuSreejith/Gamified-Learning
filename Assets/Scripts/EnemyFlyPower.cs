using UnityEngine;

public class EnemyPower : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 10;

    private Vector3 target;

    public void Init(Vector3 targetPos)
    {
        target = targetPos;
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Machine"))
        {
            // other.GetComponent<MachineHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}