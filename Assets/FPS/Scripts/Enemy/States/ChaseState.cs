using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyStateMachine enemy;
    private float assignedAngle;

    private const float FLANK_ORBIT_RADIUS = 2.0f;


    private const float ATTACK_RANGE_EXIT_BUFFER = 0.5f;
    private bool inAttackPosition = false;


    private const float ALIGN_THRESHOLD_DEG = 20f;

    public ChaseState(EnemyStateMachine enemy) { this.enemy = enemy; }

    public void Enter()
    {
        enemy.anim.SetLocomotion(true);
        enemy.anim.useRootMotion = false;
        assignedAngle = enemy.group.RequestFlankAngle(enemy);
        inAttackPosition = false;
    }

    public void Tick()
    {
        if (enemy.isHit)
        {
            enemy.SwitchState(new DamageState(enemy));
            return;
        }

        float dist = enemy.DistanceToPlayer();
        enemy.motor.LookAtTarget(enemy.player.position);

        if (inAttackPosition && dist > enemy.attackRange + ATTACK_RANGE_EXIT_BUFFER)
            inAttackPosition = false;
        else if (!inAttackPosition && dist <= enemy.attackRange)
            inAttackPosition = true;

        if (inAttackPosition)
        {

            enemy.motor.MoveDirection(Vector3.zero);

            if (enemy.group.CanAttack(enemy) && enemy.CanAttackNow())
            {
                enemy.group.ReserveAttackSlot(enemy);
                float r = Random.value;

                if (r < 0.25f)
                    enemy.SwitchState(new PreAttackState(enemy));
                else if (r < 0.5f)
                    enemy.SwitchState(new TauntState(enemy));
                else if (r < 0.75f)
                    enemy.SwitchState(new DodgeState(enemy));
                else
                    enemy.SwitchState(new DashBackState(enemy));
            }


            return;
        }

        Vector3 toEnemyFlat = enemy.transform.position - enemy.player.position;
        toEnemyFlat.y = 0;
        float currentRadius = toEnemyFlat.magnitude;

        Vector3 flankDir = Quaternion.AngleAxis(assignedAngle, Vector3.up) * enemy.player.forward;
        Vector3 targetPos = enemy.player.position + flankDir * FLANK_ORBIT_RADIUS;
        Vector3 toTarget = targetPos - enemy.transform.position;
        toTarget.y = 0;

        Vector3 moveDir;

        if (currentRadius < 0.05f)
        {

            moveDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
        }
        else
        {
            float currentRelAngle = Vector3.SignedAngle(enemy.player.forward, toEnemyFlat, Vector3.up);
            float angleDiff = Mathf.DeltaAngle(currentRelAngle, assignedAngle);

            if (Mathf.Abs(angleDiff) <= ALIGN_THRESHOLD_DEG)
            {

                moveDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
            }
            else
            {

                float safeRadius = Mathf.Max(FLANK_ORBIT_RADIUS, enemy.attackRange + 0.5f);

                Vector3 tangentPositive = Quaternion.AngleAxis(90f, Vector3.up) * toEnemyFlat.normalized;
                Vector3 tangent = (angleDiff >= 0f) ? tangentPositive : -tangentPositive;

                float radiusError = currentRadius - safeRadius;
                Vector3 radial = -toEnemyFlat.normalized * Mathf.Clamp(radiusError, -1f, 1f);

                moveDir = (tangent + radial * 0.6f).normalized;
            }
        }

        enemy.motor.MoveDirection(moveDir);
    }

    public void Exit()
    {
        enemy.group.ReleaseFlankAngle(enemy);
    }
}