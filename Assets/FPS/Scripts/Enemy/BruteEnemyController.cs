using UnityEngine;
using static FPS.EnemyController;

namespace FPS {
    public class BruteEnemyController : MonoBehaviour
    {
        public enum State { Idle, Chase, Attack, Dead }
        public enum AttackAction { None, Attack, Dodge, Dash, Idle, Strafe, Taunt }

        public enum CombatRole { Rusher, Flanker }



        public AttackAction currentAction;
        private float actionTimer;
        private float actionDuration;
        private bool isPerformingAction;

        public CombatRole myRole;
        [Range(0, 1)] public float flankerChance = 0.5f;


        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Animator animator;


        [Header("Detection")]
        public float detectionRange = 15f;
        public float attackRange = 2.5f;
        public float chaseDecisionInterval = 1.5f;
        private float chaseDecisionTimer;
        private bool isStrafingInChase;
        private float chaseStrafeDir;
        public float avoidanceRadius = 2f;
        public LayerMask obstacleMask;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float acceleration = 20f;
        public float rotationSpeed = 10f;

        [Header("Attack Behavior")]
        public float minActionTime = 0.8f;
        public float maxActionTime = 2.0f;
        public string[] attackAnimations = { "Attack1", "Attack2"};
        public float attackCooldown = 2.0f;
        public LayerMask hitLayer;
        public float attackForwardPush = 3f;
        private float lastAttackTime;
        private float damageAmount = 10f;

        [Header("Dash/Dodge")]
        public float dashForce = 8f;
        public float dodgeForce = 6f;

        [Header("Strafing")]
        public float strafeSpeed = 3f;
        public float strafeDirection;

        [Header("Idle/Taunt Settings")]
        public string[] tauntAnimations = { "Taunt1", "Taunt2", "Taunt3" };
        public float minIdleTime = 3f;
        public float maxIdleTime = 6f;

        public float separationRadius = 1.2f;
        public float separationForce = 15f;

        [Header("Health")]
        private float maxHealth = 100f;
        private float currentHealth;

        public float surroundDistance = 4f; 

        private float slotAngle;


        private float idleTimer;
        private float currentIdleDuration;
        private bool isTaunting;
        private float tauntEndTime;


        public float initialAttackDelay = 1.0f; 
        private float attackDelayTimer;
        private bool hasWaitFinished;

        private State currentState;

        private bool actionApplied;
        private float dodgeDirection;
        private float currentChaseSpeed;

        float strafeLockTimer;
        public float strafeLockDuration = 0.5f;


        void Start()
        {
            rb = GetComponent<Rigidbody>();

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player").transform;

            rb.freezeRotation = true;


            myRole = (Random.value < flankerChance) ? CombatRole.Flanker : CombatRole.Rusher;

            // Flankers get a specific side to prefer (left or right)
            slotAngle = (Random.value > 0.5f) ? 90f : -90f;



            ResetIdleTimer();

        }
        private void Update()
        {
            UpdateAnimations(rb.linearVelocity);

            //if(animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle")){
            //    animator.applyRootMotion = true;
            //}else{
            //    animator.applyRootMotion = false;
            //}
        }

        void FixedUpdate(){
            float distance = Vector3.Distance(transform.position, player.position);

            switch (currentState){
                case State.Idle:
                    if(CanSeePlayer(distance)){
                        isTaunting = false;
                        currentState = State.Chase;
                    }else{
                        HandleIdleState();
                    }
                    break;

                case State.Chase:
                    ChasePlayer(distance);
                    break;

                case State.Attack:
                    AttackPlayer(distance);
                    break;
            }
        }
        void HandleIdleState()
        {
            if (isTaunting)
            {
                // Simple timer check to see if the taunt is done
                if (Time.time >= tauntEndTime)
                {
                    isTaunting = false;
                    ResetIdleTimer();
                }
                return;
            }

            idleTimer += Time.deltaTime;

            if (idleTimer >= currentIdleDuration)
            {
                if (tauntAnimations.Length > 0 && Random.value > 0.6f)
                {
                    isTaunting = true;

    
                    string pickedTaunt = tauntAnimations[Random.Range(0, tauntAnimations.Length)];
                    animator.CrossFadeInFixedTime(pickedTaunt, 0.2f);


                    float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
                    tauntEndTime = Time.time + (clipLength > 0 ? clipLength : 2.0f);
                }
                else
                {
                    ResetIdleTimer();
                }
            }
        }


        void ResetIdleTimer(){
            idleTimer = 0f;
            currentIdleDuration = Random.Range(minIdleTime, maxIdleTime);
        }

        bool CanSeePlayer(float distance)
        {
            if (distance > detectionRange) return false;

            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 dir = (player.position - origin).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, detectionRange))
            {
                if (hit.transform.CompareTag("Player"))
                    return true;
            }

            return false;
        }

        void ChasePlayer(float distance)
        {
            if (currentAction == AttackAction.Dodge || currentAction == AttackAction.Dash || currentAction == AttackAction.Attack)
            {
                return;
            }

            chaseDecisionTimer += Time.deltaTime;
            if (chaseDecisionTimer >= chaseDecisionInterval)
            {
                chaseDecisionTimer = 0;
                currentChaseSpeed = Random.Range(moveSpeed * 0.7f, moveSpeed);
                isStrafingInChase = Random.value > 0.5f;
                chaseStrafeDir = Random.value > 0.5f ? 1f : -1f;
            }

            Vector3 toPlayer = (player.position - transform.position);
            toPlayer.y = 0;
            Vector3 dirToPlayer = toPlayer.normalized;

            Vector3 moveDir = Vector3.zero;

            if (myRole == CombatRole.Rusher)
            {
                moveDir = dirToPlayer;
            }
            else
            {
                Vector3 toPlayerFlat = (player.position - transform.position);
                toPlayerFlat.y = 0;

                float dist = toPlayerFlat.magnitude;

                if (dist <= attackRange * 1.4f)
                {
                    moveDir = toPlayerFlat.normalized;
                }
                else
                {
                    
                    Vector3 sideDir = Quaternion.Euler(0, slotAngle, 0) * player.forward;
                    Vector3 flankPos = player.position + sideDir * surroundDistance;

                    moveDir = (flankPos - transform.position).normalized;
                }
            }

            // Apply movement and rotation
            Vector3 separation = GetSeparationDir();
            Vector3 finalDir = (moveDir + separation * 0.5f).normalized;
            finalDir = ApplyAvoidance(finalDir);

            MoveTowards(finalDir, currentChaseSpeed);
            RotateTowards(dirToPlayer); // Still face player while flanking

            // Transition to Attack
           if (distance <= attackRange * 1.4f)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

                animator.SetBool("Run", false);
                //animator.SetFloat("MoveX", 0);
                //animator.SetFloat("MoveY", 0);

                hasWaitFinished = false;
                attackDelayTimer = 0f;

                chaseDecisionTimer = 0;
                isStrafingInChase = false;
                currentState = State.Attack;
            }

        }

        void MoveTowards(Vector3 dir, float speed)
        {
            ApplyMovementForce(dir * speed);
        }

        void AttackPlayer(float distance)
        {

            if (currentAction == AttackAction.Dodge || currentAction == AttackAction.Dash || currentAction == AttackAction.Attack)
            {
                PerformAction();
                return;
            }

            Vector3 dirToPlayer = (player.position - transform.position);
            dirToPlayer.y = 0;

            if (dirToPlayer != Vector3.zero)
            {
                RotateTowards(dirToPlayer.normalized);
            }

            if (distance > attackRange * 1.6f)
            {
                currentAction = AttackAction.None;
                isPerformingAction = false;
                hasWaitFinished = false;
                attackDelayTimer = 0f;
                currentState = State.Chase;
                return;
            }


            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

            Vector3 separation = GetSeparationDir();

            if (distance < attackRange * 0.7f)
            {
                ApplyMovementForce(separation * 0.5f);
            }

        
            if (!hasWaitFinished)
            {
                attackDelayTimer += Time.fixedDeltaTime;

                if (attackDelayTimer >= initialAttackDelay)
                {
                    hasWaitFinished = true;
                }
                else
                {
                    return;
                }
            }

            if (!isPerformingAction)
            {
                PickNewAction();
            }

            PerformAction();
        }
        void PerformAction()
        {
            if (currentAction == AttackAction.None) return;

            actionTimer += Time.fixedDeltaTime;

            switch (currentAction)
            {
                case AttackAction.Dodge:
                    Dodge();
                    break;
                case AttackAction.Dash:
                    Dash();
                    break;
                case AttackAction.Strafe:
                    break;
            }

            if(actionTimer >= actionDuration){
                isPerformingAction = false;
                currentAction = AttackAction.None;
                actionApplied = false;


                //animator.SetFloat("MoveX", 0, 0.5f, Time.fixedDeltaTime);
                //animator.SetFloat("MoveY", 0, 0.5f, Time.fixedDeltaTime);
                animator.SetBool("Run", false);
                animator.CrossFadeInFixedTime("Idle", 0.2f);

                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
        }
        //void Strafe()
        //{
        //    RotateTowards(player.position);


        //    strafeDirection = GetValidStrafeDirection(strafeDirection);


        //    if (strafeDirection == 0f)
        //    {
        //        ApplyMovementForce(Vector3.zero);

        //        isPerformingAction = false;
        //        currentAction = AttackAction.None;

        //        return;
        //    }

        //    Vector3 toPlayer = (player.position - transform.position);
        //    toPlayer.y = 0;

        //    Vector3 circleDir = Vector3.Cross(Vector3.up, toPlayer.normalized) * strafeDirection;

        //    Vector3 distanceDir = GetDistanceDir();
        //    Vector3 separation = GetSeparationDir();

        //    Vector3 finalDir = (circleDir + distanceDir * 1.5f + separation * 0.8f).normalized;

        //    finalDir = ApplyAvoidance(finalDir);

        //    ApplyMovementForce(finalDir * strafeSpeed);
        //}
        void Dodge()
        {
            if (actionApplied) return;

            actionApplied = true;

            float dir = (Random.value > 0.5f) ? 1f : -1f;

            animator.CrossFadeInFixedTime("Dodge", 0.1f);
            animator.SetFloat("DodgeVelocity", dir);

            Vector3 dodgeDir = transform.right * dir;

            rb.linearVelocity = new Vector3(dodgeDir.x * dodgeForce, rb.linearVelocity.y, dodgeDir.z * dodgeForce);
        }

        void Dash()
        {
            if (actionApplied) return;

            actionApplied = true;

            animator.CrossFadeInFixedTime("Dash", 0.1f);

            Vector3 dashDir = -transform.forward;

            rb.linearVelocity = new Vector3(dashDir.x * dashForce, rb.linearVelocity.y, dashDir.z * dashForce);
        }

        void PickNewAction()
        {
            animator.SetBool("Run", false);

            bool bothBlocked = AreBothSidesBlocked();

            if (bothBlocked)
            {
                AttackAction[] allowed = new AttackAction[]
                {
                    AttackAction.Attack,
                    AttackAction.Idle,
                    AttackAction.Taunt,
                    AttackAction.Dash
                };

                currentAction = allowed[Random.Range(0, allowed.Length)];
            }
            else
            {
          
                AttackAction[] allowed = new AttackAction[]
                {
                    AttackAction.Attack,
                    AttackAction.Dodge,
                    AttackAction.Dash,
                    AttackAction.Idle,
                    AttackAction.Taunt
                };

                currentAction = allowed[Random.Range(0, allowed.Length)];
            }

            actionDuration = Random.Range(minActionTime, maxActionTime);
            actionTimer = 0f;
            isPerformingAction = true;
            actionApplied = false;

            switch (currentAction)
            {
                case AttackAction.Attack:
                    ExecuteRandomAttack();
                    break;

                case AttackAction.Dodge:
                    animator.CrossFadeInFixedTime("Dodge", 0.1f);
                    actionDuration = animator.GetCurrentAnimatorStateInfo(0).length;
                    break;

                case AttackAction.Dash:
                    animator.CrossFadeInFixedTime("Dash", 0.1f);
                    break;

                case AttackAction.Strafe:
                    ExecuteRandomAttack();
                    break;

                case AttackAction.Taunt:
                    ExecuteRandomTaunt();
                    break;

                case AttackAction.Idle:
                    break;
            }
        }

        void ExecuteRandomAttack(){
            if (attackAnimations.Length == 0) return;

            string randomAttack = attackAnimations[Random.Range(0, attackAnimations.Length)];

            animator.CrossFadeInFixedTime(randomAttack, 0.1f);

            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            rb.linearVelocity = transform.forward * attackForwardPush + Vector3.up * rb.linearVelocity.y;

            PerformRayCast();

            lastAttackTime = Time.time;
        }
        void PerformRayCast()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position + transform.forward, 3f, hitLayer);
            foreach(Collider col in colliders)
            {
                col.GetComponent<HealthControl>().TakeDamage(damageAmount);
            }

        }
        void ExecuteRandomTaunt(){
            if (tauntAnimations.Length == 0) return;

            string pickedTaunt = tauntAnimations[Random.Range(0, tauntAnimations.Length)];
            animator.CrossFadeInFixedTime(pickedTaunt, 0.2f);

            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);


            actionDuration = animator.GetCurrentAnimatorStateInfo(0).length;
            if (actionDuration <= 0) actionDuration = 1.5f; 
        }


        bool IsSideBlocked(float dir)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 side = transform.right * dir;

            float checkDistance = 1.5f;

            if (Physics.SphereCast(origin, 0.5f, side, out RaycastHit hit, checkDistance))
            {
                if (hit.collider.CompareTag("Enemy") || ((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        bool AreBothSidesBlocked()
        {
            return IsSideBlocked(-1f) && IsSideBlocked(1f);
        }

        Vector3 GetSeparationDir()
        {
            Vector3 separation = Vector3.zero;
            Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject && hit.CompareTag("Enemy"))
                {
                    Vector3 diff = transform.position - hit.transform.position;
                    separation += diff.normalized / diff.magnitude;
                }
            }
            return separation.normalized;
        }

        Vector3 ApplyAvoidance(Vector3 dir)
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, dir, out hit, avoidanceRadius, obstacleMask))
            {
                Vector3 hitNormal = hit.normal;
                hitNormal.y = 0;
                return (dir + hitNormal * 2f).normalized;
            }
            return dir;
        }

        void ApplyMovementForce(Vector3 targetVelocity)
        {
            if (currentAction == AttackAction.Dodge || currentAction == AttackAction.Dash || currentAction == AttackAction.Attack)
                return;

            Vector3 current = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 target = new Vector3(targetVelocity.x, 0, targetVelocity.z);

            Vector3 smoothed = Vector3.Lerp(current, target, 8f * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(smoothed.x, rb.linearVelocity.y, smoothed.z);
        }

        void RotateTowards(Vector3 dir)
        {
            if (dir == Vector3.zero) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);

            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }


        void UpdateAnimations(Vector3 velocity)
        {
            if (currentAction == AttackAction.Dodge || currentAction == AttackAction.Dash) return;

            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            animator.SetFloat("MoveX", localVelocity.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveY", localVelocity.z, 0.1f, Time.deltaTime);

            bool isMoving = velocity.magnitude > 0.1f;
            animator.SetBool("Run", isMoving && currentState == State.Chase);
        }

        public void TakeDamage(float damage)
        {
            if (currentState == State.Dead)
                return;

            currentHealth -= damage;

            if (currentHealth <= 0)
                Die();
            else
                animator.SetTrigger("Hit");
        }

        void Die()
        {
            currentState = State.Dead;

            animator.SetTrigger("Die");

            rb.linearVelocity = Vector3.zero;

            Collider col = GetComponent<Collider>();
            if (col) col.enabled = false;

            Destroy(gameObject, 3f);
        }

        void OnDrawGizmosSelected(){
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }

}