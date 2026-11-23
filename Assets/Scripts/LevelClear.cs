using UnityEngine;

public class LevelClear : MonoBehaviour  // Counts the enemies in the scene. When the last one dies, the floor is cleared.
{
    public static bool IsCleared { get; private set; }
    public static event System.Action Cleared;  // raised once when the last enemy dies

    [Header("Text")]
    [SerializeField] bool showText = true;
    [SerializeField] string message = "FLOOR CLEARED";
    [SerializeField] string subtitle = "";   // for example "Head for the exit"

    [Header("Debug")]
    [SerializeField] bool showEnemyCount;         

    public int TotalEnemies { get; private set; }  // count enemies at the start 
    public int EnemiesLeft => EnemyHealth.Alive;

    float clearedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { IsCleared = false; }

    void Awake()
    {
        IsCleared = false;   // pressing R reloads
    }

    void Start()
    {
        // every enemy has woken up by now, so this is the number on the floor
        TotalEnemies = EnemyHealth.Alive;
    }

    void OnEnable() { EnemyHealth.Killed += OnKill; }
    void OnDisable() { EnemyHealth.Killed -= OnKill; }

    void OnKill()
    {
        if (IsCleared) return;
        if (EnemyHealth.Alive > 0) return;   // still enemies left

        IsCleared = true;
        clearedAt = Time.unscaledTime;
        Cleared?.Invoke();
    }

    void OnGUI()
    {
        if (showEnemyCount)
        {
            var count = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            count.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 100, 400, 30), "Enemies: " + EnemiesLeft + " / " + TotalEnemies, count);
        }

        if (!IsCleared || !showText) return;

        float t = Time.unscaledTime - clearedAt;

        // pulse color
        float pop = Mathf.Clamp01(t / 0.25f);
        float scale = Mathf.Lerp(1.8f, 1f, 1f - (1f - pop) * (1f - pop));
        float pulse = 1f + 0.04f * Mathf.Sin(t * 8f);
        float alpha = Mathf.Clamp01(t / 0.15f);

        // colors cycling
        Color[] palette =
        {
            new Color(1f, 0.18f, 0.58f),
            new Color(0f, 0.9f, 1f),
            new Color(1f, 0.9f, 0f)
        };
        float k = Mathf.Repeat(t * 0.9f, palette.Length);
        int i = Mathf.FloorToInt(k);
        Color neon = Color.Lerp(palette[i], palette[(i + 1) % palette.Length], k - i);
        neon.a = alpha;

        int size = Mathf.RoundToInt(Screen.height * 0.12f * scale * pulse);
        var style = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontSize = size, fontStyle = FontStyle.BoldAndItalic };

        float bandHeight = Screen.height * 0.2f;
        float bandY = Screen.height * 0.28f;
        Rect textRect = new Rect(0, bandY, Screen.width, bandHeight);

        // dark band behind the text
        GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
        GUI.DrawTexture(new Rect(0, bandY + bandHeight * 0.1f, Screen.width, bandHeight * 0.8f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // shadow then the colored text
        float o = Mathf.Max(3f, Screen.height * 0.006f);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f * alpha);
        GUI.Label(new Rect(textRect.x + o, textRect.y + o, textRect.width, textRect.height), message, style);
        style.normal.textColor = neon;
        GUI.Label(textRect, message, style);

        if (!string.IsNullOrEmpty(subtitle))
        {
            var sub = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.UpperCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.04f), fontStyle = FontStyle.Bold };
            sub.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(0, bandY + bandHeight * 0.95f, Screen.width, Screen.height * 0.08f), subtitle, sub);
        }
    }
}