using UnityEngine;

public class RepositionState : IEnemyState
{
    private EnemyStateMachine enemy;
    private Vector3 strafeDir;

    public RepositionState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.anim.SetLocomotion(true);
        enemy.motor.disableAutoRotation = true;

        Vector3 toPlayer = enemy.player.position - enemy.transform.position;
        toPlayer.y = 0;

        Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized);
        strafeDir = side.normalized * enemy.strafeSign;
    }

    public void Tick()
    {
        float dist = enemy.DistanceToPlayer();

        if (dist > enemy.attackRange + 1.5f && enemy.CanSwitchState())
        {
            enemy.LockState(0.5f);
            enemy.SwitchState(new ChaseState(enemy));
            return;
        }

        enemy.motor.LookAtTarget(enemy.player.position);

    
        if (enemy.ShouldRecalculateDecision(0.4f))
        {
            Vector3 toPlayer = (enemy.player.position - enemy.transform.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer) * enemy.strafeSign;

            enemy.SetMoveDirection(side);
        }

        enemy.motor.MoveDirection(enemy.GetMoveDirection());
    }

    public void Exit()
    {
        enemy.motor.isStrafing = false;
        enemy.motor.disableAutoRotation = false;
    }
}