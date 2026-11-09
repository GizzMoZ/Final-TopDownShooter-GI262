using UnityEngine;
using UnityEngine.InputSystem;

public class Crosshair : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; 
    }

    void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void LateUpdate()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 pos = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        pos.z = 0f;
        transform.position = pos;
    }
}