using System.Collections.Generic;
using UnityEngine;

public class TeslaChainTower : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float range = 9f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private Transform turretHead;

    [Header("Chain Attack")]
    [SerializeField] private float fireRate = 0.75f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float chainRange = 4f;
    [SerializeField] private int maxChains = 3;

    private float nextAttackTime;

    private void Update()
    {
        EnemyHealth firstTarget = TowerTargetingUtility.FindClosestEnemy(transform.position, range, targetMask);
        if (firstTarget == null)
        {
            return;
        }

        RotateTowards(firstTarget.transform.position);

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        FireChain(firstTarget);
    }

    private void FireChain(EnemyHealth firstTarget)
    {
        List<EnemyHealth> chainedTargets = new List<EnemyHealth>();
        EnemyHealth currentTarget = firstTarget;

        for (int i = 0; i < Mathf.Max(1, maxChains) && currentTarget != null; i++)
        {
            if (!chainedTargets.Contains(currentTarget) && !currentTarget.IsDead)
            {
                currentTarget.TakeDamage(damage);
                chainedTargets.Add(currentTarget);
            }

            currentTarget = FindNextChainTarget(currentTarget, chainedTargets);
        }
    }

    private EnemyHealth FindNextChainTarget(EnemyHealth fromTarget, List<EnemyHealth> excludedTargets)
    {
        List<EnemyHealth> nearbyTargets = TowerTargetingUtility.FindEnemiesInRange(fromTarget.transform.position, chainRange, targetMask);
        EnemyHealth closestTarget = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < nearbyTargets.Count; i++)
        {
            EnemyHealth candidate = nearbyTargets[i];
            if (candidate == null || candidate.IsDead || excludedTargets.Contains(candidate))
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - fromTarget.transform.position).sqrMagnitude;
            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                closestTarget = candidate;
            }
        }

        return closestTarget;
    }

    private void RotateTowards(Vector3 worldPosition)
    {
        Transform targetTransform = turretHead != null ? turretHead : transform;
        Vector3 lookDirection = worldPosition - targetTransform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        targetTransform.rotation = Quaternion.Slerp(
            targetTransform.rotation,
            Quaternion.LookRotation(lookDirection),
            12f * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
