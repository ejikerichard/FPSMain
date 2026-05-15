using UnityEngine;

public class AttackState : IEnemyState
{
    private EnemyStateMachine enemy;

    public AttackState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.motor.Stop();
        enemy.SetMoveDirection(Vector3.zero);

        enemy.anim.SetLocomotion(false);


        enemy.anim.ForceStopMovement();

        enemy.anim.PlayAttack();
        enemy.SetAttackTime();
    }

    public void Tick()
    {
        enemy.motor.LookAtTarget(enemy.player.position, 2f);

        if (!enemy.anim.IsAnimationFinished()) return;

        float r = Random.value;

        if (r < 0.25f)
            enemy.SwitchState(new DodgeState(enemy));
        else if (r < 0.5f)
            enemy.SwitchState(new DashBackState(enemy));
        else
            enemy.SwitchState(new RecoverState(enemy));
    }

    public void Exit()
    {
        enemy.group.ReleaseAttackSlot(enemy);

        enemy.anim.SetLocomotion(true);
    }
}