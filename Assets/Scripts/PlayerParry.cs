using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Queen's parry. It only works inside the area the sword swings through, in front of the player.
// Space performs a parry swing (or every normal swing parries, if the card says so).
// One bullet inside the area is reflected, or a melee attacker inside it is blocked and stunned.
public class PlayerParry : MonoBehaviour
{
    [SerializeField] float hitStopTime = 0.07f;   // the short freeze when a parry works

    const float AttackerRadius = 0.5f;            // how big an enemy counts as when checking the area

    Camera cam;
    PlayerShooting shooting;
    PlayerWeaponVisual weaponVisual;
    SpriteRenderer zoneRing;  // swing area

    bool windowOpen;
    bool swingMode;       // opened by a normal swing (no miss cooldown) instead of Space
    float windowEnd;
    float readyAt;

    Vector2 zoneCenter;
    float zoneRadius;

    bool popping;
    bool hitStopping;

    void Awake()
    {
        cam = Camera.main;
        shooting = GetComponent<PlayerShooting>();
        weaponVisual = GetComponent<PlayerWeaponVisual>();

        // the ring is not a child, so it stays in the world while the player turns
        var go = new GameObject("ParryZone");
        zoneRing = go.AddComponent<SpriteRenderer>();
        zoneRing.sprite = RingSprite();
        zoneRing.enabled = false;

        var playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            zoneRing.sortingLayerID = playerSprite.sortingLayerID;
            zoneRing.sortingOrder = playerSprite.sortingOrder + 4;
        }
    }

    void OnDestroy()
    {
        if (zoneRing != null) Destroy(zoneRing.gameObject);
    }

    void OnDisable()
    {
        // never leave the game frozen
        if (hitStopping)
        {
            Time.timeScale = 1f;
            hitStopping = false;
        }
    }

    void Update()
    {
        CardData card = CardManager.Current;
        PlayerHealth hp = PlayerHealth.Instance;

        if (card == null || !card.hasParry || hp == null || hp.IsDead)
        {
            windowOpen = false;
            if (!popping) zoneRing.enabled = false;
            return;
        }

        // Space: a parry swing (not used when every swing parries)
        var kb = Keyboard.current;
        if (!card.parryOnSwing && kb != null && kb.spaceKey.wasPressedThisFrame && !windowOpen && Time.time >= readyAt)
        {
            OpenWindow(card, false);
            if (weaponVisual != null) weaponVisual.Swing(GetSword());   // the sword swings, so you see the parry
        }

        UpdateZone();

        if (windowOpen)
        {
            if (TryParryBullet(card))
            {
                OnSuccess();
            }
            else if (Time.time >= windowEnd)
            {
                // nothing arrived in time
                windowOpen = false;
                if (!swingMode) readyAt = Time.time + card.parryMissCooldown;
            }
        }

        if (!popping)
        {
            zoneRing.enabled = windowOpen;
            if (windowOpen)
            {
                zoneRing.color = new Color(0.4f, 0.9f, 1f, 0.7f);
                zoneRing.transform.position = zoneCenter;
                zoneRing.transform.localScale = Vector3.one * zoneRadius * 2f;
            }
        }
    }

    // Called by PlayerShooting every time the player swings. Only matters if the card parries on every swing.
    public void OnSwing()
    {
        CardData card = CardManager.Current;
        if (card == null || !card.hasParry || !card.parryOnSwing) return;

        OpenWindow(card, true);
    }

    void OpenWindow(CardData card, bool fromSwing)
    {
        windowOpen = true;
        swingMode = fromSwing;
        windowEnd = Time.time + card.parryWindow;
    }

    // Called by PlayerHealth when something hurts the player. True = blocked.
    public bool TryBlock(EnemyHealth attacker)
    {
        CardData card = CardManager.Current;
        if (card == null || !card.hasParry || !windowOpen || Time.time > windowEnd) return false;
        if (attacker == null) return false;

        // the attacker has to be inside the swing area
        UpdateZone();
        float d = Vector2.Distance(attacker.transform.position, zoneCenter);
        if (d > zoneRadius + AttackerRadius) return false;

        attacker.Stun(card.parryStunTime);
        OnSuccess();
        return true;
    }

    // reflects the nearest enemy bullet inside the swing area toward the mouse (one bullet per parry)
    bool TryParryBullet(CardData card)
    {
        EnemyBullet best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(zoneCenter, zoneRadius))
        {
            var b = hit.GetComponent<EnemyBullet>();
            if (b == null || b.IsReflected) continue;

            float d = ((Vector2)b.transform.position - zoneCenter).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = b; }
        }
        if (best == null) return false;

        best.Reflect(AimDirection(), card.reflectSpeed, 3f);
        return true;
    }

    // the swing area: a circle in front of the player, the same as the sword's hit area
    void UpdateZone()
    {
        WeaponData sword = GetSword();
        float range = sword != null ? sword.meleeRange : 1f;
        float radius = sword != null ? sword.meleeRadius : 0.9f;
        float reach = CardManager.MeleeReachMultiplier;

        Vector2 facing = transform.up;
        zoneCenter = (Vector2)transform.position + facing * (range * reach);
        zoneRadius = radius * reach;
    }

    WeaponData GetSword()
    {
        WeaponData w = CardManager.GrantedMeleeWeapon;
        if (w == null && shooting != null && shooting.CurrentWeapon != null && shooting.CurrentWeapon.isMelee)
            w = shooting.CurrentWeapon;
        return w;
    }

    Vector2 AimDirection()
    {
        var mouse = Mouse.current;
        if (mouse == null || cam == null) return transform.up;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 aim = mouseWorld - (Vector2)transform.position;
        return aim.sqrMagnitude < 0.0001f ? (Vector2)transform.up : aim.normalized;
    }

    void OnSuccess()
    {
        windowOpen = false;
        readyAt = 0f;   // a good parry resets the cooldown at once

        StartCoroutine(PopRing(zoneCenter, zoneRadius));
        StartCoroutine(HitStop());
    }

    IEnumerator HitStop()
    {
        hitStopping = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;
        hitStopping = false;
    }

    // the ring flashes white and fades where the parry happened
    IEnumerator PopRing(Vector2 center, float radius)
    {
        popping = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.18f;
            float c = Mathf.Clamp01(t);

            zoneRing.enabled = true;
            zoneRing.color = new Color(1f, 1f, 1f, 1f - c);
            zoneRing.transform.position = center;
            zoneRing.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(1f, 1.4f, c);
            yield return null;
        }
        popping = false;
        zoneRing.enabled = false;
    }

    static Sprite ringSprite;

    static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;

        const int size = 128;
        var tex = new Texture2D(size, size);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(dist - (r - 3f)) / 2.5f);   // a thin ring
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();

        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }

    // ---------- Parry UI (bottom left, above the weapon lines) ----------

    void OnGUI()
    {
        CardData card = CardManager.Current;
        if (card == null || !card.hasParry || card.parryOnSwing) return;   // nothing to show when every swing parries

        PlayerHealth hp = PlayerHealth.Instance;
        if (hp == null || hp.IsDead) return;

        int size = Mathf.RoundToInt(Screen.height * 0.03f);
        float lineHeight = size * 1.5f;
        float x = Screen.height * 0.03f;
        float y = Screen.height - Screen.height * 0.03f - lineHeight * 3f;

        string text;
        bool ready = !windowOpen && Time.time >= readyAt;
        if (windowOpen) text = "PARRY!";
        else if (ready) text = "SPACE  Parry: ready";
        else text = "SPACE  Parry: " + (readyAt - Time.time).ToString("0.0");

        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold };
        float shadow = Mathf.Max(2f, size * 0.07f);

        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(x + shadow, y + shadow, 700, lineHeight), text, style);

        style.normal.textColor = windowOpen ? new Color(0.4f, 0.9f, 1f)
                               : ready ? new Color(0.5f, 1f, 0.8f)
                               : new Color(1f, 1f, 1f, 0.5f);
        GUI.Label(new Rect(x, y, 700, lineHeight), text, style);
    }
}