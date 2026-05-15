using UnityEngine;
public class RecoverState : IEnemyState
{
    private EnemyStateMachine enemy;

    public RecoverState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.motor.Stop();
        enemy.SetMoveDirection(Vector3.zero);
        enemy.anim.PlayRecover();
    }

    public void Tick()
    {
        if (enemy.anim.IsAnimationFinished())
        {
            enemy.SwitchState(new ChaseState(enemy));
        }

        enemy.motor.LookAtTarget(enemy.player.position, 0.3f);
    }

    public void Exit() { }
}