using UnityEngine;

public class DamageState : IEnemyState
{
    private EnemyStateMachine enemy;

    public DamageState(EnemyStateMachine enemy){ this.enemy = enemy; }

    public void Enter()
    {
        enemy.motor.Stop();
        enemy.anim.SetLocomotion(false);

        enemy.anim.PlayDamage();
    }

    public void Exit()
    {
        enemy.anim.SetLocomotion(true);
        enemy.isHit = false;
    }

    public void Tick()
    {
        if(enemy.anim.IsAnimationFinished())
        {
            enemy.SwitchState(new IdleState(enemy));
        }
    }
}
