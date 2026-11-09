using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Handgun";
    public Bullet bulletPrefab;
    public bool automatic = false;      // hold to keep firing
    public float fireDelay = 0.3f;      // delay between shots
    public int pellets = 1;             // shotgun spread
    public float spreadAngle = 0f;      // shotgun spread angle
    public float bulletSpeed = 20f;
    public float bulletLifetime = 2f;
    public int damage = 1;
}