// FILE: Assets/_Project/Scripts/Skills/Runtime/ProjectileSpawner.cs
using UnityEngine;
using Game.Combat;
using CombatDamageType = Game.Combat.DamageType;

namespace Game.Skills
{
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject defaultProjectilePrefab;

        [Header("Defaults")]
        [SerializeField] private CombatDamageType defaultDamageType = CombatDamageType.Magical;
        [SerializeField] private float defaultSpeed = 10f;
        [SerializeField] private float defaultLifetime = 5f;
        [SerializeField] private float defaultExplosionRadius = 0f;
        [SerializeField] private GameObject defaultExplosionVFX;

        /// <summary>
        /// 原有的完整參數版本
        /// </summary>
        public GameObject SpawnProjectile(
            GameObject prefab,
            Vector3 position,
            Vector3 direction,
            float speed,
            float damage,
            float lifetime,
            float explosionRadius,
            GameObject explosionVFX)
        {
            var usePrefab = prefab != null ? prefab : defaultProjectilePrefab;
            if (!usePrefab) return null;

            var rot = direction.sqrMagnitude > 1e-4f
                ? Quaternion.LookRotation(Vector3.forward, direction) // 2D前提
                : Quaternion.identity;

            var go = Instantiate(usePrefab, position, rot);
            var proj = go.GetComponent<Projectile>();

            if (proj != null)
            {
                proj.Initialize(
                    owner: gameObject,
                    direction: direction.normalized,
                    speed: speed,
                    damage: damage,
                    lifetime: lifetime,
                    explosionRadius: explosionRadius,
                    explosionVFX: explosionVFX,
                    damageType: defaultDamageType
                );
            }
            else
            {
#if UNITY_2023_1_OR_NEWER
                var rb2d = go.GetComponent<Rigidbody2D>();
                if (rb2d) rb2d.linearVelocity = direction.normalized * speed;
                var rb = go.GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity = direction.normalized * speed;
#else
                var rb2d = go.GetComponent<Rigidbody2D>();
                if (rb2d) rb2d.velocity = (Vector2)(direction.normalized * speed);
                var rb = go.GetComponent<Rigidbody>();
                if (rb) rb.velocity = direction.normalized * speed;
#endif
                Destroy(go, lifetime);
            }

            return go;
        }

        /// <summary>
        /// 簡化版本 - 從 SkillInstance 提取參數
        /// </summary>
        public GameObject SpawnProjectile(Vector2 position, Vector2 direction, SkillInstance skill)
        {
            if (skill == null || skill.definition == null)
            {
                Debug.LogError("Invalid skill passed to SpawnProjectile!");
                return null;
            }

            // Get parameters from skill
            var parameters = skill.GetParameters();
            float speed = parameters.Get(SkillParameterKeys.Speed, defaultSpeed);
            float damage = parameters.Get(SkillParameterKeys.Damage, 10f);
            float lifetime = defaultLifetime;
            float explosionRadius = parameters.Get(SkillParameterKeys.AreaOfEffect, defaultExplosionRadius);

            // Use skill's VFX if available, otherwise use default
            GameObject vfx = skill.definition.impactVFX != null
                ? skill.definition.impactVFX
                : defaultExplosionVFX;

            // Use default prefab (you could add a prefab reference to SkillDefinition if needed)
            GameObject prefab = defaultProjectilePrefab;

            return SpawnProjectile(
                prefab: prefab,
                position: position,
                direction: direction,
                speed: speed,
                damage: damage,
                lifetime: lifetime,
                explosionRadius: explosionRadius,
                explosionVFX: vfx
            );
        }
    }
}