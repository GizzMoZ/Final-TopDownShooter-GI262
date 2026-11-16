using System.Collections;
using UnityEngine;

public enum EnemyType { Normal, MeleeOnly, RangedOnly }

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] EnemyType type = EnemyType.Normal;
    [SerializeField] int health = 1;

    [Header("Drop on death")]
    [SerializeField] WeaponData dropWeapon;     
    [SerializeField] int dropAmmo = 6;
    [SerializeField] WeaponPickup pickupPrefab;

    public bool IsDead { get; private set; }
    public bool IsStunned { get; private set; }

    SpriteRenderer sr;
    Collider2D col;
    Rigidbody2D rb;
    Color baseColor;
    Coroutine flashRoutine;
    Coroutine stunRoutine;

    // Feedback color.
    static readonly Color StunColor = new Color(1f, 0.92f, 0.2f);         // stunt = yellow
    static readonly Color ImmuneFlashColor = new Color(0.4f, 0.4f, 0.4f); // use wrong weapon = gray
    static readonly Color DeadColor = new Color(0.35f, 0.1f, 0.1f);       // dead = dark red

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        baseColor = sr.color;   // temp color pre art
    }

    public void TakeDamage(int amount, DamageType damageType)
    {
        if (IsDead) return;

        bool immune =
            (type == EnemyType.MeleeOnly && damageType == DamageType.Ranged) ||
            (type == EnemyType.RangedOnly && damageType == DamageType.Melee);

        if (immune)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());
            return;
        }

        health -= amount;
        if (health <= 0) Die();
    }

    public void Stun(float duration)
    {
        if (IsDead) return;

        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        IsStunned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        sr.color = StunColor;

        yield return new WaitForSeconds(duration);

        IsStunned = false;
        if (!IsDead) sr.color = baseColor;
    }

    IEnumerator Flash()
    {
        sr.color = ImmuneFlashColor;
        yield return new WaitForSeconds(0.08f);
        if (!IsDead) sr.color = IsStunned ? StunColor : baseColor;
    }

    void Die()
    {
        IsDead = true;
        IsStunned = false;
        StopAllCoroutines();

        col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        WeaponData dropW = dropWeapon;
        int dropA = dropAmmo;

        var gun = GetComponent<EnemyWeapon>();
        if (gun != null)
        {
            if (gun.Weapon != null)
            {
                dropW = gun.Weapon;
                dropA = gun.Ammo;
            }
            gun.Hide();
        }

        if (dropW != null && pickupPrefab == null)
            Debug.LogWarning(name + " had a weapon to drop but Pickup Prefab is not set on its Enemy Health.", this);

        if (dropW != null && pickupPrefab != null)
        {
            var p = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
            p.Setup(dropW, dropA);
            p.Launch(Vector2.zero);
        }

        sr.color = DeadColor;
        sr.sortingOrder -= 1;
    }
}