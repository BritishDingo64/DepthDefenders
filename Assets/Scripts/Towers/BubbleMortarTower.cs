using UnityEngine;

// A tower that launches splash-damage mortar projectiles at nearby enemies.
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
    [SerializeField] private DamagePopup damagePopupPrefab;
    [SerializeField] private Vector3 damagePopupOffset = new Vector3(0f, 1.5f, 0f);

    private float nextShotTime;
    private Animator animator;
    private bool isPlaced;

    private void Start()
    {
        // Cache the turret animator if available.
        if (turretHead != null)
        {
            animator = turretHead.GetComponent<Animator>();
        }
    }

    private void Update()
    {
        // Only attack when the tower has been placed.
        if (!isPlaced)
        {
            return;
        }

        EnemyHealth target = TowerTargetingUtility.FindClosestEnemy(transform.position, range, targetMask);
        if (target == null)
        {
            ResetAnimation();
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        FireAt(target);
    }

    private void FireAt(EnemyHealth target)
    {
        // Spawn or reuse a projectile visual object and initialize its behavior.
        // The `BubbleMortarProjectile` handles arced travel, retargeting, and splash damage.
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

        projectile.Initialize(spawnPosition, target, target.transform.position, damage, splashRadius, projectileTravelTime, arcHeight, targetMask, damagePopupPrefab, damagePopupOffset);
    }

    private void ResetAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.ResetTrigger("Attack");
        }
    }

    public void PlaceTower()
    {
        // Mark the tower as placed and active. This switches the tower into its
        // runtime behaviour so `Update` will search for targets and fire.
        isPlaced = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
