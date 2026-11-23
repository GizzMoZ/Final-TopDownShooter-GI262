using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] LayerMask wallMask;   // set to Walls (falls back to the Walls layer if empty)

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 lastPos;
    float dieAt;
    bool reflected;

    public bool IsReflected => reflected;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        lastPos = rb.position;
        dieAt = Time.time + 10f;   // safety, replaced by Launch

        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Walls");
    }

    public void Launch(Vector2 direction, float speed, float lifetime)
    {
        lastPos = rb.position;
        rb.linearVelocity = direction * speed;
        dieAt = Time.time + lifetime;
    }

    // Sends the bullet back the other way (a parry). It now kills the first enemy it hits and never hurts the player.
    public void Reflect(Vector2 direction, float speed, float lifetime)
    {
        reflected = true;
        lastPos = rb.position;
        rb.linearVelocity = direction.normalized * speed;
        dieAt = Time.time + lifetime;

        if (sr != null) sr.color = new Color(0.4f, 0.9f, 1f);
    }

    void FixedUpdate()
    {
        if (Time.time >= dieAt)
        {
            Destroy(gameObject);
            return;
        }

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

        if (reflected)
        {
            // a reflected bullet kills the first enemy it meets, whatever its type
            var enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(1, DamageType.Special);
                Destroy(gameObject);
                return;
            }

            if (other.GetComponent<PlayerHealth>() != null) return;   // never hurts the player
            Destroy(gameObject);                                       // a wall
            return;
        }

        if (other.GetComponent<EnemyHealth>() != null) return;   // enemy bullets pass through enemies

        other.GetComponent<PlayerHealth>()?.TakeDamage(1);
        Destroy(gameObject);
    }
}