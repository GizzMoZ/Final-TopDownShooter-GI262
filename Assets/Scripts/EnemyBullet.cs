using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] LayerMask wallMask;  

    Rigidbody2D rb;
    Vector2 lastPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPos = rb.position;

        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Walls");
    }

    public void Launch(Vector2 direction, float speed, float lifetime)
    {
        lastPos = rb.position;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        if (Physics2D.Linecast(lastPos, pos, wallMask))
        {
            Destroy(gameObject);
            return;
        }
        lastPos = pos;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.GetComponent<EnemyHealth>() != null) return;  

        other.GetComponent<PlayerHealth>()?.TakeDamage(1);
        Destroy(gameObject);
    }
}