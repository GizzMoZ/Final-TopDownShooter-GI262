using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] LayerMask wallMask;  

    public int Damage { get; private set; }

    Rigidbody2D rb;
    Vector2 lastPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPos = rb.position;

        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Walls");
    }

    public void Launch(Vector2 direction, float speed, float lifetime, int damage)
    {
        Damage = damage;
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
        if (other.GetComponent<PlayerHealth>() != null) return;

        other.GetComponent<IDamageable>()?.TakeDamage(Damage, DamageType.Ranged);
        Destroy(gameObject);
    }
}