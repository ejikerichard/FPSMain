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
        enemy.dodged = true;
        enemy.anim.PlayDodge();
    }

    public void Tick()
    {
        if (enemy.player.GetComponent<HealthControl>().IsDead) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            enemy.SwitchState(new ChaseState(enemy));
        }
        enemy.motor.LookAtTarget(enemy.player.position, 0.3f);
    }

    public void Exit()
    {

        enemy.group.ReleaseAttackSlot(enemy);
        enemy.dodged = false;
    }
}