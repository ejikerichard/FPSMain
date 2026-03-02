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

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

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
        private float personalCombatRange;

        private Collider[] nearbyEnemies = new Collider[20];

        /* ===================================================== */

        void Awake()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();

            agent.updateRotation = false;
            agent.speed = moveSpeed;

            // IMPORTANT — makes avoidance actually work
            agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            agent.avoidancePriority = Random.Range(0, 99);
            agent.radius = 0.45f;

            currentHealth = maxHealth;
        }

        void Start()
        {
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            strafeDir = Random.value > 0.5f ? 1 : -1;

            float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
            targetOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * personalSpace;

            personalCombatRange = combatRange + Random.Range(-0.8f, 1.2f);
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
            ApplySeparationVelocity();
            UpdateAnimations();

            // Update orbit slot occasionally for multi-enemy formation
            if (currentState == EnemyState.Combat && Time.frameCount % 60 == 0)
                AssignOrbitSlot();

            // Update nearby enemies for separation
            if (Time.frameCount % 10 == 0)
                Physics.OverlapSphereNonAlloc(transform.position, checkRadius, nearbyEnemies, enemyMask);
        }

        /* ================= STATE MACHINE ================= */

        void HandleStateTransitions(float distSq)
        {
            float chaseSq = chaseRange * chaseRange;
            float combatSq = personalCombatRange * personalCombatRange;

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
                    agent.autoBraking = false;
                    agent.stoppingDistance = 0.5f;

                    Vector3 dirToEnemy =
                        (transform.position - player.position).normalized;

                    Vector3 dynamicOffset =
                        dirToEnemy * personalSpace;

                    MoveWithSeparation(player.position + dynamicOffset);

                    decisionTimer = 0;
                    break;

                case EnemyState.Combat:
                    agent.stoppingDistance = personalCombatRange * 0.9f;
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

            if( distance <= personalCombatRange * 0.85f && canAttack)
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
                    MoveWithSeparation(player.position + retreat * (personalCombatRange + 2f));
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
            Vector3 separation = Vector3.zero;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                checkRadius,
                nearbyEnemies,
                enemyMask);

            for (int i = 0; i < count; i++)
            {
                Transform other = nearbyEnemies[i].transform;
                if (!other || other == transform) continue;

                Vector3 away = transform.position - other.position;
                float dist = away.magnitude;

                if (dist > 0.01f)
                {
                    // smooth falloff force
                    float strength = (checkRadius - dist) / checkRadius;
                    separation += away.normalized * strength;
                }
            }

            // BIG difference — strong push apart
            return separation * 2.5f;
        }
        void MoveWithSeparation(Vector3 targetPos)
        {
            Vector3 separation = ComputeSeparation();
            Vector3 finalPos = targetPos + separation;

            agent.isStopped = false;

            // ALWAYS refresh if close or stopped
            if (!agent.hasPath ||
                agent.remainingDistance < 0.3f ||
                Vector3.Distance(agent.destination, finalPos) > 0.5f)
            {
                agent.SetDestination(finalPos);
            }
        }

        /* ================= ORBITING ================= */

        void AssignOrbitSlot()
        {
            Collider[] enemies = new Collider[20];

            int count = Physics.OverlapSphereNonAlloc(
                player.position,
                orbitDistance + 3f,
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
        }
        Vector3 CalcOrbitPos()
        {
            Collider[] enemies = new Collider[20];

            int count = Physics.OverlapSphereNonAlloc(
                player.position,
                orbitDistance + 3f,
                enemies,
                enemyMask);

            int totalSlots = Mathf.Max(2, count);;

            float angleStep = 360f / totalSlots;

            orbitAngle += orbitSpeed * Time.deltaTime * strafeDir;

            float angle = orbitSlot * angleStep + orbitAngle;

            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset =
                new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad))
                * personalCombatRange;

            return player.position + offset + ComputeSeparation();
        }

        void ApplySeparationVelocity()
        {
            if (agent.isStopped || !agent.hasPath) return;

            Vector3 separation = ComputeSeparation();

            // steer current velocity instead of repathing
            agent.Move(separation * 1.2f * Time.deltaTime);
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
            if (distSq <= personalCombatRange * personalCombatRange)
                Debug.Log($"Hit Player for {currentDamage}");
        }
        public void TakeDamage(float damage){
            if (currentState == EnemyState.Dead) return;

            currentHealth -= damage;

            if (currentHealth <= 0)
                Die();
            else
                animator.SetTrigger("Hit"); // optional hit reaction
        }
        void Die(){
            currentState = EnemyState.Dead;
            agent.isStopped = true;
            actionLocked = true;

            animator.SetTrigger("Die"); // play death animation

            //if (deathEffect)
            //    Instantiate(deathEffect, transform.position, Quaternion.identity);

            // Disable collider and destroy object after animation
            Collider col = GetComponent<Collider>();
            if (col) col.enabled = false;

            Destroy(gameObject, 3f); // destroy after 3 seconds (or adjust)
        }
    }
}