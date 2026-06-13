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

        enemy.anim.PlayDashBack();

        Vector3 back = -enemy.transform.forward;
        enemy.motor.Dash(back, 8f, dashDuration);
    }

    public void Tick()
    {
        if (enemy.isHit)
        {
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
            enemy.SwitchState(new DashForwardState(enemy));
        }
    }

    public void Exit() {
        enemy.SetRetreatTime();
    }
}