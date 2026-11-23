using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CardSelection : MonoBehaviour   // Shows the card pick screen when the game starts.
{
    [SerializeField] CardData[] cards;
    [SerializeField] bool showCardHud = true;   // small label in the top right with the card you picked

    bool selecting;
    int hovered = -1;

    PlayerController controller;
    PlayerShooting shooting;

    void Start()
    {
        if (CardManager.HasChosen) return;  // the card stays chosen when you restart with R

        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning("CardSelection: no cards assigned, so the game starts without one.", this);
            return;
        }

        var player = PlayerHealth.Instance;
        if (player != null)
        {
            controller = player.GetComponent<PlayerController>();
            shooting = player.GetComponent<PlayerShooting>();
        }

        selecting = true;
        Time.timeScale = 0f;  // freeze the world while choosing
        if (controller != null) controller.enabled = false;
        if (shooting != null) shooting.enabled = false;
    }

    void Update()
    {
        if (!selecting) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        // mouse: hover and click
        hovered = -1;
        if (mouse != null)
        {
            Vector2 m = mouse.position.ReadValue();
            m.y = Screen.height - m.y;   // screen y starts at the bottom, GUI y at the top

            for (int i = 0; i < cards.Length; i++)
                if (cards[i] != null && BaseRect(i).Contains(m)) hovered = i;

            if (hovered >= 0 && mouse.leftButton.wasPressedThisFrame)
            {
                Pick(hovered);
                return;
            }
        }

        // keyboard: 1 to 4
        if (kb != null)
        {
            KeyControl[] keys = { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key };
            for (int i = 0; i < keys.Length && i < cards.Length; i++)
            {
                if (keys[i].wasPressedThisFrame)
                {
                    Pick(i);
                    return;
                }
            }
        }
    }

    void Pick(int index)
    {
        if (index < 0 || index >= cards.Length || cards[index] == null) return;

        CardManager.Choose(cards[index]);
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        selecting = false;
        Time.timeScale = 1f;

        if (shooting != null) shooting.ApplyCard();                                     // existing
        if (PlayerHealth.Instance != null) PlayerHealth.Instance.ApplyCard();           // new: the Crown

        // wait one frame so the click that picked the card doesn't also fire a shot
        yield return null;

        if (controller != null) controller.enabled = true;
        if (shooting != null) shooting.enabled = true;
    }

    // ---------- Drawing ----------

    // where card i sits (used for clicking)
    Rect BaseRect(int i)
    {
        float h = Mathf.Min(Screen.height * 0.55f, 520f);
        float w = h * 0.7f;
        float gap = w * 0.12f;
        float total = cards.Length * w + (cards.Length - 1) * gap;

        float x = (Screen.width - total) / 2f + i * (w + gap);
        float y = Screen.height * 0.5f - h / 2f;
        return new Rect(x, y, w, h);
    }

    void OnGUI()
    {
        if (!selecting)
        {
            DrawHud();
            return;
        }

        int body = Mathf.RoundToInt(Screen.height * 0.022f);
        var title = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.06f), fontStyle = FontStyle.Bold };
        var nameStyle = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.034f), fontStyle = FontStyle.Bold };
        var desc = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.UpperCenter, fontSize = body, wordWrap = true };
        var hint = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontSize = body };
        var number = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.UpperLeft, fontSize = Mathf.RoundToInt(Screen.height * 0.03f), fontStyle = FontStyle.Bold };
        title.normal.textColor = nameStyle.normal.textColor = desc.normal.textColor =
            hint.normal.textColor = number.normal.textColor = Color.white;

        // dim the whole screen
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, Screen.height * 0.07f, Screen.width, Screen.height * 0.1f), "CHOOSE YOUR CARD", title);

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;

            Rect r = BaseRect(i);
            if (i == hovered) r.y -= r.height * 0.03f;   // lift the card under the mouse
            DrawCard(cards[i], r, i == hovered, i + 1, nameStyle, desc, number);
        }

        GUI.Label(new Rect(0, Screen.height * 0.88f, Screen.width, 40), "Click a card or press 1 - " + cards.Length, hint);

        // our own pointer, since the game's crosshair is hidden behind this screen
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 m = mouse.position.ReadValue();
            m.y = Screen.height - m.y;
            GUI.DrawTexture(new Rect(m.x - 10, m.y - 1, 20, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(m.x - 1, m.y - 10, 2, 20), Texture2D.whiteTexture);
        }
    }

    void DrawCard(CardData card, Rect rect, bool hover, int slot, GUIStyle nameStyle, GUIStyle desc, GUIStyle number)
    {
        // border
        GUI.color = hover ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        GUI.DrawTexture(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), Texture2D.whiteTexture);

        // card body
        GUI.color = new Color(0.12f, 0.12f, 0.14f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // art
        Rect art = new Rect(rect.x + 12, rect.y + 12, rect.width - 24, rect.height * 0.5f);
        if (card.sprite != null)
        {
            GUI.color = Color.white;
            DrawSprite(art, card.sprite);
        }
        else
        {
            GUI.color = card.color;
            GUI.DrawTexture(art, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;

        GUI.Label(new Rect(rect.x, art.yMax + 6, rect.width, rect.height * 0.12f), card.cardName, nameStyle);
        GUI.Label(new Rect(rect.x + 14, art.yMax + rect.height * 0.14f, rect.width - 28, rect.yMax - art.yMax - rect.height * 0.16f),
                  card.description, desc);
        GUI.Label(new Rect(rect.x + 10, rect.y + 6, 60, 40), slot.ToString(), number);
    }

    // draws a sprite (even one cut out of a sprite sheet), fitted inside the area
    static void DrawSprite(Rect area, Sprite s)
    {
        Texture2D tex = s.texture;
        Rect r = s.textureRect;
        Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);

        float aspect = r.width / r.height;
        float w = area.width, h = w / aspect;
        if (h > area.height) { h = area.height; w = h * aspect; }

        Rect dest = new Rect(area.x + (area.width - w) / 2f, area.y + (area.height - h) / 2f, w, h);
        GUI.DrawTextureWithTexCoords(dest, tex, uv);
    }

        void DrawHud()
    {
        if (!showCardHud || CardManager.Current == null) return;

        CardData c = CardManager.Current;

        var style = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.UpperRight, fontSize = 22, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width - 520, 20, 500, 36), "Card: " + c.cardName, style);

        var small = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.UpperRight, fontSize = 16 };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
        GUI.Label(new Rect(Screen.width - 520, 52, 500, 28), Summary(c), small);
    }

    // lists only the multipliers that differ from normal, so you can see what the card really does
    static string Summary(CardData c)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!Mathf.Approximately(c.moveSpeedMultiplier, 1f))  parts.Add("speed x" + c.moveSpeedMultiplier.ToString("0.##"));
        if (!Mathf.Approximately(c.meleeSpeedMultiplier, 1f)) parts.Add("swing x" + c.meleeSpeedMultiplier.ToString("0.##"));
        if (!Mathf.Approximately(c.fireRateMultiplier, 1f))   parts.Add("fire rate x" + c.fireRateMultiplier.ToString("0.##"));
        if (!Mathf.Approximately(c.meleeReachMultiplier, 1f)) parts.Add("reach x" + c.meleeReachMultiplier.ToString("0.##"));
        if (!Mathf.Approximately(c.noiseMultiplier, 1f))      parts.Add("noise x" + c.noiseMultiplier.ToString("0.##"));
        return parts.Count == 0 ? "no speed changes" : string.Join("   ", parts);
    }
}