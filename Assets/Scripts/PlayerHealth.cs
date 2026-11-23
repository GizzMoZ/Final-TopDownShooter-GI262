using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [SerializeField] int maxHealth = 1; // 1 = die in one hit
    [SerializeField] Color deadColor = new Color(0.6f, 0.1f, 0.1f);

    int health;
    bool isDead;
    int extraHits;              // King: hits the crown can still absorb
    float invulnerableUntil;    // safe until this time (after the crown takes a hit)

    public bool IsDead => isDead;

    Rigidbody2D rb;
    SpriteRenderer sr;
    PlayerController controller;
    PlayerParry parry;
    Coroutine flashRoutine;

    void Awake()
    {
        Instance = this;

        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
        parry = GetComponent<PlayerParry>();

        ApplyCard();
    }

    // Reads the chosen card. Called when the scene loads and again right after a card is picked.
    public void ApplyCard()
    {
        extraHits = CardManager.ExtraHits;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // R restarts any time, dead or alive
        if (kb.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // Temporary test key
        if (!isDead && kb.gKey.wasPressedThisFrame)
            TakeDamage(1);
    }

    // attacker is the enemy that hit you (so a parry can stun it). It can be left out.
    public void TakeDamage(int amount, EnemyHealth attacker = null)
    {
        if (isDead) return;

        // King: right after the crown breaks you are safe for a moment
        if (Time.time < invulnerableUntil) return;

        // Queen: a well timed parry blocks the hit
        if (parry != null && parry.TryBlock(attacker)) return;

        // King: the crown takes the hit instead of you
        if (extraHits > 0)
        {
            extraHits--;
            invulnerableUntil = Time.time + CardManager.GraceTime;
            StartFlash(CardManager.GraceTime);
            return;
        }

        health -= amount;
        if (health <= 0) Die();
    }

    void StartFlash(float duration)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(duration));
    }

    // flickers while the player is safe
    IEnumerator FlashRoutine(float duration)
    {
        Color normal = sr.color;
        float end = Time.time + duration;

        while (Time.time < end && !isDead)
        {
            sr.color = new Color(normal.r, normal.g, normal.b, 0.3f);
            yield return new WaitForSeconds(0.07f);
            sr.color = normal;
            yield return new WaitForSeconds(0.07f);
        }

        if (!isDead) sr.color = normal;
    }

    void Die()
    {
        isDead = true;

        if (flashRoutine != null) StopCoroutine(flashRoutine);

        controller.enabled = false;
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (sr != null) sr.color = deadColor;
    }

    void OnGUI()
    {
        if (isDead)
        {
            var deadStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 36,
                fontStyle = FontStyle.Bold
            };
            deadStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 60),
                      "Press R to restart", deadStyle);
            return;
        }

        // King: shows whether the crown is still there
        if (CardManager.ExtraHits > 0)
        {
            var crown = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.UpperRight, fontSize = 20, fontStyle = FontStyle.Bold };
            crown.normal.textColor = extraHits > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(new Rect(Screen.width - 520, 84, 500, 30), extraHits > 0 ? "CROWN" : "CROWN BROKEN", crown);
        }
    }
}