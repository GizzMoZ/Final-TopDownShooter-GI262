public enum DamageType { Melee, Ranged, Thrown, Special }   // Special = kills any enemy type

public interface IDamageable
{
    void TakeDamage(int amount, DamageType type);
}