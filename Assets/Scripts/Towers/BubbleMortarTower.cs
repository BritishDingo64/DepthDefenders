using UnityEngine;

public class BubbleMortarTower : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float range = 12f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform turretHead;

    [Header("Attack")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float damage = 18f;
    [SerializeField] private float splashRadius = 3f;
    [SerializeField] private float projectileTravelTime = 0.8f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private GameObject projectileVisualPrefab;

    private float nextShotTime;

    private void Update()
    {
        EnemyHealth target = TowerTargetingUtility.FindClosestEnemy(transform.position, range, targetMask);
        if (target == null)
        {
            return;
        }

        RotateTowards(target.transform.position);

        if (Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        FireAt(target);
    }

    private void FireAt(EnemyHealth target)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        GameObject projectileObject = projectileVisualPrefab != null
            ? Instantiate(projectileVisualPrefab, spawnPosition, Quaternion.identity)
            : new GameObject("BubbleMortarProjectile");

        projectileObject.transform.position = spawnPosition;
        BubbleMortarProjectile projectile = projectileObject.GetComponent<BubbleMortarProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<BubbleMortarProjectile>();
        }

        projectile.Initialize(target.transform.position, damage, splashRadius, projectileTravelTime, arcHeight, targetMask);
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
            8f * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
