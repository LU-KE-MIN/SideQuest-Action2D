using System;

public interface IResourceProvider
{
    event Action<float, float> OnResourceChanged;
    
    float CurrentResource { get; }
    float MaxResource { get; }
    bool CanAfford(float cost);
    bool TryConsume(float cost);
    void Restore(float amount);
    void ModifyMax(float amount);
}

public interface ISkillPointProvider
{
    event Action<int> OnPointsChanged;
    
    int AvailablePoints { get; }
    bool TrySpendPoints(int amount);
    void AddPoints(int amount);
}

public interface IDamageable
{
    event Action<float> OnDamageReceived;
    event Action OnDeath;
    
    float CurrentHealth { get; }
    float MaxHealth { get; }
    void TakeDamage(float amount, DamageType type = DamageType.Physical);
    void Heal(float amount);
    bool IsAlive { get; }
}

public enum DamageType
{
    Physical,
    Magical,
    True,
    Fire,
    Ice,
    Lightning
}