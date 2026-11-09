using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float sightRange = 8f;
    [SerializeField] LayerMask obstacleMask;

    Transform player;
    Rigidbody2D rb;
    EnemyHealth health;
    bool chasing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) player = ph.transform;
    }

    void FixedUpdate()
    {
        if (health != null && health.IsDead) { rb.linearVelocity = Vector2.zero; return; }
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (!chasing)
        {
            if (toPlayer.magnitude <= sightRange &&
                !Physics2D.Linecast(rb.position, player.position, obstacleMask))
                chasing = true;
            else
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        Vector2 dir = toPlayer.normalized;
        rb.linearVelocity = dir * moveSpeed;
        rb.rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (health != null && health.IsDead) return;

        var ph = c.collider.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(1);
    }
}