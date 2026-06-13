using UnityEngine;

public class TauntState : IEnemyState
{
    private EnemyStateMachine enemy;
    private float timer;
    private const float TAUNT_DURATION = 2.2f;

    public TauntState(EnemyStateMachine enemy) { this.enemy = enemy; }

    public void Enter()
    {
        enemy.motor.Stop();
        enemy.anim.SetLocomotion(false);

        int randTaunt = Random.Range(0, 2);
        enemy.anim.PlayTaunt(randTaunt);

        timer = TAUNT_DURATION;
    }

    public void Tick()
    {
        if (enemy.isHit)
        {
            enemy.SwitchState(new DamageState(enemy));
            return;
        }


        enemy.motor.LookAtTarget(enemy.player.position);
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // After taunt, go straight to attack if still in range
            if (enemy.DistanceToPlayer() <= enemy.attackRange && enemy.group.CanAttack(enemy))
                enemy.SwitchState(new PreAttackState(enemy));
            else
                enemy.SwitchState(new ChaseState(enemy));
        }

    }

    public void Exit()
    {
        enemy.anim.SetLocomotion(true);
    }
}