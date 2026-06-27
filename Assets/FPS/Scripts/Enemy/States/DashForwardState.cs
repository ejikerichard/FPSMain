using UnityEngine;

public class DashForwardState : IEnemyState
{
    private EnemyStateMachine enemy;

    private float dashDuration = 0.25f;
    private float recoveryDelay = 0.2f;

    private float timer;
    private bool dashFinished;

    public DashForwardState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = dashDuration;
        dashFinished = false;

        Vector3 dir = (enemy.player.position - enemy.transform.position);
        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
        {
            enemy.transform.rotation = Quaternion.LookRotation(dir);
        }

        enemy.anim.PlayDashForward();

        enemy.motor.Dash(dir.normalized, 10f, dashDuration);
    }

    public void Tick()
    {
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
            enemy.SwitchState(new IdleState(enemy));
        }
    }

    public void Exit() { }
}