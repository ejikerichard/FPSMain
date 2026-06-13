using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyStateMachine enemy;
    private float assignedAngle;

    private const float FLANK_ORBIT_RADIUS = 2.0f;

    public ChaseState(EnemyStateMachine enemy) { this.enemy = enemy; }

    public void Enter()
    {
        enemy.anim.SetLocomotion(true);
        enemy.anim.useRootMotion = false;
        assignedAngle = enemy.group.RequestFlankAngle(enemy);
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

        // Attack when in range and slot is available
        if (dist <= enemy.attackRange && enemy.group.CanAttack(enemy) && enemy.CanAttackNow())
        {
            enemy.group.ReserveAttackSlot(enemy);
            float r = Random.value;
            if (r < 0.25f)
                enemy.SwitchState(new PreAttackState(enemy));
            else if (r < 0.5f)
                enemy.SwitchState(new TauntState(enemy));

                return;
        }

        if (dist > enemy.attackRange)
        {
            // Move toward the flanking position around the player
            Vector3 flankDir = Quaternion.AngleAxis(assignedAngle, Vector3.up) * enemy.player.forward;
            Vector3 targetPos = enemy.player.position + flankDir * FLANK_ORBIT_RADIUS;
            Vector3 moveDir = (targetPos - enemy.transform.position).normalized;
            enemy.motor.MoveDirection(moveDir);
        }
        else
        {
            enemy.motor.MoveDirection(Vector3.zero);
        }
    }

    public void Exit()
    {
        enemy.group.ReleaseFlankAngle(enemy);
    }
}