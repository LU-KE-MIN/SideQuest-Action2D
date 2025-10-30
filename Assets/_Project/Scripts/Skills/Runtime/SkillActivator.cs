// FILE: Assets/_Project/Scripts/Skills/Runtime/SkillActivator.cs
using UnityEngine;

namespace Game.Skills
{
    public class SkillActivator : MonoBehaviour, ISkillActivator
    {
        private IResourceProvider resourceProvider;
        private StatusEffectManager statusEffectManager;
        private PassiveSkillManager passiveManager;

        [Header("Cooldown")]
        [SerializeField] private float globalCooldown = 0.5f;

        [Header("Skill Executors")]
        [SerializeField] private ProjectileSpawner projectileSpawner;
        [SerializeField] private DashExecutor dashExecutor;
        [SerializeField] private Transform firePoint;

        void Awake()
        {
            resourceProvider = GetComponent<IResourceProvider>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            passiveManager = GetComponent<PassiveSkillManager>();

            if (firePoint == null)
                firePoint = transform;
        }

        public bool CanActivate(SkillInstance skill, SkillContext context)
        {
            Debug.Log($"[SkillActivator] CanActivate 檢查開始");
            if (skill == null || !skill.IsUnlocked)
            {
                Debug.LogWarning($"[SkillActivator] 基本檢查失敗"); // ← 加這行
                return false;
            }

            if (statusEffectManager != null && statusEffectManager.IsDisabled())
            {
                Debug.LogWarning("[SkillActivator] 狀態異常，無法施放");
                return false;
            }

            var effects = skill.definition.baseEffects;
            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var eff = effects[i];
                    if (eff != null && !eff.CanExecute(context))
                    {
                        Debug.LogWarning($"[SkillActivator] 效果 {i} 的 CanExecute 失敗");
                        return false;
                    }
                }
            }

            Debug.Log($"[SkillActivator] CanActivate 通過");
            return true;
        }

        public void Activate(SkillInstance skill, SkillContext context)
        {
            Debug.Log($"[SkillActivator] Activate 被調用，skillId: {skill.definition?.skillId}");
            if (!CanActivate(skill, context)) return;

            float cost = CalculateCost(skill);

            // 消耗資源
            if (resourceProvider != null)
            {
                var tryConsumeMethod = resourceProvider.GetType().GetMethod("TryConsume");
                if (tryConsumeMethod != null)
                {
                    bool ok = (bool)tryConsumeMethod.Invoke(resourceProvider, new object[] { cost });
                    if (!ok) return;
                }
            }

            context.skillLevel = skill.currentLevel;
            context.parameters = skill.GetParameters();

            // 執行特定技能邏輯（Fireball, Dash 等）
            ExecuteSkillLogic(skill, context);

            // 執行效果系統
            var effects = skill.definition.baseEffects;
            if (effects != null)
                for (int i = 0; i < effects.Count; i++)
                    effects[i]?.OnActivate(context);

            var tier = skill.definition.GetTier(skill.currentLevel);
            if (tier?.additionalEffects != null)
                for (int i = 0; i < tier.additionalEffects.Count; i++)
                    tier.additionalEffects[i]?.OnActivate(context);

            float cdModifier = GetCooldownModifier(skill);
            skill.StartCooldown(cdModifier, globalCooldown);

            SkillEvents.TriggerSkillActivated(skill, context);
            PlayEffects(skill, context);
        }

        private void ExecuteSkillLogic(SkillInstance skill, SkillContext context)
        {
            if (skill.definition == null)
            {
                Debug.LogError("[SkillActivator] skill.definition 是 null！"); // ← 加這行
                return;
            }

            string skillId = skill.definition.skillId.ToLower();
            Debug.Log($"[SkillActivator] ExecuteSkillLogic，skillId: '{skillId}'");
            
            switch (skillId)
            {
                case "fireball":
                    ExecuteFireball(skill, context);
                    break;

                case "dash":
                    ExecuteDash(skill, context);
                    break;

                // 其他技能可以繼續添加
                default:
                    Debug.LogWarning($"[SkillActivator] 沒有找到 '{skillId}' 的執行器"); // ← 加這行
                    break;
            }
        }

        private void ExecuteFireball(SkillInstance skill, SkillContext context)
        {
            Debug.Log("[SkillActivator] ExecuteFireball 被調用");
            if (projectileSpawner == null)
            {
                Debug.LogWarning("ProjectileSpawner not assigned for Fireball skill!");
                return;
            }

            Vector2 direction = context.direction;
            projectileSpawner.SpawnProjectile(firePoint.position, direction, skill);
        }

        private void ExecuteDash(SkillInstance skill, SkillContext context)
        {
            if (dashExecutor == null)
            {
                Debug.LogWarning("DashExecutor not assigned for Dash skill!");
                return;
            }

            Vector2 direction = context.direction;
            dashExecutor.ExecuteDash(direction, skill);
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