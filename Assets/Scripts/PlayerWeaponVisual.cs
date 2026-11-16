using UnityEngine;

// Shows the weapon the player is holding and plays its swing
[RequireComponent(typeof(PlayerShooting))]
public class PlayerWeaponVisual : MonoBehaviour
{
    PlayerShooting shooting;
    HeldWeapon held;
    WeaponData shown;
    bool firstFrame = true;

    void Awake()
    {
        shooting = GetComponent<PlayerShooting>();
        held = HeldWeapon.Create(transform);
    }

    void LateUpdate()
    {
        WeaponData w = shooting.CurrentWeapon;
        if (!firstFrame && w == shown) return;   // only update when the weapon changes

        firstFrame = false;
        shown = w;
        held.Show(w);
    }

    public void Swing(WeaponData w)
    {
        held.Swing(w);
    }
}