using UnityEngine;

public class StunnedState : IEnemyState
{
    private EnemyStateMachine enemy;
    private float duration;
    private float timer;

    public StunnedState(EnemyStateMachine enemy, float stunTime = 1.5f)
    {
        this.enemy = enemy;
        duration = stunTime;
    }

    public void Enter()
    {
        timer = duration;
        enemy.anim.PlayStunned();
        enemy.motor.Stop();
    }

    public void Tick()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            enemy.SwitchState(new ChaseState(enemy));
        }

        enemy.motor.LookAtTarget(enemy.player.position, 0.3f);
    }

    public void Exit() { }
}