using UnityEngine;

public class PreAttackState : IEnemyState
{
    private EnemyStateMachine enemy;
    private float timer;

    public PreAttackState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = 0.15f; // 🔥 tweak: 0.1–0.25

        enemy.motor.Stop();
        enemy.SetMoveDirection(Vector3.zero);

        // 🔥 this triggers idle
        enemy.anim.SetLocomotion(false);
        enemy.anim.PlayIdle();
    }

    public void Tick()
    {
        timer -= Time.deltaTime;

        enemy.motor.LookAtTarget(enemy.player.position);

        if (timer <= 0f)
        {
            enemy.SwitchState(new AttackState(enemy));
        }
    }

    public void Exit() { }
}