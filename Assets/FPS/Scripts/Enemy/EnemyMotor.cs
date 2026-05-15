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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
        requestedDir = Vector3.zero;
        lastResolvedDir = Vector3.zero;
        committedSteerDir = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        GetComponent<EnemyAnimatorSync>()?.UpdateMovement(Vector3.zero, player);
    }

    public void Dash(Vector3 direction, float force, float duration)
    {
        direction.y = 0;
        hasTarget = false;
        dashTimer = duration;
        rb.linearVelocity = direction.normalized * force;
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

    /// <summary>
    /// Called from ChaseState.Tick() (Update). Just stores the request.
    /// Actual movement is applied in FixedUpdate so timing is consistent.
    /// </summary>
    public void MoveDirection(Vector3 desiredDir)
    {
        desiredDir.y = 0;
        requestedDir = desiredDir.sqrMagnitude > 0.01f ? desiredDir.normalized : Vector3.zero;
        hasMoveRequest = true;
    }

    void FixedUpdate()
    {
        var anim = GetComponent<EnemyAnimatorSync>();

        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            anim?.UpdateMovement(transform.InverseTransformDirection(rb.linearVelocity), player);
            return;
        }

        // Decay steer hold timer
        if (steerHoldTimer > 0f)
            steerHoldTimer -= Time.fixedDeltaTime;

        Vector3 targetDir = Vector3.zero;

        if (hasMoveRequest && requestedDir.sqrMagnitude > 0.01f)
        {
            targetDir = ResolveDirection(requestedDir);
        }
        else
        {
            // No movement requested — stop immediately
            committedSteerDir = Vector3.zero;
        }

        lastResolvedDir = targetDir;

        hasMoveRequest = false;

        // Apply velocity directly — no smoothing, no slide.
        // Stopping is immediate: velocity is set to 0 when no input.
        LastMoveInput = lastResolvedDir;

        // Base movement velocity
        Vector3 velocity = lastResolvedDir * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        // Separation: only push when moving so stationary enemies don't drift
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

    /// <summary>
    /// Resolves the final movement direction:
    /// - If path is clear, use desired direction directly.
    /// - If blocked, sweep for a free angle and COMMIT to it for MIN_STEER_HOLD seconds.
    ///   The committed direction won't change until the hold expires, eliminating jitter.
    /// </summary>
    private Vector3 ResolveDirection(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float castRadius = 0.3f;
        float castDist = blockCheckDistance;

        bool desiredBlocked = Physics.SphereCast(origin, castRadius, desiredDir, out _, castDist, enemyLayer);

        if (!desiredBlocked)
        {
            // Path clear — release commitment and move freely
            committedSteerDir = Vector3.zero;
            steerHoldTimer = 0f;
            return desiredDir;
        }

        // Path blocked — check if we're still holding a committed direction
        if (steerHoldTimer > 0f && committedSteerDir.sqrMagnitude > 0.01f)
        {
            // Check committed dir is still actually free; if not, force a re-evaluation
            bool committedBlocked = Physics.SphereCast(origin, castRadius, committedSteerDir, out _, castDist, enemyLayer);
            if (!committedBlocked)
                return committedSteerDir; // Keep going the committed way
            // Committed dir is now blocked too — fall through to re-sweep
        }

        // Sweep for a new free direction
        Vector3 freeDir = SweepForFreeDirection(desiredDir, origin, castRadius, castDist);

        // Commit to it
        committedSteerDir = freeDir;
        steerHoldTimer = MIN_STEER_HOLD;

        return freeDir;
    }

    /// <summary>
    /// Sweeps 20° increments outward from desiredDir, alternating left/right.
    /// Returns Vector3.zero if completely surrounded.
    /// </summary>
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

        return Vector3.zero; // Fully surrounded — stop
    }

    /// <summary>
    /// Returns a small velocity nudge that pushes this enemy away from nearby allies.
    /// Strength falls off with distance and is capped so it never causes jitter.
    /// </summary>
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

            // Linear falloff: full push at 0, zero push at separationRadius
            float t = 1f - (dist / separationRadius);
            push += away.normalized * (t * separationStrength);
        }

        // Cap so separation never exceeds half moveSpeed — prevents overpowering movement
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
