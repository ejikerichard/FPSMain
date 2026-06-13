using UnityEngine;
using System.Collections.Generic;
using System;

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
    public LayerMask hitLayer;
    public float detectRange = 15f;
    public float attackRange = 2.5f;

    public float attackCooldown = 1.5f;
    private float lastAttackTime = -999f;

    public float retreatCooldown = 1.5f;
    private float lastRetreatTime = -999f;

    private float stateLockTimer;
    private float damageAmount = 10f;

    public float blockCooldown = 0.5f;
    private float lastBlockTime = -999f;

    private float decisionTimer;
    private Vector3 currentMoveDir;

    private float currentHealth = 100f;

    public bool isHit = false;

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
    public void PerformRaycast()
    {
        //Collider[] colliders = Physics.OverlapSphere(transform.position + transform.forward, 3f, hitLayer);
        //foreach (Collider col in colliders)
        //{
        //    col.GetComponent<HealthControl>().TakeDamage(damageAmount);

        //    //Vector3 directionPlayer = (col.transform.position - transform.position).normalized;
        //    //float dotProduct = Vector3.Dot(transform.forward, directionPlayer);
        //    //if (dotProduct > angleThreshold)
        //    //{
        //    //    Vector3 pushdir = (transform.position - col.transform.position).normalized;

        //    //    col.GetComponent<Rigidbody>().AddForce(pushdir * 10f, ForceMode.Impulse);
        //    //}
        //    Debug.Log("Player hit for " + damageAmount + " damage!");
        //}
    }

    public void TakeDamage(float amount)
    {
        if(currentHealth <= 0f)
            return;

        currentHealth -= amount;
        isHit = true;
        Debug.Log("Enemy took " + amount + " damage! Current health: " + currentHealth);
        if (currentHealth <= 0f)
        {
           // Die();
        }
    }
}