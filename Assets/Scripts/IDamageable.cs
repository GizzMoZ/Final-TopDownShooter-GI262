public enum DamageType { Melee, Ranged, Thrown }

public interface IDamageable
{
    void TakeDamage(int amount, DamageType type);
}