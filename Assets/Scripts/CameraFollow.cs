using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Normal view (measured from the Hotline Miami 2 clip)")]
    [SerializeField] float normalLean = 0.12f;      
    [SerializeField] float verticalBias = 0.15f;   

    [Header("Hold Shift to pan")]
    [SerializeField] float shiftLean = 1.0f;       

    [Header("Smoothing")]
    [SerializeField] float followSmoothTime = 0.06f; 
    [SerializeField] float lookSmoothTime = 0.15f;  

    Camera cam;
    Transform target;
    Vector2 followPos, followVel;
    Vector2 lookCurrent, lookVel;
    bool snapped;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (PlayerHealth.Instance == null) return;
            target = PlayerHealth.Instance.transform;
        }

        float size = cam.orthographicSize;
        Vector2 lookGoal = Vector2.up * verticalBias * size;  

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        if (mouse != null)
        {
            bool shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            float lean = shift ? shiftLean : normalLean;

            float aspect = (float)Screen.width / Screen.height;
            Vector2 ratio = (mouse.position.ReadValue() - new Vector2(Screen.width, Screen.height) * 0.5f)
                            / (Screen.height * 0.5f);
            ratio.x = Mathf.Clamp(ratio.x, -aspect, aspect);
            ratio.y = Mathf.Clamp(ratio.y, -1f, 1f);

            lookGoal += ratio * size * lean;
        }

        // snap to player at start
        if (!snapped)
        {
            followPos = target.position;
            lookCurrent = lookGoal;
            snapped = true;
        }

        // smoothing camera
        followPos = Vector2.SmoothDamp(followPos, target.position, ref followVel, followSmoothTime);
        lookCurrent = Vector2.SmoothDamp(lookCurrent, lookGoal, ref lookVel, lookSmoothTime);

        Vector2 pos = followPos + lookCurrent;
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }
}