using UnityEngine;

namespace Game.Skills
{
    [CreateAssetMenu(fileName = "FireballEffect", menuName = "Skills/Effects/Fireball")]
    public class FireballEffect : SkillEffectDefinition
    {
        [Header("Projectile")]
        public GameObject projectilePrefab;
        public float baseSpeed = 10f;
        public float baseDamage = 20f;
        public float baseLifetime = 5f;
        public int baseProjectileCount = 1;
        public float spreadAngle = 15f;

        [Header("Impact")]
        public float explosionRadius = 0f;
        public GameObject explosionVFX;

        protected override void Execute(SkillContext context)
        {
            var spawner = context.caster.GetComponent<ProjectileSpawner>();
            if (spawner == null) return;

            var parameters = context.parameters;
            float speed = parameters.Get(SkillParameterKeys.Speed, baseSpeed);
            float damage = parameters.Get(SkillParameterKeys.Damage, baseDamage);
            float lifetime = parameters.Get(SkillParameterKeys.Duration, baseLifetime);
            int count = Mathf.RoundToInt(parameters.Get(SkillParameterKeys.ProjectileCount, baseProjectileCount));

            if (count == 1)
            {
                spawner.SpawnProjectile(
                    projectilePrefab,
                    context.casterTransform.position,
                    context.direction,
                    speed,
                    damage,
                    lifetime,
                    explosionRadius,
                    explosionVFX
                );
            }
            else
            {
                float angleStep = spreadAngle / (count - 1);
                float startAngle = -spreadAngle / 2f;

                for (int i = 0; i < count; i++)
                {
                    float angle = startAngle + angleStep * i;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * context.direction;

                    spawner.SpawnProjectile(
                        projectilePrefab,
                        context.casterTransform.position,
                        direction,
                        speed,
                        damage,
                        lifetime,
                        explosionRadius,
                        explosionVFX
                    );
                }
            }
        }
    }
}
