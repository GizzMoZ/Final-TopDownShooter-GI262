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

    public bool IsDead => isDead;

    Rigidbody2D rb;
    SpriteRenderer sr;
    PlayerController controller;

    void Awake()
    {
        Instance = this;

        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame) // R restart
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (!isDead && kb.gKey.wasPressedThisFrame) // G kill yourself
            TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;
        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        controller.enabled = false;
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (sr != null) sr.color = deadColor;
    }

    void OnGUI()
    {
        if (!isDead) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 36,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 60),
                  "Press R to restart", style);
    }
}