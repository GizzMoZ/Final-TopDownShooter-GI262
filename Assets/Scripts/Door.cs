using System.Collections.Generic;
using UnityEngine;

// A swinging door opened with E. Put this on an empty GameObject placed at the HINGE (one end of the gap).
// The door extends along the object's right (red) arrow, so rotate Z by 90 for a vertical gap.
// It builds its own door leaf, so there is nothing else to set up.
[RequireComponent(typeof(Rigidbody2D))]
public class Door : MonoBehaviour
{
    static readonly List<Door> all = new List<Door>();
    public static IReadOnlyList<Door> All => all;   // every door in the scene

    [Header("Size (1 = one tile)")]
    [SerializeField] float length = 1f;
    [SerializeField] float thickness = 0.18f;
    [SerializeField] Sprite leafSprite;                     // optional art, drawn along the door's length
    [SerializeField] Color doorColor = new Color(0.55f, 0.36f, 0.2f);
    [SerializeField] Color lockedColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] int sortingOrder = 2;

    [Header("Lock")]
    [SerializeField] string requiredKeyId = "";             // empty = not locked. Must match a key's Key Id exactly
    [SerializeField] bool keyIsUsedUp = true;

    [Header("Swing")]
    [SerializeField] float openAngle = 100f;                // how far it opens (up to about 170)
    [SerializeField] float swingTime = 0.12f;               // lower = snappier
    [SerializeField] bool enemiesOpen = true;               // enemies open a closed door by walking into it

    public bool IsLocked => locked;
    public bool IsOpen => targetAngle != 0f;
    public bool CanOpen => !locked || PlayerKeys.Has(requiredKeyId);

    // the middle of the doorway when the door is shut. It never moves, so a prompt can sit here.
    public Vector2 DoorwayCenter
    {
        get
        {
            float z = Application.isPlaying ? closedZ : transform.eulerAngles.z;
            Vector2 along = Quaternion.Euler(0f, 0f, z) * Vector2.right;
            return (Vector2)transform.position + along * (length * 0.5f);
        }
    }

    Rigidbody2D rb;
    SpriteRenderer sr;

    float closedZ;
    float angle;          // where the door is now (degrees from shut)
    float targetAngle;    // where it is going: 0 = shut
    float angVel;

    bool locked;
    float blockedUntil;   // the door waits while it is touching someone it would swing into
    float rattleTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (GetComponentInChildren<Collider2D>() == null) BuildLeaf();
        sr = GetComponentInChildren<SpriteRenderer>();

        closedZ = transform.eulerAngles.z;
        locked = !string.IsNullOrEmpty(requiredKeyId);
        UpdateTint();
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void OnValidate()
    {
        length = Mathf.Max(0.2f, length);
        openAngle = Mathf.Clamp(openAngle, 10f, 170f);
    }

    // ---------- Building the door ----------

    void BuildLeaf()
    {
        var go = new GameObject("Leaf");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(length * 0.5f, 0f, 0f);

        // on the Walls layer so a closed door blocks bullets and enemy sight
        int walls = LayerMask.NameToLayer("Walls");
        if (walls >= 0) go.layer = walls;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (leafSprite != null)
        {
            renderer.sprite = leafSprite;
            float s = length / Mathf.Max(0.0001f, leafSprite.bounds.size.x);
            go.transform.localScale = new Vector3(s, s, 1f);
        }
        else
        {
            renderer.sprite = SquareSprite();
            go.transform.localScale = new Vector3(length, thickness, 1f);
        }

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(length / go.transform.localScale.x, thickness / go.transform.localScale.y);
    }

    static Sprite squareSprite;

    static Sprite SquareSprite()
    {
        if (squareSprite != null) return squareSprite;

        var tex = new Texture2D(4, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();

        squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return squareSprite;
    }

    void UpdateTint()
    {
        if (sr == null) return;

        Color unlocked = leafSprite != null ? Color.white : doorColor;
        sr.color = locked ? lockedColor : unlocked;
    }

    // ---------- Swinging ----------

    void FixedUpdate()
    {
        // a locked door never moves, but it rattles when you try it
        if (locked)
        {
            float wobble = 0f;
            if (rattleTimer > 0f)
            {
                rattleTimer -= Time.fixedDeltaTime;
                wobble = Mathf.Sin(Time.time * 60f) * 2.5f * Mathf.Clamp01(rattleTimer / 0.25f);
            }
            rb.MoveRotation(closedZ + wobble);
            return;
        }

        if (Time.time < blockedUntil)
        {
            angVel = 0f;   // someone is in the way: wait
        }
        else
        {
            // a smooth swing with no bounce
            angle = Mathf.SmoothDamp(angle, targetAngle, ref angVel, swingTime, Mathf.Infinity, Time.fixedDeltaTime);
            if (Mathf.Abs(targetAngle - angle) < 0.05f)
            {
                angle = targetAngle;
                angVel = 0f;
            }
        }

        rb.MoveRotation(closedZ + angle);
    }

    void OnCollisionEnter2D(Collision2D c) { Touch(c); }
    void OnCollisionStay2D(Collision2D c) { Touch(c); }

    void Touch(Collision2D c)
    {
        if (locked) return;

        Rigidbody2D other = c.rigidbody;
        if (other == null) return;

        bool isEnemy = other.GetComponent<EnemyHealth>() != null;
        bool isPlayer = other.GetComponent<PlayerHealth>() != null;
        if (!isEnemy && !isPlayer) return;

        float side = SideOf(other.position);

        // an enemy walking into a shut door opens it, away from itself
        if (isEnemy && enemiesOpen && targetAngle == 0f && Mathf.Abs(angle) < 1f)
        {
            targetAngle = -side * openAngle;
            return;
        }

        // the door never pushes anyone: if it is swinging into this character, it waits
        if (!Mathf.Approximately(angle, targetAngle))
        {
            float leafSide = Mathf.Sign(Mathf.Abs(angle) > 0.5f ? angle : targetAngle);
            if (Mathf.Approximately(side, leafSide)) blockedUntil = Time.time + 0.1f;
        }
    }

    // +1 or -1: which side of the shut door this point is on
    float SideOf(Vector2 point)
    {
        Vector2 normal = Quaternion.Euler(0f, 0f, closedZ) * Vector2.up;
        float d = Vector2.Dot(point - (Vector2)transform.position, normal);
        return d >= 0f ? 1f : -1f;
    }

    // ---------- Pressing E ----------

    public string PromptText()
    {
        if (locked)
            return PlayerKeys.Has(requiredKeyId) ? "E  Unlock" : "Locked  (needs " + requiredKeyId + " key)";

        return IsOpen ? "E  Close door" : "E  Open door";
    }

    public void Interact(Vector2 from)
    {
        if (locked)
        {
            if (!PlayerKeys.Has(requiredKeyId))
            {
                rattleTimer = 0.25f;   // no key: it just rattles
                return;
            }

            if (keyIsUsedUp) PlayerKeys.Use(requiredKeyId);
            locked = false;
            UpdateTint();
            targetAngle = -SideOf(from) * openAngle;   // unlocking opens it
            return;
        }

        if (IsOpen) targetAngle = 0f;                          // close it
        else targetAngle = -SideOf(from) * openAngle;          // open it, away from the player
    }

    // ---------- Scene view helper ----------

    void OnDrawGizmos()
    {
        bool lockedPreview = Application.isPlaying ? locked : !string.IsNullOrEmpty(requiredKeyId);
        Gizmos.color = lockedPreview ? new Color(1f, 0.3f, 0.3f) : new Color(0.95f, 0.75f, 0.3f);

        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, 0f, transform.eulerAngles.z), Vector3.one);
        Gizmos.DrawWireSphere(Vector3.zero, 0.08f);   // the hinge
        Gizmos.DrawWireCube(new Vector3(length * 0.5f, 0f, 0f), new Vector3(length, thickness, 0.01f));
    }
}