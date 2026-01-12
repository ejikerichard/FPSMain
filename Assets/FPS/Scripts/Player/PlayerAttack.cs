using UnityEngine;

namespace FPS
{
    public class PlayerAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera armCam;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private PlayerController controller;

        [Header("Attack Settings")]
        [SerializeField] private float range = 100f;
        [SerializeField] private float timeBetweenAttacks = 0.5f;
        [SerializeField] private int maxCombo = 2;

        [Header("Weapon")]
        [SerializeField] WeaponData weaponData;

        [Header("Animator Parameters")]
        [SerializeField] private string attackIDParam = "AttackID";
        [SerializeField] private string noHitParam = "NoHit_ID";
        [SerializeField] private string isAttackParam = "IsAttacking";

        private int attackIndex = 0;
        private float attackCooldown;
        private bool isAttacking;

        void Awake(){
            if (!controller)
                controller = GetComponent<PlayerController>();

            if (!armCam)
                armCam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        }

        void Update(){
            attackCooldown -= Time.deltaTime;

            if (controller.attackPressed)
                TryAttack();

            HandleAttackReset();
        }

        void TryAttack(){
            if (attackCooldown > 0f || isAttacking)
                return;

            bool hit = PerformRaycast();

            attackIndex++;
            if (attackIndex > maxCombo)
                attackIndex = 1;

            isAttacking = true;
            attackCooldown = timeBetweenAttacks;

            controller.anim.SetBool(isAttackParam, true);

            if (hit){
                Debug.Log("Hit");
                controller.anim.SetInteger(attackIDParam, attackIndex);
            }
            else
                controller.anim.SetInteger(noHitParam, attackIndex);
        }

        bool PerformRaycast(){
            Vector3 rayOrigin = armCam.transform.position;
            RaycastHit hit;
            return Physics.Raycast(rayOrigin, armCam.transform.forward, out hit, range, hitLayers);
        }

        void HandleAttackReset(){
            if (!isAttacking)
                return;

            if (controller.stateInfo.normalizedTime < 1f)
                return;

            if (!controller.IsAnimatorTag("AttackHit") &&
                !controller.IsAnimatorTag("AttackNoHit"))
                return;

            isAttacking = false;
            controller.anim.SetBool(isAttackParam, false);

            if(attackIndex >= maxCombo){
                attackIndex = 0;
                controller.anim.SetInteger(attackIDParam, 0);
                controller.anim.SetInteger(noHitParam, 0);
            }
        }
    }
}
