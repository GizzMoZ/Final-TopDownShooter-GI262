using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    public string cardName = "Card";
    [TextArea(2, 4)] public string description;
    public Sprite sprite;              
    public Color color = Color.white;        // if not sprite

    [Header("Loadout")]
    public bool disableMelee;                // the melee slot cannot be used
    public bool disableRanged;               // the ranged slot cannot be used
    public WeaponData grantedWeapon;         // replaces the ranged weapon
    public WeaponData grantedMeleeWeapon;    // replaces the melee weapon
    public bool unlimitedAmmo;               // the granted ranged weapon never runs out of ammo
    public bool lockLoadout;                 // no picking up, throwing or swapping weapons

    [Header("Passive multipliers (1 = no change)")]
    public float moveSpeedMultiplier = 1f;
    public float noiseMultiplier = 1f;       // gunshot noise radius (smaller = enemies hear less)
    public float meleeReachMultiplier = 1f;  // melee range and hit size
    public float meleeSpeedMultiplier = 1f;  // higher = faster melee swings
    public float fireRateMultiplier = 1f;    // higher = faster shooting
    public bool thrownWeaponsKill;           // every thrown weapon kills instead of stunning

    [Header("Countdown (Ace)")]
    public bool hasCountdown;                // the player dies when the timer reaches 0
    public float startTime = 10f;            // seconds at the start, before the first kill
    public float timeAfterKill = 3f;         // every kill sets the timer to exactly this (it does not add up)

    [Header("Crown (King)")]
    public int extraHits;                    // hits that are absorbed before you die
    public float graceTime = 1f;             // seconds of safety after the crown takes a hit

    [Header("Parry (Queen)")]
    public bool hasParry;                    // the sword can parry
    public bool parryOnSwing;                // on = every normal swing parries and Space isn't used
    public float parryWindow = 0.25f;        // how long the parry stays active
    public float parryMissCooldown = 1.2f;   // wait after a Space parry that hit nothing
    public float parryStunTime = 2f;         // how long a parried melee attacker is stunned
    public float reflectSpeed = 28f;         // speed of a reflected bullet
}