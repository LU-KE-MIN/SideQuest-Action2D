using UnityEngine;

namespace Game.Skills
{
    public class StatusEffectManager : MonoBehaviour
    {
        public bool IsDisabled() => false;
        public float GetCooldownModifier() => 1f;
    }
}
