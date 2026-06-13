using UnityEngine;
public class FlankState : IEnemyState
{
    private EnemyStateMachine enemy;

    public FlankState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        //enemy.anim.PlayRun();
    }

    public void Tick()
    {

        if (enemy.isHit)
        {
            enemy.SwitchState(new DamageState(enemy));
            return;
        }

        Vector3 toPlayer = (enemy.player.position - enemy.transform.position).normalized;

        Vector3 side = Vector3.Cross(Vector3.up, toPlayer) * enemy.strafeSign;
        Vector3 desired = (side + toPlayer * 0.5f).normalized;

        enemy.motor.LookAtTarget(enemy.player.position);
        enemy.motor.MoveDirection(desired);

        if (enemy.DistanceToPlayer() <= enemy.attackRange)
        {
            enemy.SwitchState(new AttackState(enemy));
        }
    }

    public void Exit() { }
}