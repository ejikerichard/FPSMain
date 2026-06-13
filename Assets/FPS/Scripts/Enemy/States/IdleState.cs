using UnityEngine;
public class IdleState : IEnemyState
{
    private EnemyStateMachine enemy;

    public IdleState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.anim.SetLocomotion(false);
        enemy.motor.Stop();

        int randomIdle = Random.Range(0, 2);
        enemy.anim.PlayIdle(randomIdle);
    }
    public void Tick()
    {
        if (enemy.isHit)
        {
            enemy.SwitchState(new DamageState(enemy));
            return;
        }


        if (enemy.DistanceToPlayer() < enemy.detectRange)
        {
            // 🎲 50/50 role
            enemy.role = (Random.value < 0.5f)
                ? EnemyStateMachine.EnemyRole.DirectChaser
                : EnemyStateMachine.EnemyRole.Flanker;

            // assign strafe direction once
            enemy.strafeSign = (Random.value < 0.5f) ? -1f : 1f;

            enemy.SwitchState(new ChaseState(enemy));
        }
    }

    public void Exit() { }
}