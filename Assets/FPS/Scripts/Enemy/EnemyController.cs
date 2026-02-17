using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

namespace FPS
{
    public class EnemyController : MonoBehaviour
    {
        public enum EnemyState { Idle, Combat, Attack, Dead }

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform target;

        [Header("Ranges")]
        [SerializeField] private float chaseRange = 20f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float orbitDistance = 2.4f;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitSpeed = 80f;
        [SerializeField] private float flankBias = 0.6f; // higher = more go behind

        [Header("Combat")]
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float lungeForce = 4f;
        [SerializeField] private float lungeDuration = 0.25f;

        [Header("Spacing")]
        [SerializeField] private float minEnemySpacing = 1.5f;
        [SerializeField] private float separationStrength = 3f;

        [Header("Attack Limiter")]
        [SerializeField] private int maxSimultaneousAttackers = 2;

        [Header("LOS Settings")]
        [SerializeField] private LayerMask obstacleMask; // Set this to your "World/Obstacle" layer
        private bool hasLineOfSight;

        [Header("Movement Smoothing")]
        [SerializeField] private float smoothTime = 0.1f;
        private Vector3 moveDirection;
        private Vector3 smoothVelocity;
        [SerializeField] private float animationDeadzone = 0.15f; // Minimum move speed to animate
        [SerializeField] private float animationSmoothTime = 0.1f; // How fast transitions happen
        [SerializeField] private float movementSmoothing = 8f; // Higher = Snappier, Lower = Weightier
        [SerializeField] private LayerMask wallLayer; // Set to your environment layer
        private Vector3 currentVelocity;

        [Header("Decision Logic")]
        [SerializeField] private float decisionCooldown = 1.5f; // Time spent strafing before attacking
        private float decisionTimer;
        private bool isStrafing; // Local lock for the "Strafe" act

        private static List<EnemyController> allEnemies = new List<EnemyController>();
        private static int currentAttackers = 0;

        [Header("Grounding")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundOffset = 0.1f; // Adjust based on model pivot


        private EnemyState currentState;
        private float attackTimer;
        private float health = 100f;
        private bool isAttacking;
        private float orbitDirection;
        private Vector3 lastPos;
        // =====================================================

        void OnEnable() => allEnemies.Add(this);
        void OnDisable() => allEnemies.Remove(this);

        void Start()
        {
            animator = transform.GetChild(0).GetComponent<Animator>();
            target = GameObject.FindGameObjectWithTag("Player").transform;

            orbitDirection = Random.value > 0.5f ? 1f : -1f;

        }

        void Update()
        {
            if (target == null || currentState == EnemyState.Dead)
                return;

            attackTimer -= Time.deltaTime;

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > chaseRange)
                Idle();
            else
                CombatLogic(dist);
        }

        // =====================================================
        // IDLE
        // =====================================================

        void Idle()
        {
            currentState = EnemyState.Idle;

            animator.SetBool("BearRun", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveZ", 0);
        }

        // =====================================================
        // MAIN COMBAT
        // =====================================================

        void CombatLogic(float dist){
            // 1. If currently lunging/animating an attack, don't do anything else
            if (isAttacking) return;

            // 2. If we are outside attack range, just move to player
            if(dist > attackRange){
                isStrafing = false; // Reset decision
                OrbitPlayer();
                return;
            }

            // 3. INSIDE ATTACK RANGE: Make a decision if we haven't already
            decisionTimer -= Time.deltaTime;

            if(decisionTimer <= 0){
                // 50/50 chance to Strafe or Attack
                if(Random.value > 0.5f){
                    StartCoroutine(PerformAttackAction());
                }
                else{
                    StartCoroutine(PerformStrafeAction());
                }
            }
        }

        bool CheckLineOfSight()
        {
            Vector3 directionToPlayer = (target.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, target.position);

            // Raycast from enemy eyes (transform.position + offset) to player center
            // Returns true if it hits NOTHING (meaning path is clear) or hits the Player
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleMask))
            {
                if (hit.collider.CompareTag("Player"))
                    return true;

                return false; // Hit a wall or pillar
            }

            return true; // Path is clear
        }
        // =====================================================
        // TRUE ORBIT STRAFING + BEHIND MOVEMENT
        // =====================================================

        void OrbitPlayer()
        {
            currentState = EnemyState.Combat;

            float dist = Vector3.Distance(transform.position, target.position);

            // 1. DIRECTION LOGIC
            Vector3 desiredDir = Vector3.zero;

            if (dist > attackRange + 1f) // Outside range: Move toward player
            {
                desiredDir = (target.position - transform.position).normalized;
            }
            else // Inside range: Start Strafing
            {
                Vector3 toPlayer = (target.position - transform.position).normalized;
                Vector3 strafe = Vector3.Cross(toPlayer, Vector3.up) * orbitDirection;

                // Maintain distance: if too close, push away; if too far, pull in
                Vector3 distanceCorrection = toPlayer * (dist - orbitDistance);

                desiredDir = (strafe + distanceCorrection + SeparationForce()).normalized;
            }

            // 2. THE FLIGHT FIX: Kill the Y movement
            desiredDir.y = 0;

            // 3. SMOOTH MOVEMENT
            moveDirection = Vector3.Lerp(moveDirection, desiredDir, Time.deltaTime * movementSmoothing);
            Vector3 nextPos = transform.position + (moveDirection * (orbitSpeed * 0.05f) * Time.deltaTime);

            // 4. THE GROUND SNAP: Raycast down to find the floor
            RaycastHit hit;
            if (Physics.Raycast(nextPos + Vector3.up * 2f, Vector3.down, out hit, 5f, groundLayer))
            {
                nextPos.y = hit.point.y + groundOffset;
            }

            transform.position = nextPos;

            UpdateAnimation();
            LookAtTarget();
        }

        Vector3 SeparationForce()
        {
            Vector3 force = Vector3.zero;
            foreach (var enemy in allEnemies)
            {
                if (enemy == this || enemy == null) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minEnemySpacing)
                {
                    // Gentle nudge away from neighbors
                    force += (transform.position - enemy.transform.position).normalized * (1f - dist / minEnemySpacing);
                }
            }
            return force * separationStrength;
        }
        IEnumerator PerformStrafeAction()
        {
            isStrafing = true;
            decisionTimer = Random.Range(1f, 2.5f); // How long to strafe for

            // Flip direction randomly when starting a new strafe
            orbitDirection = Random.value > 0.5f ? 1f : -1f;

            while(decisionTimer > 0 && !isAttacking){
                OrbitPlayer(); // Use your existing smooth orbit logic here
                yield return null;
            }

            isStrafing = false;
        }

        IEnumerator PerformAttackAction()
        {
            // Check if we are allowed to attack (Global Limiter)
            if(currentAttackers < maxSimultaneousAttackers && attackTimer <= 0){
                yield return StartCoroutine(AttackRoutine()); // Your existing AttackRoutine
                decisionTimer = decisionCooldown; // Pause before next decision
            }
            else{
                // If too many enemies are already attacking, just strafe instead
                yield return StartCoroutine(PerformStrafeAction());
            }
        }
        // =====================================================
        // ATTACK
        // =====================================================

        IEnumerator AttackRoutine()
        {
            currentAttackers++;
            isAttacking = true;
            attackTimer = attackCooldown + Random.Range(0f, 0.5f);
;
            LookAtTarget();

            animator.SetTrigger(Random.value > 0.5f ? "LightAttack" : "HeavyAttack");

            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(LungeForward());
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
            currentAttackers--;
        }

        IEnumerator LungeForward()
        {
            float timer = 0f;
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;

            while (timer < lungeDuration)
            {
                transform.Translate(dir * lungeForce * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // =====================================================
        // ANIMATION
        // =====================================================

        void UpdateAnimation()
        {
            // 1. Calculate the magnitude (speed) of current movement
            float currentMoveSpeed = moveDirection.magnitude;

            // 2. Check if we are actually moving significantly
            bool isMoving = currentMoveSpeed > animationDeadzone;

            // 3. Update the Boolean for the "BearRun" state
            animator.SetBool("BearRun", isMoving);

            if (isMoving)
            {
                // Convert world moveDirection to local space (Left/Right, Forward/Back)
                Vector3 localDir = transform.InverseTransformDirection(moveDirection);

                // Normalize the values so they stay between -1 and 1 for the Blend Tree
                // Using 'DampTime' here prevents the "snapping" between animations
                animator.SetFloat("MoveX", localDir.x, animationSmoothTime, Time.deltaTime);
                animator.SetFloat("MoveZ", localDir.z, animationSmoothTime, Time.deltaTime);
            }
            else
            {
                // Force parameters to 0 so the Blend Tree returns to the center (Idle)
                animator.SetFloat("MoveX", 0, animationSmoothTime, Time.deltaTime);
                animator.SetFloat("MoveZ", 0, animationSmoothTime, Time.deltaTime);
            }
        }

        // =====================================================
        // ROTATION
        // =====================================================

        void LookAtTarget()
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;

            if (direction == Vector3.zero)
                return;

            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        // =====================================================
        // DAMAGE
        // =====================================================

        public void TakeDamage(float damage)
        {
            if (currentState == EnemyState.Dead)
                return;

            health -= damage;
            animator.SetTrigger("Hit");

            if (health <= 0)
                Die();
        }

        void Die()
        {
            currentState = EnemyState.Dead;
            animator.SetTrigger("Die");
            Destroy(gameObject, 4f);
        }
    }
}
