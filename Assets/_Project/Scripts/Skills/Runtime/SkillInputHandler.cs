// Assets/_Project/Scripts/Skills/Runtime/SkillInputHandler.cs
using UnityEngine;
using Game.Combat;         // IDamageable óp

namespace Game.Skills
{
    public class SkillInputHandler : MonoBehaviour
    {
        private SkillInventory inventory;
        private ISkillActivator activator;
        private Camera mainCamera;

        [Header("Input Settings (Old Input)")]
        [SerializeField]
        private KeyCode[] skillHotkeys =
        {
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.G
        };

        void Awake()
        {
            inventory = GetComponent<SkillInventory>();
            activator = GetComponent<ISkillActivator>();
            mainCamera = Camera.main;
        }

        void Update()
        {
            // ãå Input ÇÃÇ›
            for (int i = 0; i < skillHotkeys.Length; i++)
            {
                if (Input.GetKeyDown(skillHotkeys[i]))
                    TryActivateSkillInSlot(i);
            }
        }

        private void TryActivateSkillInSlot(int slotIndex)
        {
            var skills = inventory.GetActiveSkills();
            SkillInstance skillToActivate = null;
            foreach (var s in skills)
            {
                if (s.slotIndex == slotIndex) { skillToActivate = s; break; }
            }
            if (skillToActivate == null) return;

            var ctx = BuildSkillContext(skillToActivate);
            activator.Activate(skillToActivate, ctx);
        }

        private SkillContext BuildSkillContext(SkillInstance skill)
        {
            var ctx = new SkillContext
            {
                caster = gameObject,
                casterTransform = transform,
                deltaTime = Time.deltaTime
            };

            if (mainCamera != null)
            {
                var mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = transform.position.z;
                ctx.direction = (mousePos - transform.position).normalized;
                ctx.targetPosition = mousePos;
            }
            else
            {
                ctx.direction = transform.forward;
                ctx.targetPosition = transform.position + transform.forward * 10f;
            }

            if (skill.definition.baseEffects.Exists(e => e.requiresTarget))
            {
                ctx.targetObject = FindNearestTarget(ctx.targetPosition, 5f);
            }
            return ctx;
        }

        private GameObject FindNearestTarget(Vector3 position, float radius)
        {
            var colliders = Physics.OverlapSphere(position, radius); // 2D Ç»ÇÁ Physics2D Ç…ïœçX
            GameObject nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;
                var dmg = col.GetComponent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;

                float dist = Vector3.Distance(position, col.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = col.gameObject; }
            }
            return nearest;
        }
    }
}
