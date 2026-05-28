using TMPro;
using UnityEngine;

// Tower that launches ice projectiles to damage and slow enemies.
public class IceTower : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float range = 8f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform turretHead;

    [Header("Attack")]
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float damage = 12f;
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private GameObject iciclePrefab;
    [SerializeField] private float icicleSpeed = 20f;
    [SerializeField] private Vector3 impactOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private DamagePopup damagePopupPrefab;
    [SerializeField] private Vector3 damagePopupOffset = new Vector3(0f, 1.2f, 0f);

    private float nextShotTime;
    private Vector3 initialTurretLocalEuler;
    private bool hasInitialTurretLocalEuler;

    private void Start()
    {
        // Cache initial turret rotation for smooth aiming.
        if (turretHead != null)
        {
            initialTurretLocalEuler = turretHead.localEulerAngles;
            hasInitialTurretLocalEuler = true;
        }
    }

    private void Update()
    {
        // Acquire a target and fire when ready.
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
        // Create an icicle projectile for the selected enemy. If no prefab exists,
        // apply immediate hit effects as a fallback so the tower still deals damage.
        if (target == null || target.IsDead)
        {
            return;
        }

        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : (turretHead != null ? turretHead.position : transform.position + Vector3.up);

        if (iciclePrefab == null)
        {
            ApplyHitEffects(target);
            return;
        }

        GameObject icicleInstance = Instantiate(iciclePrefab, spawnPosition, Quaternion.identity);
        IceProjectile projectile = icicleInstance.GetComponent<IceProjectile>();
        if (projectile == null)
        {
            projectile = icicleInstance.AddComponent<IceProjectile>();
        }

        projectile.Initialize(target, damage, slowMultiplier, slowDuration, icicleSpeed, impactOffset, damagePopupPrefab, damagePopupOffset);
    }

    private void ApplyHitEffects(EnemyHealth target)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        target.TakeDamage(damage);

        SpawnDamagePopup(target.transform.position, damage);

        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.ApplySlow(slowMultiplier, slowDuration);
        }
    }

    private void SpawnDamagePopup(Vector3 worldPosition, float amount)
    {
        DamagePopup popup = null;
        Vector3 popupPosition = worldPosition + damagePopupOffset;

        if (damagePopupPrefab != null)
        {
            popup = Instantiate(damagePopupPrefab, popupPosition, Quaternion.identity);
        }
        else
        {
            GameObject popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = popupPosition;

            TextMeshPro tmp = popupObject.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4f;
            tmp.color = new Color(1f, 0.25f, 0.25f, 1f);

            popup = popupObject.AddComponent<DamagePopup>();
            popup.text = tmp;
        }

        if (popup != null)
            popup.Initialize(Mathf.RoundToInt(amount).ToString());
    }

    private void RotateTowards(Vector3 worldPosition)
    {
        // Smoothly rotate the turret toward the target position.
        Transform targetTransform = turretHead != null ? turretHead : transform;
        Vector3 lookDirection = worldPosition - targetTransform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        // If we're rotating a turret head mounted on a parent, convert the world look
        // direction into the turret's local yaw and slerp the local rotation for
        // smoother constrained tracking behavior.
        if (targetTransform == turretHead && hasInitialTurretLocalEuler)
        {
            Transform parent = turretHead.parent;
            Vector3 localDirection = parent != null
                ? parent.InverseTransformDirection(lookDirection.normalized)
                : lookDirection.normalized;

            float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            Vector3 desiredEuler = initialTurretLocalEuler;
            desiredEuler.y = initialTurretLocalEuler.y + targetYaw;

            Quaternion desiredLocalRotation = Quaternion.Euler(desiredEuler);
            turretHead.localRotation = Quaternion.Slerp(
                turretHead.localRotation,
                desiredLocalRotation,
                10f * Time.deltaTime);
            return;
        }

        targetTransform.rotation = Quaternion.Slerp(
            targetTransform.rotation,
            Quaternion.LookRotation(lookDirection.normalized, Vector3.up),
            10f * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
