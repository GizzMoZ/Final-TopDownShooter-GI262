using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] LayerMask wallMask;

    public int Damage { get; private set; }

    Rigidbody2D rb;
    Vector2 lastPos;
    bool pierce;
    DamageType damageType = DamageType.Ranged;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPos = rb.position;

        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Walls");
    }

    public void Launch(Vector2 direction, float speed, float lifetime, int damage,
                       bool pierce = false, bool killsAnyEnemy = false)
    {
        Damage = damage;
        this.pierce = pierce;
        damageType = killsAnyEnemy ? DamageType.Special : DamageType.Ranged;

        lastPos = rb.position;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        // Sweep from the last position so a fast bullet can't skip over a thin wall
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

        var target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(Damage, damageType);
            if (pierce) return;   
        }

        Destroy(gameObject);      // a wall, or an enemy that stops the bullet
    }
}