using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponData weapon;
    public int ammo = -1;   // -1 = full ammo

    Rigidbody2D rb;
    SpriteRenderer sr;        // the prefab's own placeholder sprite
    SpriteRenderer visual;    // child that shows the weapon's real sprite
    bool ready;
    bool flying;

    const float FlyDamping = 1f;
    const float RestDamping = 3f;
    const float MinFlySpeed = 4f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.linearDamping = RestDamping;
        rb.angularDamping = 3f;

        // child object for the real weapon sprite, so the prefab's own scale doesn't shrink it
        var go = new GameObject("WeaponSprite");
        go.transform.SetParent(transform, false);
        visual = go.AddComponent<SpriteRenderer>();
        visual.enabled = false;
        if (sr != null)
        {
            visual.sortingLayerID = sr.sortingLayerID;
            visual.sortingOrder = sr.sortingOrder + 1;
        }
    }

    void Start()
    {
        if (!ready && weapon != null)
            Setup(weapon, ammo < 0 ? weapon.maxAmmo : ammo);
    }

    void FixedUpdate()
    {
        if (flying && rb.linearVelocity.magnitude < MinFlySpeed)
            StopFlying();
    }

    public void Setup(WeaponData w, int a)
    {
        weapon = w;
        ammo = a;
        ready = true;
        ApplyLook(w);
    }

    void ApplyLook(WeaponData w)
    {
        if (w.sprite != null)
        {
            // real sprite: hide the placeholder square, show the weapon at its set length
            if (sr != null) sr.enabled = false;

            visual.enabled = true;
            visual.sprite = w.sprite;
            visual.color = Color.white;
            visual.transform.localScale = Vector3.one * WeaponVisual.FitScale(w.sprite, w.spriteLength, transform);
        }
        else
        {
            // no sprite yet: the placeholder square, tinted with the weapon's color
            visual.enabled = false;
            if (sr != null)
            {
                sr.enabled = true;
                sr.color = w.color;
            }
        }
    }

    public void Launch(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        rb.angularVelocity = velocity.sqrMagnitude > 0.01f ? 720f : 0f;

        flying = weapon != null && velocity.magnitude > MinFlySpeed;
        rb.linearDamping = flying ? FlyDamping : RestDamping;
    }

    void StopFlying()
    {
        flying = false;
        rb.linearDamping = RestDamping;
        rb.angularVelocity = 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!flying) return;
        if (other.isTrigger) return;
        if (other.GetComponent<PlayerHealth>() != null) return;

        var enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            if (weapon.killsWhenThrown || CardManager.ThrownWeaponsKill) enemy.TakeDamage(weapon.damage, DamageType.Thrown);
            else enemy.Stun(weapon.stunDuration);
        }

        rb.linearVelocity = Vector2.zero;   // stops on enemies and walls alike
        StopFlying();
    }
}