using System.Collections;
using UnityEngine;

public enum EnemyType { Normal, MeleeOnly, RangedOnly }

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] EnemyType type = EnemyType.Normal;
    [SerializeField] int health = 1;

    public bool IsDead { get; private set; }

    SpriteRenderer sr;
    Collider2D col;
    Rigidbody2D rb;
    Color baseColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        switch (type)
        {
            case EnemyType.Normal: baseColor = new Color(0.9f, 0.25f, 0.25f); break; // red
            case EnemyType.MeleeOnly: baseColor = new Color(1f, 0.6f, 0.1f); break; // orange
            case EnemyType.RangedOnly: baseColor = new Color(0.3f, 0.5f, 1f); break; // blue
        }
        sr.color = baseColor;
    }

    public void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead) return;

        bool immune =
            (type == EnemyType.MeleeOnly && damageType == DamageType.Ranged) ||
            (type == EnemyType.RangedOnly && damageType == DamageType.Melee);

        if (immune)
        {
            StopAllCoroutines();
            StartCoroutine(Flash());
            return;
        }

        health -= amount;
        if (health <= 0) Die();
    }

    IEnumerator Flash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (!IsDead) sr.color = baseColor;
    }

    void Die()
    {
        IsDead = true;
        StopAllCoroutines();

        col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        sr.color = new Color(0.25f, 0.02f, 0.02f);  
        sr.sortingOrder -= 1;
    }
}