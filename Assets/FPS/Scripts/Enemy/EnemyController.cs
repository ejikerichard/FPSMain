using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace FPS
{
    public class EnemyController : MonoBehaviour
    {
        public enum EnemyState { Idle, Chase, Combat, Attack, Dodge, Dead }

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform target;

        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float chaseRange = 15f;
        [SerializeField] private float strafeDistance = 3f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float dodgeChance = 0.15f;
        [SerializeField] private float attackLungeForce = 2.5f;
        [SerializeField] private float lungeDuration = 0.25f;

        private static System.Collections.Generic.List<EnemyController> allEnemies =
        new System.Collections.Generic.List<EnemyController>();


        [Header("Difficulty")]
        [SerializeField] private float aggressionMultiplier = 1f;

        [Header("Boss Settings")]
        [SerializeField] private bool isBoss = false;
        [SerializeField] private float phaseTwoHealthThreshold = 0.5f;

        private EnemyState currentState;
        private float attackTimer;
        private float health = 100f;
        private bool phaseTwo = false;
        private float strafeDirection = 1f;
        private bool isAttacking;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = transform.GetChild(0).GetComponent<Animator>();
            target = GameObject.FindWithTag("Player").transform;

            allEnemies.Add(this);


            currentState = EnemyState.Idle;
            attackTimer = 0f;
        }

        void Update()
        {
            if (target == null || currentState == EnemyState.Dead)
                return;

            attackTimer -= Time.deltaTime;

            float dist = Vector3.Distance(transform.position, target.position);

            if (isBoss)
                HandleBossPhase();

            if (dist <= attackRange)
                EnterCombat();
            else if (dist <= chaseRange)
                ChasePlayer();
            else
                Idle();
        }

        // =============================
        // STATES
        // =============================

        void Idle(){
            currentState = EnemyState.Idle;
            animator.SetBool("BearRun", false);
        }

        void ChasePlayer(){
            currentState = EnemyState.Chase;
            agent.isStopped = false;
            agent.SetDestination(target.position);

            animator.SetBool("BearRun", true);
        }

        void EnterCombat(){
            currentState = EnemyState.Combat;

            animator.SetBool("BearRun", false);

            LookAtTarget();
            StrafeAroundPlayer();

            if(attackTimer <= 0f && !isAttacking){
                if (Random.value < dodgeChance)
                    StartCoroutine(DodgeRoutine());
                else
                    StartCoroutine(AttackRoutine());
            }
        }

        // =============================
        // STRAFING
        // =============================

        void StrafeAroundPlayer(){
            int index = allEnemies.IndexOf(this);
            int total = allEnemies.Count;

            if (total == 0) return;

            // Dynamic radius grows with enemy count
            float dynamicRadius = attackRange + (total * 0.6f);

            float angleStep = 360f / total;
            float angle = angleStep * index;

            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(rad),
                0,
                Mathf.Sin(rad)
            ) * dynamicRadius;

            Vector3 desiredPosition = target.position + offset;

            // Personal separation force (anti overlap)
            Vector3 separation = Vector3.zero;

            foreach(var enemy in allEnemies){
                if(enemy == this) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);

                if(dist < 1.5f){
                    separation += (transform.position - enemy.transform.position).normalized * 2f;
                }
            }

            desiredPosition += separation;

            agent.isStopped = false;
            agent.SetDestination(desiredPosition);
        }


        // =============================
        // ATTACK
        // =============================

        IEnumerator AttackRoutine()
        {
            currentState = EnemyState.Attack;
            isAttacking = true;

            attackTimer = attackCooldown / aggressionMultiplier;

            agent.isStopped = true;   // stop pathfinding during lunge

            LookAtTarget();

            int attackType = Random.Range(0, 2);

            if (attackType == 0)
                animator.SetTrigger("LightAttack");
            else
                animator.SetTrigger("HeavyAttack");

            // Wait small delay before lunge (sync with swing start)
            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(PerformLunge());

            yield return new WaitForSeconds(0.6f);

            agent.isStopped = false;

            isAttacking = false;
        }

        IEnumerator PerformLunge(){
            float timer = 0f;

            Vector3 forwardDir = (target.position - transform.position).normalized;
            forwardDir.y = 0;

            while (timer < lungeDuration)
            {
                agent.Move(forwardDir * attackLungeForce * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // =============================
        // DODGE
        // =============================

        IEnumerator DodgeRoutine(){
            currentState = EnemyState.Dodge;

            //animator.SetTrigger("Dodge");

            Vector3 dodgeDir = (transform.position - target.position).normalized;
            agent.Move(dodgeDir * 2f);

            yield return new WaitForSeconds(0.6f);
        }

        // =============================
        // LOOK AT PLAYER
        // =============================

        void LookAtTarget(){
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;

            if (direction == Vector3.zero)
                return;

            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 6f);
        }

        // =============================
        // DAMAGE SYSTEM
        // =============================

        public void TakeDamage(float damage){
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
            //agent.isStopped = true;
            //animator.SetTrigger("Die");
            Destroy(gameObject, 0.5f);

            allEnemies.Remove(this);
        }

        // =============================
        // BOSS PHASE SYSTEM
        // =============================

        void HandleBossPhase()
        {
            if (!phaseTwo && health <= 100f * phaseTwoHealthThreshold)
            {
                phaseTwo = true;
                aggressionMultiplier = 1.5f;
                attackCooldown *= 0.7f;
                //animator.SetTrigger("PhaseTwo");
            }
        }
    }
}
