using UnityEngine;

public class DashBackState : IEnemyState
{
    private EnemyStateMachine enemy;

    private float dashDuration = 0.4f;
    private float recoveryDelay = 0.25f;

    private float timer;
    private bool dashFinished;

    public DashBackState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = dashDuration;
        dashFinished = false;

        enemy.dodged = true;

        enemy.anim.PlayDashBack();

        Vector3 back = -enemy.transform.forward;
        enemy.motor.Dash(back, enemy.dashForce, dashDuration);
    }

    public void Tick()
    {
        if (enemy.player.GetComponent<HealthControl>().IsDead) return;

        if (enemy.isHit){
            enemy.SwitchState(new DamageState(enemy));
            return;
        }


        enemy.motor.LookAtTarget(enemy.player.position, 2f);

        timer -= Time.deltaTime;


        if (!dashFinished && timer <= 0f)
        {
            dashFinished = true;


            enemy.motor.Stop();

            timer = recoveryDelay;
            return;
        }


        if (dashFinished && timer <= 0f)
        {
             float r = Random.value;
            if (r < 0.25f)
                enemy.SwitchState(new DashForwardState(enemy));
            else if (r < 0.5f)
                enemy.SwitchState(new TauntState(enemy));
        }
    }

    public void Exit()
    {
        enemy.SetRetreatTime();
        enemy.group.ReleaseAttackSlot(enemy);
        enemy.dodged = false;
    }
}