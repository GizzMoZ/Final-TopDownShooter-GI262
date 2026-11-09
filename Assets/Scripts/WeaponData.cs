using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Handgun";
    public Color color = Color.white;   

    [Header("Ranged")]
    public Bullet bulletPrefab;
    public bool automatic = false;
    public int maxAmmo = 12;
    public int pellets = 1;
    public float spreadAngle = 0f;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 2f;

    [Header("Melee")]
    public bool isMelee = false;
    public float meleeRange = 0.8f;     
    public float meleeRadius = 0.7f;    

    [Header("Shared")]
    public float fireDelay = 0.3f;
    public int damage = 1;
}