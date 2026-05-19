using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Tower that zaps a primary enemy and chains lightning to nearby enemies.
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
    [SerializeField] private GameObject zapEffectPrefab;
    [SerializeField] private DamagePopup damagePopupPrefab;
    [SerializeField] private Vector3 damagePopupOffset = new Vector3(0f, 1.5f, 0f);

    private float nextAttackTime;

    private void Update()
    {
        // Find a target and fire a chain attack when ready.
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
        // Apply damage to the first target and chain to nearby enemies.
        // The algorithm walks from the first target and repeatedly finds the next
        // closest eligible enemy within `chainRange` until `maxChains` is reached.
        List<EnemyHealth> chainedTargets = new List<EnemyHealth>();
        EnemyHealth currentTarget = firstTarget;
        
        // Determine the starting position for the zap (turret head if available, otherwise tower center)
        Vector3 zapStartPos = turretHead != null ? turretHead.position : transform.position;

        for (int i = 0; i < Mathf.Max(1, maxChains) && currentTarget != null; i++)
        {
            if (!chainedTargets.Contains(currentTarget) && !currentTarget.IsDead)
            {
                currentTarget.TakeDamage(damage);
                SpawnDamagePopup(currentTarget.transform.position, damage);
                chainedTargets.Add(currentTarget);
                
                // Spawn zap effect
                Vector3 fromPos = i == 0 ? zapStartPos : chainedTargets[i - 1].transform.position;
                SpawnZapEffect(fromPos, currentTarget.transform.position);
            }

            currentTarget = FindNextChainTarget(currentTarget, chainedTargets);
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

    private EnemyHealth FindNextChainTarget(EnemyHealth fromTarget, List<EnemyHealth> excludedTargets)
    {
        // Find the next closest enemy eligible for chaining. This excludes already
        // hit targets (to avoid immediate loops) and ignores dead enemies.
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

    private void SpawnZapEffect(Vector3 fromPosition, Vector3 toPosition)
    {
        if (zapEffectPrefab == null)
        {
            return;
        }

        GameObject zapInstance = Instantiate(zapEffectPrefab);
        
        TeslaZapEffect zapEffect = zapInstance.GetComponent<TeslaZapEffect>();
        if (zapEffect != null)
        {
            zapEffect.SetPositions(fromPosition, toPosition);
        }
        
        // Destroy the effect after a short duration
        Destroy(zapInstance, 0.5f);
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
