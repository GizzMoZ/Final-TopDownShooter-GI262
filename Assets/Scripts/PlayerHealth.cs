using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 1;
    [SerializeField] Color deadColor = new Color(0.6f, 0.1f, 0.1f);

    int health;
    bool isDead;

    Rigidbody2D rb;
    SpriteRenderer sr;
    PlayerController controller;

    void Awake()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Temporary test key until enemies exist
        if (!isDead && kb.gKey.wasPressedThisFrame)
            TakeDamage(1);

        if (isDead && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        controller.enabled = false;      // no more movement or aiming
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;            // body stays put, can't be hit again

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