// Assets/_Project/Scripts/Skills/Runtime/ProjectileSpawner.cs
using UnityEngine;
using Game.Combat;                           // Projectile などがここ
using CombatDamageType = Game.Combat.DamageType;  // ← 型を固定（重要）

namespace Game.Skills
{
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject defaultProjectilePrefab;

        [Header("Defaults")]
        [SerializeField] private CombatDamageType defaultDamageType = CombatDamageType.Magical;

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
                    damageType: defaultDamageType     // ← CombatDamageType なので一致
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
    }
}
