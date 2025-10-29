// Assets/_Project/Scripts/Skills/Runtime/SkillActivator.cs
using UnityEngine;

namespace Game.Skills
{
    public class SkillActivator : MonoBehaviour, ISkillActivator
    {
        private IResourceProvider resourceProvider;
        private StatusEffectManager statusEffectManager;
        private PassiveSkillManager passiveManager;

        [SerializeField] private float globalCooldown = 0.5f;

        void Awake()
        {
            resourceProvider = GetComponent<IResourceProvider>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            passiveManager = GetComponent<PassiveSkillManager>();
        }

        public bool CanActivate(SkillInstance skill, SkillContext context)
        {
            if (skill == null || !skill.IsUnlocked || !skill.IsReady) return false;

            float cost = CalculateCost(skill);
            // 若沒有 CanAfford，可以改為嘗試 TryConsume 前先略過此檢查
            if (resourceProvider != null && !resourceProvider.CanAfford(cost)) return false;

            if (statusEffectManager != null && statusEffectManager.IsDisabled()) return false;

            var effects = skill.definition.baseEffects;
            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var eff = effects[i];
                    if (eff != null && !eff.CanExecute(context)) return false;
                }
            }
            return true;
        }

        public void Activate(SkillInstance skill, SkillContext context)
        {
            if (!CanActivate(skill, context)) return;

            float cost = CalculateCost(skill);
            // 介面只有 TryConsume 的情況
            if (resourceProvider != null)
            {
                // 若 TryConsume 回傳 false，就中止
                var tryConsumeMethod = resourceProvider.GetType().GetMethod("TryConsume");
                if (tryConsumeMethod != null)
                {
                    bool ok = (bool)tryConsumeMethod.Invoke(resourceProvider, new object[] { cost });
                    if (!ok) return;
                }
            }

            context.skillLevel = skill.currentLevel;
            context.parameters = skill.GetParameters();

            var effects = skill.definition.baseEffects;
            if (effects != null)
                for (int i = 0; i < effects.Count; i++) effects[i]?.OnActivate(context);

            var tier = skill.definition.GetTier(skill.currentLevel);
            if (tier?.additionalEffects != null)
                for (int i = 0; i < tier.additionalEffects.Count; i++) tier.additionalEffects[i]?.OnActivate(context);

            float cdModifier = GetCooldownModifier(skill);
            skill.StartCooldown(cdModifier, globalCooldown);

            SkillEvents.TriggerSkillActivated(skill, context);
            PlayEffects(skill, context);
        }

        public float GetCooldownModifier(SkillInstance skill)
        {
            float m = 1f;
            if (passiveManager != null) m *= passiveManager.GetGlobalCooldownModifier();
            if (statusEffectManager != null) m *= statusEffectManager.GetCooldownModifier();
            return m;
        }

        private float CalculateCost(SkillInstance skill)
        {
            var parameters = skill.GetParameters();
            float baseCost = skill.definition.baseManaCost;
            float reduction01 = parameters.Get("manaCostReduction", 0f);
            return Mathf.Max(0, baseCost * (1f - reduction01));
        }

        private void PlayEffects(SkillInstance skill, SkillContext context)
        {
            if (skill.definition.castingVFX != null)
                Instantiate(skill.definition.castingVFX, context.casterTransform.position, Quaternion.identity);

            if (skill.definition.castSound != null)
                AudioSource.PlayClipAtPoint(skill.definition.castSound, context.casterTransform.position);
        }
    }
}
