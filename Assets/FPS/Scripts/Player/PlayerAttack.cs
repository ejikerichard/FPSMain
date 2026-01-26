using UnityEngine;

namespace FPS
{
    public class PlayerAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera armCam;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private PlayerController controller;
        [SerializeField] private WeaponInventory weaponInventory;

        [Header("Attack Settings")]
        //[SerializeField] private float range = 100f;
        [SerializeField] private float timeBetweenAttacks = 0.15f;
        [SerializeField] private int maxCombo = 2;
        [SerializeField] private float comboBufferStart = 0.7f; // normalizedTime

        [Header("Animator Parameters")]
        [SerializeField] private string attackIDParam = "AttackID";
        [SerializeField] private string noHitParam = "NoHit_ID";
        [SerializeField] private string isAttackParam = "IsAttacking";

        [SerializeField] GameObject hitDecalPrefab;

        int attackIndex = 0;
        bool isAttacking;
        bool attackBuffered;
        float attackCooldown;
        RaycastHit hitInfo;

        void Awake(){
            if (!controller)
                controller = GetComponent<PlayerController>();

            if (!armCam)
                armCam = Camera.main;

            if(!weaponInventory)
                weaponInventory = GetComponent<WeaponInventory>();
        }

        void Update()
        {
            attackCooldown -= Time.deltaTime;

            HandleInput();
            HandleAttackFlow();
        }

        // =========================
        // INPUT
        // =========================
        void HandleInput()
        {
            if (!controller.attackPressed)
                return;

            // consume input
            controller.attackPressed = false;

            // start first attack
            if(!isAttacking && attackCooldown <= 0f){
                StartAttack();
                return;
            }

            // buffer next attack
            if(isAttacking){
                attackBuffered = true;
            }
        }

        // =========================
        // ATTACK FLOW
        // =========================
        void HandleAttackFlow(){
            if (!isAttacking)
                return;

            if (!controller.IsAnimatorTag("AttackHit") &&
                !controller.IsAnimatorTag("AttackNoHit"))
                return;

            AnimatorStateInfo state = controller.stateInfo;

            // chain attack near end
            if(state.normalizedTime >= comboBufferStart &&
                attackBuffered &&
                attackIndex < maxCombo){
                attackBuffered = false;
                StartAttack();
                return;
            }

            // animation finished
            if(state.normalizedTime >= 1f){
                EndAttack();
            }
        }

        // =========================
        // START ATTACK
        // =========================
        void StartAttack(){
            bool hit = PerformRaycast();

            attackIndex++;
            if (attackIndex > maxCombo)
                attackIndex = 1;

            isAttacking = true;
            attackCooldown = timeBetweenAttacks;

            controller.anim.SetBool(isAttackParam, true);

            if(hit){
                controller.anim.SetInteger(noHitParam, 0);
                controller.anim.SetInteger(attackIDParam, attackIndex);
                weaponInventory.DestroyWeapon();
            }else{
                controller.anim.SetInteger(attackIDParam, 0);
                controller.anim.SetInteger(noHitParam, attackIndex);
            }

            //if(hitInfo.transform != null && hitInfo.transform.tag != "Enemy"){
   
            //}
        }

        // =========================
        // END ATTACK
        // =========================
        void EndAttack(){
            isAttacking = false;
            attackBuffered = false;

            controller.anim.SetBool(isAttackParam, false);

            ResetCombo();
        }

        void ResetCombo(){
            attackIndex = 0;
            controller.anim.SetInteger(attackIDParam, 0);
            controller.anim.SetInteger(noHitParam, 0);
        }

        // =========================
        // HIT CHECK
        // =========================
        bool PerformRaycast(){
            return Physics.Raycast(
                    armCam.transform.position,
                    armCam.transform.forward, out hitInfo,
                    weaponInventory.range,
                    hitLayers
                    );
        }

        public void HandleSpawnDecal(){

            Instantiate(hitDecalPrefab, hitInfo.point + hitInfo.normal * 0.01f, Quaternion.LookRotation(-hitInfo.normal));
        }
    }
}
