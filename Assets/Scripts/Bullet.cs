using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int Damage { get; private set; }

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float speed, float lifetime, int damage)
    {
        Damage = damage;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.GetComponent<PlayerHealth>() != null) return;

        other.GetComponent<IDamageable>()?.TakeDamage(Damage, DamageType.Ranged);
        Destroy(gameObject);
    }
}