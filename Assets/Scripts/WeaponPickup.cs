using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponData weapon;
    public int ammo = -1;   // -1 = full ammo

    Rigidbody2D rb;
    SpriteRenderer sr;
    bool ready;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (!ready && weapon != null)
            Setup(weapon, ammo < 0 ? weapon.maxAmmo : ammo);
    }

    public void Setup(WeaponData w, int a)
    {
        weapon = w;
        ammo = a;
        ready = true;
        if (sr != null) sr.color = w.color;
    }

    public void Launch(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        rb.angularVelocity = velocity.sqrMagnitude > 0.01f ? 720f : 0f;
    }
}