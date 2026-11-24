using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] float reach = 1.8f;   // how close you must be to the doorway

    Camera cam;
    Door target;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        target = null;

        PlayerHealth hp = PlayerHealth.Instance;
        if (hp == null || hp.IsDead) return;

        float best = reach * reach;
        var doors = Door.All;
        for (int i = 0; i < doors.Count; i++)
        {
            float d = ((Vector2)transform.position - doors[i].DoorwayCenter).sqrMagnitude;
            if (d <= best)
            {
                best = d;
                target = doors[i];
            }
        }

        var kb = Keyboard.current;
        if (target != null && kb != null && kb.eKey.wasPressedThisFrame)
            target.Interact(transform.position);
    }

    void OnGUI()
    {
        PlayerHealth hp = PlayerHealth.Instance;
        if (hp == null || hp.IsDead) return;

        int size = Mathf.RoundToInt(Screen.height * 0.028f);

        if (target != null && cam != null)   // prompt
        {
            Vector3 screen = cam.WorldToScreenPoint(target.DoorwayCenter);

            var prompt = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = size, fontStyle = FontStyle.Bold };
            Rect r = new Rect(screen.x - 250f, Screen.height - screen.y - size * 3f, 500f, size * 1.6f);

            prompt.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), target.PromptText(), prompt);

            // white normally, light red when it is locked and you have no key
            prompt.normal.textColor = target.CanOpen ? Color.white : new Color(1f, 0.55f, 0.55f);
            GUI.Label(r, target.PromptText(), prompt);
        }

        // the keys you carry, bottom left above the other lines
        string list = string.Join("  ", PlayerKeys.All);
        if (list.Length > 0)
        {
            float lineHeight = Mathf.RoundToInt(Screen.height * 0.03f) * 1.5f;
            float x = Screen.height * 0.03f;
            float y = Screen.height - Screen.height * 0.03f - lineHeight * 4f;

            var keys = new GUIStyle(GUI.skin.label)
            { fontSize = Mathf.RoundToInt(Screen.height * 0.03f), fontStyle = FontStyle.Bold };

            keys.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(x + 2, y + 2, 700, lineHeight), "KEYS: " + list, keys);
            keys.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(x, y, 700, lineHeight), "KEYS: " + list, keys);
        }
    }
}