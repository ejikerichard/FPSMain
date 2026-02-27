using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FPS
{
    public class EnemyController : MonoBehaviour
    {
        public enum EnemyState { Idle, Chase, Combat, Dead }
        public enum CombatAction { Idle, Strafe, Attack, StepBack, Dash }

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform player;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float sleepRange = 25f;
        [SerializeField] private float personalSpace = 2f;

        [Header("Ranges")]
        [SerializeField] private float chaseRange = 12f;
        [SerializeField] private float combatRange = 3f;

        [Header("Combat")]
        [SerializeField] private float decisionCooldown = 1.1f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float dashCooldown = 3f;

        [Header("Damage")]
        [SerializeField] private float lightDamage = 10f;
        [SerializeField] private float heavyDamage = 25f;
        [SerializeField] private float heavyAttackChance = 0.3f;

        [Header("Lunge")]
        [SerializeField] private float lungeSpeed = 6f;
        [SerializeField] private float recoverySpeed = 3f;

        [Header("Dash")]
        [SerializeField] private float dashForce = 15f;
        [SerializeField] private float dashDuration = 0.25f;

        [Header("Enemy Avoidance")]
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private float checkRadius = 2.5f;

        [Header("Orbiting")]
        [SerializeField] private float orbitDistance = 3f;
        [SerializeField] private float orbitSpeed = 120f; // degrees/sec

        private EnemyState currentState;
        private CombatAction currentAction;
        private CombatAction lastAction;

        float decisionTimer;
        float actionCommitTimer;
        float lastAttackTime;
        float lastDashTime;

        bool actionLocked;
        Coroutine activeRoutine;

        float strafeDir;
        Vector3 targetOffset;
        float currentDamage;

        [SerializeField] private float flipCooldown = 0.5f;
        private float lastFlipTime;

        private float orbitAngle; // internal angle for smooth orbit
        private int orbitSlot;

        private Collider[] nearbyEnemies = new Collider[20];

        /* ===================================================== */

        void Awake()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();

            agent.updateRotation = false;
            agent.speed = moveSpeed;
            agent.avoidancePriority = Random.Range(0, 99);
        }

        void Start()
        {
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            strafeDir = Random.value > 0.5f ? 1 : -1;

            float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
            targetOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * personalSpace;
        }

        /* ===================================================== */

        void Update()
        {
            if (!player) return;

            float distSq = (player.position - transform.position).sqrMagnitude;

            if (distSq > sleepRange * sleepRange) return;

            decisionTimer -= Time.deltaTime;
            actionCommitTimer -= Time.deltaTime;

            if (!actionLocked)
                HandleStateTransitions(distSq);

            HandleRotation();
            UpdateAnimations();

            // Update orbit slot occasionally for multi-enemy formation
            if (currentState == EnemyState.Combat && Time.frameCount % 20 == 0)
                AssignOrbitSlot();

            // Update nearby enemies for separation
            if (Time.frameCount % 10 == 0)
                Physics.OverlapSphereNonAlloc(transform.position, checkRadius, nearbyEnemies, enemyMask);
        }

        /* ================= STATE MACHINE ================= */

        void HandleStateTransitions(float distSq)
        {
            float chaseSq = chaseRange * chaseRange;
            float combatSq = combatRange * combatRange;

            if (distSq > chaseSq)
                ChangeState(EnemyState.Idle);
            else if (distSq > combatSq)
                ChangeState(EnemyState.Chase);
            else
                ChangeState(EnemyState.Combat);

            ExecuteState();
        }

        void ExecuteState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    agent.isStopped = true;
                    break;

                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.stoppingDistance = 0.5f;
                    MoveWithSeparation(player.position + targetOffset);
                    decisionTimer = 0;
                    break;

                case EnemyState.Combat:
                    CombatLogic();
                    break;
            }
        }

        /* ================= COMBAT ================= */

        void CombatLogic()
        {
            if (decisionTimer > 0 || actionCommitTimer > 0)
                return;

            decisionTimer = decisionCooldown * Random.Range(0.85f, 1.1f);

            ChooseCombatAction();
            ExecuteCombatAction();

            actionCommitTimer = 0.6f;
        }

        void ChooseCombatAction()
        {
            float distance = Vector3.Distance(transform.position, player.position);
            float roll = Random.value;

            bool canAttack = Time.time > lastAttackTime + attackCooldown;
            bool canDash = Time.time > lastDashTime + dashCooldown;

            if (distance <= combatRange * 0.85f && canAttack)
            {
                currentAction = roll < 0.7f ? CombatAction.Attack : CombatAction.Strafe;
                return;
            }

            if (!canAttack)
            {
                currentAction = roll < 0.7f ? CombatAction.StepBack : CombatAction.Strafe;
                return;
            }

            if (roll < 0.5f) currentAction = CombatAction.Attack;
            else if (roll < 0.8f) currentAction = CombatAction.Strafe;
            else if (canDash && roll < 0.95f) currentAction = CombatAction.Dash;
            else currentAction = CombatAction.Idle;

            if (currentAction == CombatAction.Idle && lastAction == CombatAction.Idle)
                currentAction = CombatAction.Strafe;
        }

        void ExecuteCombatAction()
        {
            lastAction = currentAction;

            switch (currentAction)
            {
                case CombatAction.Attack:
                    PerformAttack();
                    break;

                case CombatAction.Strafe:
                    MoveWithSeparation(CalcOrbitPos());
                    break;

                case CombatAction.StepBack:
                    Vector3 retreat = (transform.position - player.position).normalized;
                    MoveWithSeparation(player.position + retreat * (combatRange + 2f));
                    break;

                case CombatAction.Dash:
                    StartDash();
                    break;

                case CombatAction.Idle:
                    agent.isStopped = true;
                    break;
            }
        }

        /* ================= MOVEMENT + SEPARATION ================= */

        Vector3 ComputeSeparation()
        {
            Vector3 separationVec = Vector3.zero;

            for (int i = 0; i < nearbyEnemies.Length; i++)
            {
                Transform other = nearbyEnemies[i]?.transform;
                if (!other || other == transform) continue;

                Vector3 toOther = other.position - transform.position;
                float distance = toOther.magnitude;

                if (distance < 1f)
                    separationVec -= (toOther.normalized * (1f - distance));
            }

            return separationVec;
        }

        void MoveWithSeparation(Vector3 targetPos)
        {
            Vector3 separation = ComputeSeparation();
            Vector3 finalPos = targetPos + separation;
            agent.isStopped = false;
            agent.SetDestination(finalPos);
        }

        /* ================= ORBITING ================= */

        void AssignOrbitSlot()
        {
            Collider[] enemies = new Collider[20];
            int count = Physics.OverlapSphereNonAlloc(player.position, orbitDistance + 2f, enemies, enemyMask);

            EnemyController[] enemyControllers = new EnemyController[count];
            int validCount = 0;

            for (int i = 0; i < count; i++)
            {
                EnemyController ec = enemies[i].GetComponent<EnemyController>();
                if (ec != null)
                    enemyControllers[validCount++] = ec;
            }

            if (validCount > 1)
            {
                System.Array.Sort(enemyControllers, 0, validCount,
                    Comparer<EnemyController>.Create((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID())));
            }

            orbitSlot = 1;
            for (int i = 0; i < validCount; i++)
            {
                if (enemyControllers[i] == this)
                {
                    orbitSlot = i + 1;
                    break;
                }
            }
        }

        Vector3 CalcOrbitPos()
        {
            int totalSlots = 1;
            Collider[] enemies = new Collider[20];
            int count = Physics.OverlapSphereNonAlloc(player.position, orbitDistance + 2f, enemies, enemyMask);
            totalSlots += count;

            float slotAngle = 360f / totalSlots * orbitSlot;
            orbitAngle += orbitSpeed * Time.deltaTime * strafeDir;
            float finalAngle = (slotAngle + orbitAngle) % 360f;

            float rad = finalAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitDistance;

            Vector3 targetPos = player.position + offset;

            // Separation
            targetPos += ComputeSeparation();

            return targetPos;
        }

        /* ================= ATTACK / DASH ================= */

        void PerformAttack()
        {
            if (actionLocked) return;

            actionLocked = true;
            lastAttackTime = Time.time;

            bool heavy = Random.value < heavyAttackChance;
            currentDamage = heavy ? heavyDamage : lightDamage;

            animator.SetTrigger(heavy ? "HeavyAttack" : "LightAttack");

            StartCoroutine(AttackRoutine());
        }

        IEnumerator AttackRoutine()
        {
            agent.isStopped = true;

            float t = 0f;
            while (t < 0.25f)
            {
                agent.Move(transform.forward * lungeSpeed * Time.deltaTime + ComputeSeparation() * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            t = 0;
            while (t < 0.4f)
            {
                agent.Move(-transform.forward * recoverySpeed * Time.deltaTime + ComputeSeparation() * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }

            agent.isStopped = false;
            actionLocked = false;
        }

        void StartDash()
        {
            if (actionLocked) return;

            actionLocked = true;
            lastDashTime = Time.time;

            float x = Random.value > 0.5f ? 1 : -1;
            float y = Random.value > 0.5f ? 1 : -1;

            animator.SetFloat("DashX", x);
            animator.SetFloat("DashY", y);
            animator.SetTrigger("Dash");

            Vector3 dir = (transform.forward * y + transform.right * x).normalized;

            StartCoroutine(DashRoutine(dir));
        }

        IEnumerator DashRoutine(Vector3 dir)
        {
            agent.isStopped = true;

            float t = 0;
            while (t < dashDuration)
            {
                agent.Move(dir * dashForce * Time.deltaTime + ComputeSeparation() * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }

            agent.isStopped = false;
            actionLocked = false;
        }

        /* ================= ROTATION + ANIM ================= */

        void HandleRotation()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        void UpdateAnimations()
        {
            Vector3 localVel = transform.InverseTransformDirection(agent.velocity);

            animator.SetFloat("MoveX", localVel.x / moveSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveZ", localVel.z / moveSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("BearRun", agent.velocity.sqrMagnitude > 0.1f || actionLocked);
        }

        void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
        }

        public void TriggerMeleeDamage()
        {
            float distSq = (player.position - transform.position).sqrMagnitude;
            if (distSq <= combatRange * combatRange)
                Debug.Log($"Hit Player for {currentDamage}");
        }
    }
}