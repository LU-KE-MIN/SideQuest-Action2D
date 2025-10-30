// FILE: Assets/_Project/Scripts/Skills/Effects/DashEffect.cs
using UnityEngine;
using Game.Skills;

namespace Game.Skills
{
    [CreateAssetMenu(fileName = "DashEffect", menuName = "Skills/Effects/Dash")]
    public class DashEffect : SkillEffectDefinition
    {
        [Header("Dash Settings")]
        public float baseDistance = 5f;
        public float baseDuration = 0.2f;
        public float baseInvulnerabilityTime = 0.2f;
        public bool passThroughEnemies = true;

        [Header("Visual")]
        public GameObject dashTrailPrefab;
        public int afterImageCount = 3;

        protected override void Execute(SkillContext context)
        {
            var executor = context.caster.GetComponent<DashExecutor>();
            if (executor == null)
            {
                executor = context.caster.AddComponent<DashExecutor>();
            }

            var parameters = context.parameters;
            float distance = parameters.Get(SkillParameterKeys.DashDistance, baseDistance);
            float duration = parameters.Get(SkillParameterKeys.Duration, baseDuration);
            float iFrameTime = parameters.Get(SkillParameterKeys.InvulnerabilityTime, baseInvulnerabilityTime);

            executor.PerformDash(
                context.direction,
                distance,
                duration,
                iFrameTime,
                passThroughEnemies,
                dashTrailPrefab,
                afterImageCount
            );
        }
    }
}