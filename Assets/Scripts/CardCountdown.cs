using UnityEngine;

public class CardCountdown : MonoBehaviour   // Ace's countdown: the player dies when the timer reaches 0.
{
    float remaining;
    bool running;
    bool frozen;         // the floor is clear timer stop
    float resetFlash;    // the green flash after a kill

    void OnEnable() { EnemyHealth.Killed += OnKill; }
    void OnDisable() { EnemyHealth.Killed -= OnKill; }

    void Update()
    {
        CardData card = CardManager.Current;
        if (card == null || !card.hasCountdown)
        {
            running = false;
            return;
        }

        PlayerHealth player = PlayerHealth.Instance;
        if (player == null || player.IsDead) return;   // stops counting on death

        if (!running)
        {
            running = true;
            remaining = card.startTime;
            frozen = EnemyHealth.Alive == 0;   // for empty level
        }

        if (resetFlash > 0f) resetFlash -= Time.deltaTime;
        if (frozen || LevelClear.IsCleared) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            player.TakeDamage(9999);   // out of time
        }
    }

    void OnKill()
    {
        CardData card = CardManager.Current;
        if (card == null || !card.hasCountdown || !running || frozen) return;

        PlayerHealth player = PlayerHealth.Instance;
        if (player != null && player.IsDead) return;   // too late

        // that was the last enemy: freeze the timer where it is
        if (EnemyHealth.Alive <= 0)
        {
            frozen = true;
            return;
        }

        remaining = card.timeAfterKill;
        resetFlash = 0.3f;   // reset
    }

    void OnGUI()
    {
        if (!running) return;

        CardData card = CardManager.Current;
        float warnAt = card != null ? card.timeAfterKill * 0.5f : 1.5f;

        bool outOfTime = !frozen && remaining <= 0f;
        bool warning = !frozen && remaining <= warnAt;

        // the number: white, flashing red when low, green right after a kill
        Color numberColor = Color.white;
        if (resetFlash > 0f)
        {
            numberColor = new Color(0.4f, 1f, 0.4f);
        }
        else if (warning)
        {
            float flash = Mathf.PingPong(Time.unscaledTime * 6f, 1f);
            numberColor = Color.Lerp(new Color(1f, 0.2f, 0.2f), Color.white, outOfTime ? 0f : flash * 0.5f);
        }

        var caption = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.UpperCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.026f), fontStyle = FontStyle.Bold };
        caption.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

        var number = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.UpperCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.09f), fontStyle = FontStyle.Bold };
        number.normal.textColor = numberColor;

        float w = 400f;
        float x = (Screen.width - w) / 2f;
        GUI.Label(new Rect(x, 12, w, 36), "ADRENALINE", caption);
        GUI.Label(new Rect(x, 40, w, Screen.height * 0.12f), remaining.ToString("0.0"), number);

        // death message
        if (outOfTime)
        {
            var dead = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.08f), fontStyle = FontStyle.Bold };
            dead.normal.textColor = new Color(1f, 0.2f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, Screen.height * 0.15f), "OUT OF TIME", dead);
        }
    }
}