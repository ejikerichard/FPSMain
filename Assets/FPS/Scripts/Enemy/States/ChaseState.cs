using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyStateMachine enemy;

    private float assignedAngle;           // World-space angle around player (fixed on Enter)
    private Vector3 worldSlotPos;          // Locked world position — does NOT move with player rotation
    private bool slotReached = false;

    private const float FLANK_ORBIT_RADIUS = 3.5f;
    private const float SLOT_ARRIVAL_DIST = 1.0f;

    public ChaseState(EnemyStateMachine enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.anim.SetLocomotion(true);
        enemy.anim.useRootMotion = false;

        slotReached = false;

        // Get assigned angle from group (0°=front, 180°=back, 90°=right, etc.)
        assignedAngle = enemy.group.RequestFlankAngle(enemy);

        // Bake the slot into WORLD SPACE right now.
        // We use player.forward at this moment so the formation makes sense,
        // but we do NOT update it every frame — that's what caused the sliding.
        Quaternion slotRot = Quaternion.AngleAxis(assignedAngle, Vector3.up);
        Vector3 slotOffset = slotRot * enemy.player.forward * FLANK_ORBIT_RADIUS;
        worldSlotPos = enemy.player.position + slotOffset;
    }

    public void Tick()
    {
        float dist = enemy.DistanceToPlayer();

        // Always face player
        enemy.motor.LookAtTarget(enemy.player.position);

        // Attack check
        if (dist <= enemy.attackRange && enemy.group.CanAttack(enemy) && enemy.CanAttackNow())
        {
            enemy.SwitchState(new PreAttackState(enemy));
            return;
        }

        if (dist > enemy.attackRange)
        {
            enemy.motor.MoveDirection(ComputeDesiredDirection(dist));
        }
        else
        {
            enemy.motor.MoveDirection(Vector3.zero);
        }

        // If player moves far from the slot (e.g. player ran away), refresh the slot position
        // so the enemy doesn't chase a stale position across the map.
        if (Vector3.Distance(enemy.player.position, worldSlotPos) > FLANK_ORBIT_RADIUS * 2.5f)
            RefreshSlotPosition();
    }

    private Vector3 ComputeDesiredDirection(float distToPlayer)
    {
        float distToSlot = Vector3.Distance(enemy.transform.position, worldSlotPos);

        // Phase 1: head to the orbit slot position
        if (!slotReached && distToSlot > SLOT_ARRIVAL_DIST)
            return (worldSlotPos - enemy.transform.position).normalized;

        // Phase 2: slot arrived, close in on player directly
        slotReached = true;
        return (enemy.player.position - enemy.transform.position).normalized;
    }

    private void RefreshSlotPosition()
    {
        Quaternion slotRot = Quaternion.AngleAxis(assignedAngle, Vector3.up);
        Vector3 slotOffset = slotRot * enemy.player.forward * FLANK_ORBIT_RADIUS;
        worldSlotPos = enemy.player.position + slotOffset;
        slotReached = false;
    }

    public void Exit()
    {
        enemy.group.ReleaseFlankAngle(enemy);
    }
}
