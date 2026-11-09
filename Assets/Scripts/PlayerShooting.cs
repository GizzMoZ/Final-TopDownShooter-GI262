using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] WeaponData startingMelee;
    [SerializeField] WeaponData startingRanged;
    [SerializeField] WeaponPickup pickupPrefab;
    [SerializeField] float muzzleOffset = 0.6f;
    [SerializeField] float pickupRadius = 1f;
    [SerializeField] float throwSpeed = 14f;

    WeaponData melee;
    WeaponData ranged;
    int rangedAmmo;
    bool holdingMelee;

    Camera cam;
    float nextFireTime;

    WeaponData Current => holdingMelee ? melee : ranged;

    void Awake()
    {
        cam = Camera.main;
        melee = startingMelee;
        ranged = startingRanged;
        rangedAmmo = ranged != null ? ranged.maxAmmo : 0;
        holdingMelee = melee != null;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null || kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame && melee != null) holdingMelee = true;
        if (kb.digit2Key.wasPressedThisFrame && ranged != null) holdingMelee = false;

        // Right mouse: pick up a nearby weapon, otherwise throw the held one
        if (mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 aimDir = GetAim(mouse);
            if (!TryPickup(aimDir)) ThrowWeapon(aimDir);
        }

        WeaponData w = Current;
        if (w == null) return;

        bool trigger = w.automatic
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;

        if (!trigger || Time.time < nextFireTime) return;

        Vector2 aim = GetAim(mouse);
        if (holdingMelee)
        {
            Melee(aim);
        }
        else
        {
            if (rangedAmmo <= 0) return;
            rangedAmmo--;
            Shoot(aim);
        }
        nextFireTime = Time.time + w.fireDelay;
    }

    Vector2 GetAim(Mouse mouse)
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 aim = mouseWorld - (Vector2)transform.position;
        return aim.sqrMagnitude < 0.0001f ? (Vector2)transform.up : aim.normalized;
    }

    // Returns true if a weapon was picked up
    bool TryPickup(Vector2 aim)
    {
        WeaponPickup best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, pickupRadius))
        {
            var p = hit.GetComponent<WeaponPickup>();
            if (p == null) continue;
            float d = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = p; }
        }
        if (best == null) return false;

        WeaponData newWeapon = best.weapon;
        int newAmmo = best.ammo;
        Destroy(best.gameObject);

        Vector2 throwPos = (Vector2)transform.position + aim * muzzleOffset;

        if (newWeapon.isMelee)
        {
            // Throw out the old melee weapon toward the mouse
            if (melee != null) SpawnPickup(melee, 0, throwPos, aim * throwSpeed);
            melee = newWeapon;
            holdingMelee = true;
        }
        else
        {
            // Throw out the old ranged weapon toward the mouse
            if (ranged != null) SpawnPickup(ranged, rangedAmmo, throwPos, aim * throwSpeed);
            ranged = newWeapon;
            rangedAmmo = newAmmo;
            holdingMelee = false;
        }
        return true;
    }

    void ThrowWeapon(Vector2 aim)
    {
        WeaponData w = Current;
        if (w == null) return;

        Vector2 pos = (Vector2)transform.position + aim * muzzleOffset;
        SpawnPickup(w, holdingMelee ? 0 : rangedAmmo, pos, aim * throwSpeed);

        if (holdingMelee) melee = null; else ranged = null;
        holdingMelee = melee != null;
    }

    void SpawnPickup(WeaponData w, int a, Vector2 pos, Vector2 velocity)
    {
        var p = Instantiate(pickupPrefab, pos, Quaternion.identity);
        p.Setup(w, a);
        p.Launch(velocity);
    }

    void Shoot(Vector2 aim)
    {
        WeaponData w = ranged;
        for (int i = 0; i < w.pellets; i++)
        {
            float angle = Random.Range(-w.spreadAngle / 2f, w.spreadAngle / 2f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * aim;

            Bullet bullet = Instantiate(w.bulletPrefab,
                                        (Vector2)transform.position + aim * muzzleOffset,
                                        Quaternion.identity);
            bullet.Launch(dir, w.bulletSpeed, w.bulletLifetime, w.damage);
        }
    }

    void Melee(Vector2 aim)
    {
        WeaponData w = melee;
        Vector2 center = (Vector2)transform.position + aim * w.meleeRange;

        ShowSlash(center, w.meleeRadius);

        foreach (var hit in Physics2D.OverlapCircleAll(center, w.meleeRadius))
        {
            if (hit.gameObject == gameObject || hit.isTrigger) continue;
            hit.GetComponent<IDamageable>()?.TakeDamage(w.damage, DamageType.Melee);
        }
    }

    // Brief white circle so you can see the swing and its reach
    void ShowSlash(Vector2 center, float radius)
    {
        var go = new GameObject("Slash");
        go.transform.position = center;
        go.transform.localScale = Vector3.one * radius * 2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = new Color(1f, 1f, 1f, 0.5f);
        sr.sortingOrder = 50;

        Destroy(go, 0.1f);
    }

    static Sprite circleSprite;

    static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 64;
        var tex = new Texture2D(size, size);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
            }
        tex.Apply();

        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;

        string m = melee != null ? melee.weaponName : "-";
        string r = ranged != null ? $"{ranged.weaponName}  {rangedAmmo}/{ranged.maxAmmo}" : "-";

        GUI.Label(new Rect(20, 20, 600, 40),
            (holdingMelee ? "> " : "   ") + "1  Melee: " + m, style);
        GUI.Label(new Rect(20, 55, 600, 40),
            (!holdingMelee && ranged != null ? "> " : "   ") + "2  Ranged: " + r, style);
    }
}