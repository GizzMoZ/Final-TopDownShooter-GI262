using UnityEngine;

public static class CardManager   // Holds the chosen card for the whole play session. Scripts ask it for what the card changes.
{
    public static CardData Current { get; private set; }
    public static bool HasChosen => Current != null;

    // loadout
    public static bool MeleeDisabled => Current != null && Current.disableMelee;
    public static bool RangedDisabled => Current != null && Current.disableRanged;
    public static WeaponData GrantedWeapon => Current != null ? Current.grantedWeapon : null;
    public static WeaponData GrantedMeleeWeapon => Current != null ? Current.grantedMeleeWeapon : null;
    public static bool UnlimitedAmmo => Current != null && Current.unlimitedAmmo;
    public static bool LoadoutLocked => Current != null && Current.lockLoadout;

    // multipliers
    public static float MoveSpeedMultiplier => Current != null ? Current.moveSpeedMultiplier : 1f;
    public static float NoiseMultiplier => Current != null ? Current.noiseMultiplier : 1f;
    public static float MeleeReachMultiplier => Current != null ? Current.meleeReachMultiplier : 1f;
    public static float MeleeSpeedMultiplier => Current != null ? Mathf.Max(0.1f, Current.meleeSpeedMultiplier) : 1f;
    public static float FireRateMultiplier => Current != null ? Mathf.Max(0.1f, Current.fireRateMultiplier) : 1f;
    public static bool ThrownWeaponsKill => Current != null && Current.thrownWeaponsKill;

    // countdown
    public static bool HasCountdown => Current != null && Current.hasCountdown;

    // crown
    public static int ExtraHits => Current != null ? Current.extraHits : 0;
    public static float GraceTime => Current != null ? Mathf.Max(0.1f, Current.graceTime) : 1f;

    // parry
    public static bool HasParry => Current != null && Current.hasParry;

    public static void Choose(CardData card) { Current = card; }

    // call this at the start of a new chapter if you want the player to pick again
    public static void Clear() { Current = null; }

    // a fresh Play session always starts without a card
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { Current = null; }
}