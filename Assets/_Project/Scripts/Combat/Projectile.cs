// FILE: Assets/_Project/Scripts/Skills/Executors/Projectile.cs
using Game.Combat;
using UnityEngine;

namespace Game.Skills
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D rb;
        private PoolableObject poolable;
        private GameObject owner;
        private float damage;
        private float lifetime;
        private float explosionRadius;
        private GameObject explosionVFX;
        private DamageType damageType;

        private float aliveTime;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            poolable = GetComponent<PoolableObject>();
        }

        public void Initialize(
            GameObject owner,
            Vector3 direction,
            float speed,
            float damage,
            float lifetime,
            float explosionRadius = 0f,
            GameObject explosionVFX = null,
            DamageType damageType = DamageType.Magical)
        {
            this.owner = owner;
            this.damage = damage;
            this.lifetime = lifetime;
            this.explosionRadius = explosionRadius;
            this.explosionVFX = explosionVFX;
            this.damageType = damageType;

            aliveTime = 0f;

#if UNITY_2023_1_OR_NEWER
            rb.linearVelocity = direction.normalized * speed;
#else
            rb.velocity = (Vector2)(direction.normalized * speed);
#endif

            // Rotate to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        void Update()
        {
            aliveTime += Time.deltaTime;
            if (aliveTime >= lifetime)
            {
                ReturnToPool();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject == owner) return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                if (explosionRadius > 0)
                {
                    ApplyAreaDamage(other.transform.position);
                }
                else
                {
                    damageable.TakeDamage(damage, damageType);
                }

                if (explosionVFX != null)
                {
                    Instantiate(explosionVFX, transform.position, Quaternion.identity);
                }

                ReturnToPool();
            }
        }

        private void ApplyAreaDamage(Vector3 center)
        {
            var colliders = Physics2D.OverlapCircleAll(center, explosionRadius);
            foreach (var col in colliders)
            {
                if (col.gameObject == owner) continue;

                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    float distance = Vector2.Distance(center, col.transform.position);
                    float falloff = 1f - (distance / explosionRadius);
                    float finalDamage = damage * Mathf.Max(0.5f, falloff);

                    damageable.TakeDamage(finalDamage, damageType);
                }
            }
        }

        private void ReturnToPool()
        {
            if (poolable != null)
            {
                poolable.ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (explosionRadius > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, explosionRadius);
            }
        }
    }
}