using UnityEngine;

namespace Game.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount,
                        DamageType type = DamageType.Generic,
                        GameObject source = null);
    }
}
