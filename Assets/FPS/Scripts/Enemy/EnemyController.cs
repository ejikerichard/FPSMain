using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FPS
{
    public class EnemyController : MonoBehaviour
    {
        public enum EnemyState { Idle, Chase, Combat, Dead }

        [Header("References")]
        public Rigidbody rb;
        public Animator animator;
        public Transform player;

        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float rotationSpeed = 10f;
        public float walkSpeed = 2.5f;
        public float runSpeed = 5.5f;

        [Header("Ranges")]
        public float chaseRange = 12f;
        public float combatRange = 3f;

        [Header("Combat")]
        public float decisionCooldown = 1.2f;
        public float attackCooldown = 2f;
        public float dashCooldown = 3f;

        [Header("Attack")]
        public float lungeSpeed = 6f;
        private float damageAmount = 10f;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField]private float raycastDistance = 2f;

        [Header("Dash")]
        public float dashForce = 10f;
        public float dashDuration = 0.25f;

        [Header("Health")]
        private float maxHealth = 100f;
        private float currentHealth;

        [Header("Circle Combat")]
        [SerializeField] float circleRadius = 3.5f;
        [SerializeField] LayerMask enemyMask;

        EnemyState currentState;

        int orbitSlot;
        Vector3 combatSlotPosition;

        bool hasAggro;
        float aggroTimer;
        float aggroDuration = 5f; // how long enemy remembers player

        bool actionLocked;

        float decisionTimer;
        float lastAttackTime;
        float lastDashTime;

        float chaseTimer;

        bool strafeMode;
        float strafeDir;

        float angleThreshold;
        float fieldOfViewAngle = 45.0f;

        /* ================= INIT ================= */

        void Start()
        {
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player").transform;

            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            currentHealth = maxHealth;

            PickChaseMode();
        }

        /* ================= UPDATE ================= */

        void Update(){
            if (!player || currentState == EnemyState.Dead) return;

            float dist = Vector3.Distance(transform.position, player.position);


            if(dist <= chaseRange){
                hasAggro = true;
                aggroTimer = aggroDuration;
            }
            else{
                aggroTimer -= Time.deltaTime;

                if (aggroTimer <= 0)
                    hasAggro = false;
            }

    
            if(!hasAggro){
                currentState = EnemyState.Idle;
            }
            else if(dist > combatRange){
                currentState = EnemyState.Chase;
            }else{
                currentState = EnemyState.Combat;
            }

            RotateToPlayer();

            if(currentState == EnemyState.Combat)
                CombatLogic();

            if(currentState == EnemyState.Chase)
                UpdateChaseDecision();

            if(currentState == EnemyState.Combat && Time.frameCount % 60 == 0)
                AssignOrbitSlot();
        }

        void FixedUpdate()
        {
            if (actionLocked || currentState == EnemyState.Dead)
                return;

            if (currentState == EnemyState.Chase)
            {
                ChaseMovement();
            }
            else if (currentState == EnemyState.Combat)
            {
                Vector3 target = combatSlotPosition;

                Vector3 dir = target - transform.position;
                dir.y = 0;

                float dist = dir.magnitude;

                if (dist > 0.9f)
                {
                    dir.Normalize();

                    float speed = moveSpeed;

                    if (dist < 1.5f)
                        speed *= 0.5f;

                    Vector3 move = dir * speed * Time.fixedDeltaTime;

                    rb.MovePosition(
                        Vector3.Lerp(rb.position, rb.position + move, 0.6f));

                    UpdateAnimations(dir);
                }
                else
                {
                    StopMovement();
                }
            }
            else
            {
                StopMovement();
            }
        }

        /* ================= CHASE ================= */

        void UpdateChaseDecision()
        {
            chaseTimer -= Time.deltaTime;

            if (chaseTimer <= 0)
            {
                PickChaseMode();
                chaseTimer = 1.5f;
            }
        }

        void PickChaseMode()
        {
            strafeMode = Random.value < 0.5f;
            strafeDir = Random.value > 0.5f ? 1 : -1;
        }

        void ChaseMovement()
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0;

            Vector3 moveDir;

            if (!strafeMode)
            {
                moveDir = toPlayer.normalized;
            }
            else
            {
                Vector3 side =
                    Vector3.Cross(Vector3.up, toPlayer).normalized;

                moveDir = side * strafeDir;
            }

            float speed = runSpeed; // CHASING = RUN

            Vector3 move = moveDir * speed * Time.fixedDeltaTime;

            rb.MovePosition(rb.position + move);

            UpdateAnimations(moveDir);
        }

        void StopMovement()
        {
            UpdateAnimations(Vector3.zero);
        }

        /* ================= ROTATION ================= */

        void RotateToPlayer()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0;

            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion rot = Quaternion.LookRotation(dir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rot,
                rotationSpeed * Time.deltaTime);
        }

        /* ================= SLOT SYSTEM ================= */

        void AssignOrbitSlot()
        {
            Collider[] enemies = new Collider[20];

            int count = Physics.OverlapSphereNonAlloc(
                player.position,
                circleRadius + 2f,
                enemies,
                enemyMask);

            List<EnemyController> list = new List<EnemyController>();

            for (int i = 0; i < count; i++)
            {
                EnemyController ec = enemies[i].GetComponent<EnemyController>();

                if (ec != null)
                    list.Add(ec);
            }

            list.Sort((a, b) =>
                a.GetInstanceID().CompareTo(b.GetInstanceID()));

            orbitSlot = Mathf.Max(0, list.IndexOf(this));

            combatSlotPosition = GetCirclePosition();
        }

        Vector3 GetCirclePosition()
        {
            Collider[] enemies = new Collider[20];

            int count = Physics.OverlapSphereNonAlloc(
                player.position,
                circleRadius + 2f,
                enemies,
                enemyMask);

            int totalSlots = Mathf.Max(2, count);

            float angleStep = 360f / totalSlots;

            float angle = orbitSlot * angleStep;

            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset =
                new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * circleRadius;

            return player.position + offset;
        }

        /* ================= COMBAT ================= */

        void CombatLogic()
        {
            float speed = walkSpeed;
            decisionTimer -= Time.deltaTime;

            if (decisionTimer > 0 || actionLocked)
                return;

            decisionTimer = decisionCooldown;

            bool canAttack =
                Time.time > lastAttackTime + attackCooldown;

            bool canDash =
                Time.time > lastDashTime + dashCooldown;

            float roll = Random.value;

            if (canAttack && roll < 0.65f)
            {
                Attack();
                return;
            }

            if (canDash && roll < 0.90f)
            {
                Dash();
                return;
            }
        }

        /* ================= ATTACK ================= */

        void Attack()
        {
            actionLocked = true;
            lastAttackTime = Time.time;

            animator.SetTrigger("LightAttack");

            PerformRaycast();

            CameraWobble.Instance.Shake(2.5f);

            StartCoroutine(AttackRoutine());
        }

        IEnumerator AttackRoutine()
        {
            float t = 0;

            while (t < 0.25f)
            {
                rb.MovePosition(
                    rb.position +
                    transform.forward *
                    lungeSpeed *
                    Time.deltaTime);

                t += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            actionLocked = false;
        }

        void  PerformRaycast()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position + transform.forward, 3f, hitLayers);
            foreach(Collider col in colliders)
            {
                col.GetComponent<HealthControl>().TakeDamage(damageAmount);

                Vector3 directionPlayer = (col.transform.position - transform.position).normalized;
                float dotProduct = Vector3.Dot(transform.forward, directionPlayer);
                if(dotProduct > angleThreshold){
                     Vector3 pushdir = (transform.position - col.transform.position).normalized;

                    col.GetComponent<Rigidbody>().AddForce(pushdir * 10f, ForceMode.Impulse);
                }



                    Debug.Log("Player hit for " + damageAmount + " damage!");
            }
        }

        /* ================= DASH ================= */

        void Dash()
        {
            actionLocked = true;
            lastDashTime = Time.time;

            Vector3 forward = transform.forward;
            Vector3 back = -transform.forward;

            Vector3 dir = Random.value < 0.7f ? back : forward;

            animator.SetTrigger("Dash");

            StartCoroutine(DashRoutine(dir));
        }

        IEnumerator DashRoutine(Vector3 dir)
        {
            float t = 0;

            while (t < dashDuration)
            {
                rb.MovePosition(
                    rb.position +
                    dir * dashForce * Time.deltaTime);

                t += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            actionLocked = false;
        }

        /* ================= ANIMATION ================= */

        void UpdateAnimations(Vector3 moveDir)
        {
            if (!animator) return;

            float speed = moveDir.magnitude;

            Vector3 local = transform.InverseTransformDirection(moveDir);

            // 🔥 THIS IS THE FIX
            float animationMultiplier = currentState == EnemyState.Chase ? 2f : 1f;

            animator.SetFloat("MoveX", local.x * animationMultiplier, 0.15f, Time.deltaTime);
            animator.SetFloat("MoveZ", local.z * animationMultiplier, 0.15f, Time.deltaTime);

            animator.SetBool("BearRun", moveDir.sqrMagnitude > 0.0001f);
        }

        /* ================= DAMAGE ================= */

        public void TakeDamage(float damage)
        {
            if (currentState == EnemyState.Dead)
                return;

            currentHealth -= damage;

            if (currentHealth <= 0)
                Die();
            else
                animator.SetTrigger("Hit");
        }

        void Die()
        {
            currentState = EnemyState.Dead;

            animator.SetTrigger("Die");

            rb.linearVelocity = Vector3.zero;

            Collider col = GetComponent<Collider>();
            if (col) col.enabled = false;

            Destroy(gameObject, 3f);
        }
    }
}