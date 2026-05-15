using UnityEngine;
using System.Collections.Generic;

public class EnemyGroupManager : MonoBehaviour
{
    public Transform player;
    public float radius = 3f;

    public float attackRadius = 2f;
    public int maxAttackers = 2;

    private List<EnemyStateMachine> attackers = new List<EnemyStateMachine>();
    private List<EnemyStateMachine> enemies = new List<EnemyStateMachine>();

    // ── Flank Slot System ────────────────────────────────────────────────────
    // Slots spread enemies evenly around the player.
    // Priority order: front, back, sides, then diagonals.
    private static readonly float[] SLOT_ANGLES = {
        0f, 180f, 90f, 270f, 45f, 135f, 225f, 315f
    };

    private Dictionary<EnemyStateMachine, float> flankAssignments = new();
    private Dictionary<float, int> flankSlotCounts = new();
    // ────────────────────────────────────────────────────────────────────────

    public void Register(EnemyStateMachine e)
    {
        if (!enemies.Contains(e))
            enemies.Add(e);
    }

    // ── Attack Slots (unchanged) ─────────────────────────────────────────────
    public Vector3 GetAttackSlot(EnemyStateMachine enemy)
    {
        int index = attackers.IndexOf(enemy);
        int count = attackers.Count;

        float angle = (360f / Mathf.Max(count, 1)) * index;
        Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

        return player.position + dir * attackRadius;
    }

    public bool CanAttack(EnemyStateMachine enemy)
    {
        if (attackers.Contains(enemy)) return true;
        return attackers.Count < maxAttackers;
    }

    public void ReserveAttackSlot(EnemyStateMachine enemy)
    {
        if (!attackers.Contains(enemy))
            attackers.Add(enemy);
    }

    public void ReleaseAttackSlot(EnemyStateMachine enemy)
    {
        attackers.Remove(enemy);
    }

    // ── Flank Slots ──────────────────────────────────────────────────────────

    /// <summary>
    /// Call in ChaseState.Enter(). Returns a world-angle (degrees) for this
    /// enemy's position around the player. Prefers empty slots; falls back to
    /// least-occupied when all 8 are taken (7+ enemies).
    /// </summary>
    public float RequestFlankAngle(EnemyStateMachine enemy)
    {
        // Keep the same slot if re-entering ChaseState
        if (flankAssignments.TryGetValue(enemy, out float existing))
            return existing;

        float bestAngle = SLOT_ANGLES[0];
        int bestCount = int.MaxValue;

        foreach (float angle in SLOT_ANGLES)
        {
            flankSlotCounts.TryGetValue(angle, out int count);
            if (count < bestCount)
            {
                bestCount = count;
                bestAngle = angle;
                if (count == 0) break; // empty slot — grab it immediately
            }
        }

        flankAssignments[enemy] = bestAngle;
        flankSlotCounts[bestAngle] = (flankSlotCounts.TryGetValue(bestAngle, out int cur) ? cur : 0) + 1;

        return bestAngle;
    }

    /// <summary>
    /// Call in ChaseState.Exit() to free the slot for another enemy.
    /// </summary>
    public void ReleaseFlankAngle(EnemyStateMachine enemy)
    {
        if (!flankAssignments.TryGetValue(enemy, out float angle)) return;

        flankAssignments.Remove(enemy);

        if (flankSlotCounts.TryGetValue(angle, out int count))
        {
            if (count <= 1) flankSlotCounts.Remove(angle);
            else flankSlotCounts[angle] = count - 1;
        }
    }
}
