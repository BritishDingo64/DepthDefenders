using System.Collections.Generic;
using UnityEngine;

// Utility class for finding enemy targets around towers.
public static class TowerTargetingUtility
{
    public static EnemyHealth FindClosestEnemy(Vector3 origin, float range, LayerMask targetMask)
    {
        // Return the nearest living enemy in range.
        Collider[] hits = Physics.OverlapSphere(origin, range, targetMask, QueryTriggerInteraction.Ignore);
        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth enemyHealth = hits[i].GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null || enemyHealth.IsDead)
            {
                continue;
            }

            float sqrDistance = (enemyHealth.transform.position - origin).sqrMagnitude;
            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                closestEnemy = enemyHealth;
            }
        }

        return closestEnemy;
    }

    public static List<EnemyHealth> FindEnemiesInRange(Vector3 origin, float range, LayerMask targetMask)
    {
        // Collect all unique living enemies within the given radius.
        Collider[] hits = Physics.OverlapSphere(origin, range, targetMask, QueryTriggerInteraction.Ignore);
        List<EnemyHealth> enemies = new List<EnemyHealth>();
        HashSet<EnemyHealth> seen = new HashSet<EnemyHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth enemyHealth = hits[i].GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null || enemyHealth.IsDead || seen.Contains(enemyHealth))
            {
                continue;
            }

            seen.Add(enemyHealth);
            enemies.Add(enemyHealth);
        }

        return enemies;
    }
}
