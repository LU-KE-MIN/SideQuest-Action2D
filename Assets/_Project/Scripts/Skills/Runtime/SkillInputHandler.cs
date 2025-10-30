// FILE: Assets/_Project/Scripts/Skills/Runtime/SkillInputHandler.cs
using UnityEngine;
using Game.Combat;

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
            // ���L�I������
            for (int i = 0; i < skillHotkeys.Length; i++)
            {
                if (Input.GetKeyDown(skillHotkeys[i]))
                    TryActivateSkillInSlot(i);
            }

            // �����������i�͍� Unity Editor �������j
#if UNITY_EDITOR
            if (mainCamera != null)
            {
                // �v�Z���l���E���W
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

                // �`�g���F�n�߉Ɠ����l�ʒu
                Debug.DrawLine(transform.position, mouseWorldPos, Color.red);

                // �`���F�����F�Z�\ᢎ˕����i2 �d�ʒ��j
                Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
                Debug.DrawRay(transform.position, (Vector3)direction * 2f, Color.green, 0.1f);
            }
#endif
        }

        private void TryActivateSkillInSlot(int slotIndex)
        {
            Debug.Log($"[SkillInputHandler] ���������Z�\�i {slotIndex}");
            var skills = inventory.GetActiveSkills();
            SkillInstance skillToActivate = null;

            foreach (var s in skills)
            {
                if (s.slotIndex == slotIndex)
                {
                    skillToActivate = s;
                    break;
                }
            }

            if (skillToActivate == null) return;

            // Check if skill can be cast (mana + cooldown)
            if (!skillToActivate.CanCast(gameObject))
            {
                return;
            }

            // Try to consume resources and start cooldown
            if (!skillToActivate.TryCast(gameObject))
            {
                return;
            }

            // Build context and activate
            var ctx = BuildSkillContext(skillToActivate);
            activator.Activate(skillToActivate, ctx);
        }

        private SkillContext BuildSkillContext(SkillInstance skill)
        {
            var ctx = new SkillContext
            {
                caster = gameObject,
                casterTransform = transform,
                deltaTime = Time.deltaTime,
                skillLevel = skill.currentLevel,
                parameters = skill.GetParameters()
            };

            if (mainCamera != null)
            {
                // ���m�I�����F��ݒ� Z �[�x�C���z�����W
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

                // �v�Z 2D �����i���p X �a Y�j
                Vector2 playerPos = new Vector2(transform.position.x, transform.position.y);
                Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
                Vector2 direction2D = (mousePos2D - playerPos).normalized;

                ctx.direction = direction2D;
                ctx.targetPosition = mouseWorldPos;

                Debug.Log($"[BuildContext] �߉�:{playerPos} ���l:{mousePos2D} ����:{direction2D} �p�x:{Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg}��");
            }
            else
            {
                ctx.direction = transform.right;
                ctx.targetPosition = transform.position + (Vector3)ctx.direction * 10f;
                Debug.LogWarning("[BuildContext] Camera �� null!");
            }

            if (skill.definition.baseEffects != null &&
                skill.definition.baseEffects.Exists(e => e.requiresTarget))
            {
                ctx.targetObject = FindNearestTarget(ctx.targetPosition, 5f);
            }

            return ctx;
        }

        private GameObject FindNearestTarget(Vector3 position, float radius)
        {
            var colliders = Physics2D.OverlapCircleAll(position, radius);
            GameObject nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;

                var dmg = col.GetComponent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;

                float dist = Vector2.Distance(position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = col.gameObject;
                }
            }

            return nearest;
        }
    }
}