using UnityEngine;

public class DodgeState : IEnemyState
{
    private EnemyStateMachine enemy;
    private float duration = 0.6f;
    private float timer;

    public DodgeState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = duration;
        enemy.anim.PlayDodge();

      
       // enemy.motor.MoveDirection(enemy.GetMoveDirection());
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