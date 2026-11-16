using System.Collections.Generic;
using UnityEngine;

// spawn with random weapon form pool
public class EnemyWeapon : MonoBehaviour
{
    [Header("Loadout")]
    [SerializeField] WeaponData[] weaponPool;      // empty = unarmed

    [Header("Look")]
    [SerializeField] bool showHeldWeapon = true;   // show the weapon on the enemy vusual

    WeaponData weapon;
    int ammo;
    HeldWeapon held;

    public WeaponData Weapon => weapon;
    public int Ammo => ammo;
    public bool HasAmmo => weapon != null && !weapon.isMelee && ammo > 0;
    public bool IsOutOfAmmo => weapon != null && !weapon.isMelee && ammo <= 0;

    void Awake()
    {
        if (showHeldWeapon) held = HeldWeapon.Create(transform);

        // pick one weapon from the pool (ignoring empty slots)
        var valid = new List<WeaponData>();
        if (weaponPool != null)
        {
            foreach (var w in weaponPool)
                if (w != null) valid.Add(w);
        }

        if (valid.Count > 0)
        {
            WeaponData chosen = valid[Random.Range(0, valid.Count)];
            Equip(chosen, chosen.isMelee ? 0 : chosen.maxAmmo);   // always a full magazine
        }
    }

    public void UseAmmo()
    {
        ammo = Mathf.Max(0, ammo - 1);
    }

    public void Equip(WeaponData newWeapon, int newAmmo)
    {
        weapon = newWeapon;
        ammo = newAmmo;
        if (held != null) held.Show(weapon);
    }

    // swing animation (used by EnemyAI)
    public void Raise() { if (held != null) held.Raise(weapon); }
    public void Swing() { if (held != null) held.Swing(weapon); }
    public void CancelSwing() { if (held != null) held.CancelSwing(); }

    // call when the enemy dies
    public void Hide()
    {
        if (held != null) held.Hide();
    }
}