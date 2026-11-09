using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] WeaponData[] weapons;  // keys 1, 2, 3 test
    [SerializeField] float muzzleOffset = 0.6f;

    WeaponData current;
    Camera cam;
    float nextFireTime;

    void Awake()
    {
        cam = Camera.main;
        if (weapons.Length > 0) current = weapons[0];
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null || kb == null || current == null) return;

        // Switch weapons with number keys
        for (int i = 0; i < weapons.Length && i < 9; i++)
        {
            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                Equip(weapons[i]);
        }

        bool trigger = current.automatic
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;

        if (trigger && Time.time >= nextFireTime)
        {
            Shoot(mouse.position.ReadValue());
            nextFireTime = Time.time + current.fireDelay;
        }
    }

    // Call this later when the player picks up a weapon
    public void Equip(WeaponData weapon)
    {
        current = weapon;
    }

    void Shoot(Vector2 screenPos)
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(screenPos);
        Vector2 aim = mouseWorld - (Vector2)transform.position;
        if (aim.sqrMagnitude < 0.0001f) return;
        aim.Normalize();

        for (int i = 0; i < current.pellets; i++)
        {
            float angle = Random.Range(-current.spreadAngle / 2f, current.spreadAngle / 2f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * aim;

            Bullet bullet = Instantiate(current.bulletPrefab,
                                        (Vector2)transform.position + aim * muzzleOffset,
                                        Quaternion.identity);
            bullet.Launch(dir, current.bulletSpeed, current.bulletLifetime, current.damage);
        }
    }
}