using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    public enum EnemyRole
    {
        DirectChaser,
        Flanker
    }

    public EnemyRole role;
    public float strafeSign;

    public Transform player;
    public float detectRange = 15f;
    public float attackRange = 2.5f;

    public float attackCooldown = 1.5f;
    private float lastAttackTime = -999f;

    public float retreatCooldown = 1.5f;
    private float lastRetreatTime = -999f;

    private float stateLockTimer;

    public float blockCooldown = 0.5f;
    private float lastBlockTime = -999f;

    private float decisionTimer;
    private Vector3 currentMoveDir;

    public EnemyMotor motor;
    public EnemyAnimatorSync anim;
    public EnemyGroupManager group;

    private IEnemyState currentState;

    void Start()
    {
        motor = GetComponent<EnemyMotor>();
        anim = GetComponent<EnemyAnimatorSync>();
        group = GameObject.FindAnyObjectByType<EnemyGroupManager>();

        player = GameObject.FindGameObjectWithTag("Player").transform;

        motor.player = player;

        motor.stoppingDistance = attackRange - 0.3f;

        SwitchState(new IdleState(this));
        group.Register(this);
    }

    void Update()
    {
        currentState?.Tick();
    }

    public void SwitchState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
    public bool IsInState<T>() where T : IEnemyState
    {
        return currentState is T;
    }

    public float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    public bool CanAttackNow()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    public void SetAttackTime()
    {
        lastAttackTime = Time.time;
    }

    public void SetRetreatTime()
    {
        lastRetreatTime = Time.time;
    }
    public bool ShouldRecalculateDecision(float interval = 0.3f)
    {
        if (Time.time > decisionTimer)
        {
            decisionTimer = Time.time + interval;
            return true;
        }
        return false;
    }

    public void SetMoveDirection(Vector3 dir)
    {
        currentMoveDir = dir.normalized;
    }

    public Vector3 GetMoveDirection()
    {
        return currentMoveDir;
    }

    public bool IsRetreatingRecently()
    {
        return Time.time < lastRetreatTime + retreatCooldown;
    }

    public bool CanRepositionFromBlock()
    {
        return Time.time > lastBlockTime + blockCooldown;
    }

    public void SetBlockTime()
    {
        lastBlockTime = Time.time;
    }
    public bool CanSwitchState()
    {
        return Time.time > stateLockTimer;
    }

    public void LockState(float time)
    {
        stateLockTimer = Time.time + time;
    }
}