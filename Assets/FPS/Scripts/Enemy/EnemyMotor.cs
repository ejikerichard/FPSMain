using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMotor : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    private Rigidbody rb;
    public Transform player;
    private Vector3 target;
    private bool hasTarget;

    public float blockCheckDistance = 1.2f;
    public LayerMask enemyLayer;

    public float stoppingDistance = 1.8f;
    private float dashTimer;

    public float separationRadius = 1.2f;
    public float separationStrength = 2f;

    public bool isStrafing = false;
    private Vector3 strafeDirection;
    public bool disableAutoRotation = false;
    public Vector3 LastMoveInput { get; private set; }

    // The direction ChaseState (or any state) wants to move this frame
    private Vector3 requestedDir = Vector3.zero;
    private bool hasMoveRequest = false;

    // Committed steered direction — held for MIN_STEER_HOLD seconds to stop jitter
    private Vector3 committedSteerDir = Vector3.zero;
    private float steerHoldTimer = 0f;
    private const float MIN_STEER_HOLD = 0.25f; // seconds before we re-evaluate a blocked path

    // Last resolved direction — used for animation, not smoothed
    private Vector3 lastResolvedDir = Vector3.zero;

    private bool explicitStopRequested = false;

    private float noRequestTimer = 0f;
    private const float NO_REQUEST_TIMEOUT = 0.1f; // ~6 physics frames at 50Hz

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.linearDamping = 15f;       // kills residual velocity within one frame
        rb.angularDamping = 15f;
    }
    public void MoveTo(Vector3 pos)
    {
        isStrafing = false;
        target = pos;
        hasTarget = true;
    }

    public void Stop()
    {
        hasTarget = false;
        isStrafing = false;
        hasMoveRequest = false;
        explicitStopRequested = false;
        requestedDir = Vector3.zero;
        lastResolvedDir = Vector3.zero;
        committedSteerDir = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        LastMoveInput = Vector3.zero;

        var animSync = GetComponent<EnemyAnimatorSync>();
        if (animSync != null)
        {
            animSync.useRootMotion = false; 
            animSync.UpdateMovement(Vector3.zero, player);
        }
    }

    private Vector3 dashVelocity = Vector3.zero;

    public void Dash(Vector3 direction, float force, float duration)
    {
        direction.y = 0;
        hasTarget = false;
        dashTimer = duration;
        dashVelocity = direction.normalized * force;
        rb.linearVelocity = dashVelocity;
    }

    public bool IsBlockedForward()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        return Physics.Raycast(origin, transform.forward, blockCheckDistance, enemyLayer);
    }

    public void SetStrafe(Vector3 dir)
    {
        isStrafing = true;
        hasTarget = false;
        strafeDirection = dir.normalized;
    }

    public Vector3 GetVelocity() => rb.linearVelocity;
    public bool HasTarget() => hasTarget;


    public void MoveDirection(Vector3 desiredDir)
    {
        desiredDir.y = 0;
        if (desiredDir.sqrMagnitude > 0.01f)
        {
            requestedDir = desiredDir.normalized;
            explicitStopRequested = false;
        }
        else
        {
            requestedDir = Vector3.zero;
            explicitStopRequested = true;
        }
        hasMoveRequest = true;
    }
    void FixedUpdate()
    {
        var anim = GetComponent<EnemyAnimatorSync>();

        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = dashVelocity; 
            anim?.UpdateMovement(transform.InverseTransformDirection(rb.linearVelocity), player);
            return;
        }

        if (steerHoldTimer > 0f)
            steerHoldTimer -= Time.fixedDeltaTime;

        if (hasMoveRequest)
        {
            noRequestTimer = 0f;

            if (explicitStopRequested)
            {
                lastResolvedDir = Vector3.zero;
                committedSteerDir = Vector3.zero;
                steerHoldTimer = 0f;
            }
            else if (requestedDir.sqrMagnitude > 0.01f)
            {
                lastResolvedDir = ResolveDirection(requestedDir);
            }

            hasMoveRequest = false;
            explicitStopRequested = false;
        }
        else
        {
            noRequestTimer += Time.fixedDeltaTime;

            if (noRequestTimer >= NO_REQUEST_TIMEOUT)
            {
                lastResolvedDir = Vector3.zero;
                committedSteerDir = Vector3.zero;
            }
        }


        LastMoveInput = lastResolvedDir;

  
        if (lastResolvedDir.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector3.zero;  
            rb.angularVelocity = Vector3.zero;
            anim?.UpdateMovement(Vector3.zero, player);
            return;
        }

        Vector3 velocity = lastResolvedDir * moveSpeed;
        velocity.y = 0f;  
        rb.linearVelocity = velocity;

        if (lastResolvedDir.sqrMagnitude > 0.01f)
        {
            Vector3 sep = ComputeSeparation();
            velocity.x += sep.x;
            velocity.z += sep.z;
        }

        rb.linearVelocity = velocity;

        Vector3 localInput = transform.InverseTransformDirection(lastResolvedDir);
        anim?.UpdateMovement(localInput, player);
    }

    private Vector3 ResolveDirection(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float castRadius = 0.3f;
        float castDist = blockCheckDistance;

        if (steerHoldTimer > 0f && committedSteerDir.sqrMagnitude > 0.01f)
        {
            bool committedBlocked = Physics.SphereCast(origin, castRadius, committedSteerDir, out _, castDist, enemyLayer);
            if (!committedBlocked)
                return committedSteerDir;


        }

        bool desiredBlocked = Physics.SphereCast(origin, castRadius, desiredDir, out RaycastHit hit, castDist, enemyLayer);

        if (!desiredBlocked)
        {
            committedSteerDir = Vector3.zero;
            steerHoldTimer = 0f;
            return desiredDir;
        }


        if (hit.collider != null && GetInstanceID() < hit.collider.GetInstanceID())
        {
            committedSteerDir = Vector3.zero;
            steerHoldTimer = 0f;
            return Vector3.zero;
        }

        Vector3 freeDir = SweepForFreeDirection(desiredDir, origin, castRadius, castDist);

        committedSteerDir = freeDir;
        steerHoldTimer = MIN_STEER_HOLD;

        return freeDir;
    }

    private Vector3 SweepForFreeDirection(Vector3 desiredDir, Vector3 origin, float castRadius, float castDist)
    {
        int[] angles = { 20, 40, 60, 80, 100, 120, 140, 160 };

        foreach (int angle in angles)
        {
            Vector3 rightDir = Quaternion.AngleAxis(angle, Vector3.up) * desiredDir;
            if (!Physics.SphereCast(origin, castRadius, rightDir, out _, castDist, enemyLayer))
                return rightDir;

            Vector3 leftDir = Quaternion.AngleAxis(-angle, Vector3.up) * desiredDir;
            if (!Physics.SphereCast(origin, castRadius, leftDir, out _, castDist, enemyLayer))
                return leftDir;
        }

        return Vector3.zero;
    }


    private Vector3 ComputeSeparation()
    {
        Vector3 push = Vector3.zero;
        Collider[] neighbours = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);

        foreach (var col in neighbours)
        {
            if (col.transform == transform) continue;

            Vector3 away = transform.position - col.transform.position;
            away.y = 0;
            float dist = away.magnitude;
            if (dist < 0.01f) continue;

            float t = 1f - (dist / separationRadius);
            push += away.normalized * (t * separationStrength);
        }

        float maxSep = moveSpeed * 0.5f;
        if (push.magnitude > maxSep)
            push = push.normalized * maxSep;

        return push;
    }

    public void LookAtTarget(Vector3 targetPos, float speedMultiplier = 1f)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * speedMultiplier * Time.fixedDeltaTime));
    }

    public bool IsBlockedByAlly(Vector3 moveDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        return Physics.SphereCast(origin, 0.4f, moveDir, out _, blockCheckDistance, enemyLayer);
    }

    public Vector3 GetFreeDirection()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3[] directions = { transform.right, -transform.right, -transform.forward };
        foreach (var dir in directions)
        {
            if (!Physics.Raycast(origin, dir, 1.2f, enemyLayer))
                return dir;
        }
        return Vector3.zero;
    }

    public Vector3 GetSteeredDirection(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        return SweepForFreeDirection(desiredDir, origin, 0.3f, blockCheckDistance);
    }
}