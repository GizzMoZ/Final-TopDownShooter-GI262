using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Handgun";

    [Header("Look")]
    public Sprite sprite;                 // shown on the floor and in a character's hand
    public float spriteLength = 1f;       // how long the weapon looks in the game (world units, the player is about 1 wide)
    public float spriteRotation = 90f;    // in a hand: 90 if the sprite is drawn pointing right, -90 if left, 0 if up, 180 if down
    public Color color = Color.white;     // fallback bar color while there is no sprite
    public Vector2 holdOffset = new Vector2(0.25f, 0.5f);   // where it sits on a character: x = right, y = forward

    [Header("Ranged")]
    public Bullet bulletPrefab;
    public bool automatic = false;
    public int maxAmmo = 12;
    public int pellets = 1;
    public float spreadAngle = 0f;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 2f;
    public float noiseRadius = 12f;       // how far a shot can be heard
    public bool pierce;                   // bullets fly through enemies (walls still stop them)
    public bool killsAnyEnemy;            // bullets kill every enemy type, even ones immune to ranged hits

    [Header("Melee")]
    public bool isMelee = false;
    public float meleeRange = 0.8f;
    public float meleeRadius = 0.7f;

    [Header("Swing (melee animation)")]
    public float swingArc = 120f;         // degrees the weapon sweeps across the front. 0 = no sweep (use Thrust instead)
    public float swingThrust = 0f;        // how far the weapon pushes forward during the swing (knife stab)
    public float swingTime = 0.15f;       // how long the swing takes, in seconds
    public Color slashColor = new Color(1f, 1f, 1f, 0.7f);   // color of the slash trail (alpha 0 = no trail)

    [Header("Throwing")]
    public bool killsWhenThrown = false;  // true = kills on a thrown hit (knife)
    public float stunDuration = 2f;       // otherwise the thrown hit stuns this long

    [Header("Shared")]
    public float fireDelay = 0.3f;
    public int damage = 1;
}