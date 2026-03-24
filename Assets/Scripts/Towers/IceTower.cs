using UnityEngine;

public class IceTower : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float range = 8f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private Transform turretHead;

    [Header("Attack")]
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float damage = 12f;
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2f;

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

        target.TakeDamage(damage);

        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.ApplySlow(slowMultiplier, slowDuration);
        }
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
            10f * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
