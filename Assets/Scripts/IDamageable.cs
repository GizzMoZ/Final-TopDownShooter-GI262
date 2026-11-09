public enum DamageType { Melee, Ranged }

public interface IDamageable
{
    void TakeDamage(int amount, DamageType type);
}