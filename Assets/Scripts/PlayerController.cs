using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;

    Rigidbody2D rb;
    Camera cam;
    Vector2 moveInput;
    Vector2 mouseWorld;

    InputAction moveAction;
    InputAction lookAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", binding: "<Mouse>/position");
    }

    void OnEnable() { moveAction.Enable(); lookAction.Enable(); }
    void OnDisable() { moveAction.Disable(); lookAction.Disable(); }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        mouseWorld = cam.ScreenToWorldPoint(lookAction.ReadValue<Vector2>());
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;

        Vector2 dir = mouseWorld - rb.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rb.rotation = angle - 90f;
    }
}