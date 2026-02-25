using UnityEngine;
using UnityEngine.AI;

namespace FPS
{
    public class EnemyController : MonoBehaviour
    {
        public enum EnemyState { Idle, Chase, Combat, Dead }
        public enum CombatAction { Idle, Strafe, Attack, StepBack }

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform player;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float sleepRange = 25f; // AI sleeps if player is further

        [Header("Ranges")]
        [SerializeField] private float chaseRange = 12f;
        [SerializeField] private float combatRange = 3f;

        [Header("Combat Settings")]
        [SerializeField] private float decisionCooldown = 1.5f;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private float checkRadius = 2.5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackCooldown = 2.0f;
        private float lastAttackTime;

        [Header("Attack Settings")]
        [SerializeField] private float heavyAttackChance = 0.3f; // 30% chance for heavy
        [SerializeField] private float lightDamage = 10f;
        [SerializeField] private float heavyDamage = 25f;

        [Header("Lunge Settings")]
        [SerializeField] private float lungeDistance = 2.0f;
        [SerializeField] private float lungeSpeed = 5.0f;
        [SerializeField] private float recoverySpeed = 2.0f;

        private Vector3 originalPosition;
        private bool isLunging = false;

        private float currentDamage; // Set this when picking the attack

        private EnemyState currentState;
        private CombatAction currentAction;
        private float decisionTimer;
        private float strafeDir;
        private Collider[] nearbyEnemies = new Collider[5];

        //private static readonly int SpeedHash = Animator.StringToHash("Speed");
        //private static readonly int AttackHash = Animator.StringToHash("Attack");

        void Awake()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();

            // Let the agent handle movement logic but we'll apply it via velocity
            agent.updatePosition = true;
            agent.updateRotation = false; // We handle rotation for smoother lerping
            agent.speed = moveSpeed;
        }

        void Start()
        {
            if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            strafeDir = Random.value > 0.5f ? 1f : -1f;
        }

        void Update()
        {
            if (!player) return;

            float distSq = (player.position - transform.position).sqrMagnitude;
            if (distSq > sleepRange * sleepRange) return; // Optimization: Skip AI if too far

            UpdateTimers();
            HandleStateTransitions(distSq);
            HandleRotation();
            UpdateAnimations();
        }

        private void UpdateTimers()
        {
            if (decisionTimer > 0) decisionTimer -= Time.deltaTime;
        }

        private void HandleStateTransitions(float distSq)
        {
            if (distSq > chaseRange * chaseRange)
                ChangeState(EnemyState.Idle);
            else if (distSq > combatRange * combatRange)
                ChangeState(EnemyState.Chase);
            else
                ChangeState(EnemyState.Combat);

            ExecuteCurrentState();
        }

        private void ExecuteCurrentState()
        {
            switch (currentState)
            {
                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    break;

                case EnemyState.Combat:
                    DoCombatLogic();
                    break;

                case EnemyState.Idle:
                    agent.isStopped = true;
                    break;
            }
        }

        private void DoCombatLogic(){
            if (decisionTimer <= 0f)
            {
                decisionTimer = decisionCooldown + Random.Range(-0.2f, 0.2f);
                ChooseCombatAction();
            }

            switch (currentAction)
            {
                case CombatAction.Strafe:
                    agent.isStopped = false;
                    agent.SetDestination(CalculateStrafePosition());
                    break;

                case CombatAction.StepBack:
                    agent.isStopped = false;
                    Vector3 backwardDir = (transform.position - player.position).normalized;
                    Vector3 targetRetreat = transform.position + backwardDir * 3f;

                    // Safety: Ensure the retreat point is valid on the NavMesh
                    if (NavMesh.SamplePosition(targetRetreat, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                    break;

                case CombatAction.Idle:
                case CombatAction.Attack:
                    agent.isStopped = true;
                    break;
            }
        }

        private void ChooseCombatAction(){
            float roll = Random.value;

            if (roll < 0.15f) currentAction = CombatAction.Idle;
            else if (roll < 0.50f) currentAction = CombatAction.Strafe;   // 35% chance
            else if (roll < 0.70f) currentAction = CombatAction.StepBack; // 20% chance
            else PerformAttack();
        }

        private void PerformAttack(){
            if (Time.time < lastAttackTime + attackCooldown || isLunging) return;

            lastAttackTime = Time.time;
            currentAction = CombatAction.Attack;

            // Pick attack type
            bool isHeavy = Random.value < 0.3f;
            string trigger = isHeavy ? "HeavyAttack" : "LightAttack";
            animator.SetTrigger(trigger);

            // Start the physical movement (Lunge)
            StartCoroutine(AttackLungeRoutine());
        }

        private System.Collections.IEnumerator AttackLungeRoutine(){
            isLunging = true;
            agent.isStopped = true; // Stop NavMesh from fighting our manual movement

            Vector3 startPos = transform.position;
            Vector3 targetPos = transform.position + transform.forward * lungeDistance;

            // 1. Lunge Forward (Wind-up to Hit)
            float elapsed = 0;
            while(elapsed < 0.2f) // Fast snap forward (adjust time to match animation)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lungeSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Wait slightly for the "Hit" to land (sync with TriggerMeleeDamage)
            yield return new WaitForSeconds(0.3f);

            // 2. Recovery (Move back to original spot)
            elapsed = 0;
            while (elapsed < 0.5f){
                transform.position = Vector3.Lerp(transform.position, startPos, Time.deltaTime * recoverySpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            agent.isStopped = false;
            isLunging = false;
            currentAction = CombatAction.Idle;
        }

        public void TriggerMeleeDamage(){
            float distSq = (player.position - transform.position).sqrMagnitude;
            if(distSq <= combatRange * combatRange){
                Debug.Log($"Hit! Type: {(currentDamage > lightDamage ? "Heavy" : "Light")} | Damage: {currentDamage}");
                // player.GetComponent<IHealth>()?.TakeDamage(currentDamage);
            }
        }

        private Vector3 CalculateStrafePosition()
        {
            CheckForNearbyEnemies();
            Vector3 offset = Vector3.Cross(Vector3.up, (player.position - transform.position).normalized);
            return transform.position + (offset * strafeDir * 2f);
        }

        private void CheckForNearbyEnemies()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, checkRadius, nearbyEnemies, enemyMask);
            for(int i = 0; i < count; i++)
            {
                if (nearbyEnemies[i].transform == transform) continue;

                // If an enemy is in our way, flip strafe direction
                float dot = Vector3.Dot(transform.right, (nearbyEnemies[i].transform.position - transform.position).normalized);
                if (dot > 0.5f && strafeDir > 0) strafeDir = -1f;
                else if (dot < -0.5f && strafeDir < 0) strafeDir = 1f;
            }
        }

        private void HandleRotation(){
            Vector3 lookDir = (player.position - transform.position);
            lookDir.y = 0;
            if(lookDir.sqrMagnitude > 0.1f){
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        private void UpdateAnimations(){
            if (!animator) return;

            // Use desiredVelocity for more responsive animation transitions
            Vector3 moveVec = agent.velocity;

            // If agent is basically still, use a tiny vector to avoid flickering
            if (moveVec.sqrMagnitude < 0.1f) moveVec = Vector3.zero;

            // Convert to local space
            Vector3 localVelocity = transform.InverseTransformDirection(moveVec);

            // Normalize against moveSpeed
            float x = localVelocity.x / moveSpeed;
            float z = localVelocity.z / moveSpeed;

            // Apply to Blend Tree
            animator.SetFloat("MoveX", x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveZ", z, 0.1f, Time.deltaTime);

            // Determine if we are moving based on the agent's actual velocity
            bool isMoving = agent.velocity.sqrMagnitude > 0.1f || isLunging;
            animator.SetBool("BearRun", isMoving);
        }

        private void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
        }
    }
}
