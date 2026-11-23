using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] WeaponData startingMelee;
    [SerializeField] WeaponData startingRanged;
    [SerializeField] WeaponPickup pickupPrefab;
    [SerializeField] LayerMask wallMask;            // set to Walls (falls back to the Walls layer if empty)
    [SerializeField] float muzzleOffset = 0.6f;
    [SerializeField] float pickupRadius = 1f;
    [SerializeField] float throwSpeed = 14f;
    [SerializeField] bool showHitArea;              // tick to draw the melee hit circle while tuning reach

    WeaponData melee;
    WeaponData ranged;
    int rangedAmmo;
    bool holdingMelee;

    Camera cam;
    PlayerWeaponVisual weaponVisual;
    PlayerParry parry;
    float nextFireTime;
    float nextScrollTime;

    WeaponData Current => holdingMelee ? melee : ranged;
    public WeaponData CurrentWeapon => Current;

    // the card's granted weapon never runs out of ammo (only while you are holding it)
    bool HasUnlimitedAmmo =>
        CardManager.UnlimitedAmmo && ranged != null && ranged == CardManager.GrantedWeapon;

    void Awake()
    {
        cam = Camera.main;
        weaponVisual = GetComponent<PlayerWeaponVisual>();
        parry = GetComponent<PlayerParry>();

        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Walls");

        ApplyCard();
    }

    // Sets up the starting weapons, then lets the chosen card change them.
    // Called when the scene loads and again right after a card is picked.
    public void ApplyCard()
    {
        melee = startingMelee;
        ranged = startingRanged;
        rangedAmmo = ranged != null ? ranged.maxAmmo : 0;

        if (CardManager.MeleeDisabled) melee = null;
        if (CardManager.RangedDisabled) { ranged = null; rangedAmmo = 0; }

        WeaponData grantedRanged = CardManager.GrantedWeapon;
        if (grantedRanged != null)
        {
            ranged = grantedRanged;
            rangedAmmo = grantedRanged.maxAmmo;
        }

        WeaponData grantedMelee = CardManager.GrantedMeleeWeapon;
        if (grantedMelee != null) melee = grantedMelee;

        holdingMelee = melee != null;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null || kb == null) return;

        // 1 / 2: pick a slot directly
        if (kb.digit1Key.wasPressedThisFrame && melee != null) holdingMelee = true;
        if (kb.digit2Key.wasPressedThisFrame && ranged != null) holdingMelee = false;

        // V key or the mouse wheel: switch to the other weapon
        bool switchWeapon = kb.vKey.wasPressedThisFrame;
        if (Mathf.Abs(mouse.scroll.ReadValue().y) > 0.1f && Time.unscaledTime >= nextScrollTime)
        {
            switchWeapon = true;
            nextScrollTime = Time.unscaledTime + 0.15f;   // one switch per scroll, even on a smooth trackpad
        }
        if (switchWeapon) ToggleWeapon();

        // Right mouse: pick up a nearby weapon, otherwise throw the held one (not with a locked loadout)
        if (mouse.rightButton.wasPressedThisFrame && !CardManager.LoadoutLocked)
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
            if (!HasUnlimitedAmmo)
            {
                if (rangedAmmo <= 0) return;
                rangedAmmo--;
            }
            Shoot(aim);
        }

        // cards can speed up swings (melee) and shooting (ranged)
        float delay = holdingMelee
            ? w.fireDelay / CardManager.MeleeSpeedMultiplier
            : w.fireDelay / CardManager.FireRateMultiplier;
        nextFireTime = Time.time + delay;
    }

    // swaps between the melee and ranged weapon (does nothing if you only have one)
    void ToggleWeapon()
    {
        if (holdingMelee && ranged != null) holdingMelee = false;
        else if (!holdingMelee && melee != null) holdingMelee = true;
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

            // card: slots that are disabled can't take weapons
            if (p.weapon != null)
            {
                if (CardManager.MeleeDisabled && p.weapon.isMelee) continue;
                if (CardManager.RangedDisabled && !p.weapon.isMelee) continue;
            }

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

        // card: quieter shots
        Noise.Make(transform.position, w.noiseRadius * CardManager.NoiseMultiplier);

        // Wall right in front of the gun: the shot hits it, so don't spawn bullets past it
        Vector2 spawn = (Vector2)transform.position + aim * muzzleOffset;
        if (Physics2D.Linecast(transform.position, spawn, wallMask)) return;

        for (int i = 0; i < w.pellets; i++)
        {
            float angle = Random.Range(-w.spreadAngle / 2f, w.spreadAngle / 2f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * aim;

            Bullet bullet = Instantiate(w.bulletPrefab, spawn, Quaternion.identity);
            bullet.Launch(dir, w.bulletSpeed, w.bulletLifetime, w.damage, w.pierce, w.killsAnyEnemy);
        }
    }

    void Melee(Vector2 aim)
    {
        WeaponData w = melee;

        // card: longer reach
        float reach = CardManager.MeleeReachMultiplier;
        float radius = w.meleeRadius * reach;
        Vector2 center = (Vector2)transform.position + aim * (w.meleeRange * reach);

        // a weapon marked "Kills Any Enemy" (the Queen's sword) hurts every enemy type
        DamageType type = w.killsAnyEnemy ? DamageType.Special : DamageType.Melee;

        if (weaponVisual != null) weaponVisual.Swing(w);
        if (parry != null) parry.OnSwing();   // with "Parry On Swing" every swing parries
        if (showHitArea) ShowSlash(center, radius);

        foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
        {
            if (hit.gameObject == gameObject || hit.isTrigger) continue;
            hit.GetComponent<IDamageable>()?.TakeDamage(w.damage, type);
        }
    }

    // Brief white circle so you can see the swing's reach while tuning
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

    // ---------- Weapon UI (bottom left) ----------

    void OnGUI()
    {
        int size = Mathf.RoundToInt(Screen.height * 0.03f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold };

        string m = CardManager.MeleeDisabled ? "disabled" : (melee != null ? melee.weaponName : "-");

        string r;
        if (CardManager.RangedDisabled) r = "disabled";
        else if (ranged == null) r = "-";
        else if (HasUnlimitedAmmo) r = ranged.weaponName + "  unlimited";
        else r = $"{ranged.weaponName}  {rangedAmmo}/{ranged.maxAmmo}";

        float lineHeight = size * 1.5f;
        float x = Screen.height * 0.03f;
        float bottomLine = Screen.height - Screen.height * 0.03f - lineHeight;   // ranged sits lowest
        float topLine = bottomLine - lineHeight;                                 // melee just above it

        DrawHudLine(new Rect(x, topLine, 700, lineHeight), "1  Melee: " + m, holdingMelee, style);
        DrawHudLine(new Rect(x, bottomLine, 700, lineHeight), "2  Ranged: " + r, !holdingMelee && ranged != null, style);
    }

    // the weapon in your hand is bright, the other one is dimmed
    static void DrawHudLine(Rect rect, string text, bool active, GUIStyle style)
    {
        float shadow = Mathf.Max(2f, style.fontSize * 0.07f);

        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(rect.x + shadow, rect.y + shadow, rect.width, rect.height), (active ? "> " : "   ") + text, style);

        style.normal.textColor = active ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        GUI.Label(rect, (active ? "> " : "   ") + text, style);
    }
}