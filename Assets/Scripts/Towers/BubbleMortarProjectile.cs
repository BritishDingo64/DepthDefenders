using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Handles a mortar projectile that flies through the air and explodes in a splash radius.
public class BubbleMortarProjectile : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float damage;
    private float splashRadius;
    private float travelTime;
    private float arcHeight;
    private LayerMask targetMask;
    private float launchTime;
    private bool initialized;
    private EnemyHealth currentTarget;
    private DamagePopup damagePopupPrefab;
    private Vector3 damagePopupOffset;

    public void Initialize(Vector3 spawnPoint, EnemyHealth target, Vector3 destination, float projectileDamage, float projectileSplashRadius, float projectileTravelTime, float projectileArcHeight, LayerMask hitMask, DamagePopup popupPrefab, Vector3 popupOffset)
    {
        // Store settings and start the projectile flight.
        startPosition = spawnPoint;
        currentTarget = target;
        targetPosition = destination;
        damage = projectileDamage;
        splashRadius = projectileSplashRadius;
        travelTime = Mathf.Max(0.05f, projectileTravelTime);
        arcHeight = projectileArcHeight;
        targetMask = hitMask;
        damagePopupPrefab = popupPrefab;
        damagePopupOffset = popupOffset;
        launchTime = Time.time;
        initialized = true;
    }

    private void Update()
    {
        // Animate the projectile along an arced trajectory.
        if (!initialized)
        {
            return;
        }

        if (currentTarget == null || currentTarget.IsDead)
        {
            Retarget();
        }
        else
        {
            targetPosition = currentTarget.transform.position;
        }

        float elapsed = Time.time - launchTime;
        float progress = Mathf.Clamp01(elapsed / travelTime);
        Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        float heightOffset = 4f * arcHeight * progress * (1f - progress);
        transform.position = flatPosition + Vector3.up * heightOffset;

        if (progress >= 1f)
        {
            Explode();
        }
    }

    private void Retarget()
    {
        float retargetRadius = Mathf.Max(splashRadius * 2f, 4f);
        EnemyHealth newTarget = TowerTargetingUtility.FindClosestEnemy(transform.position, retargetRadius, targetMask);
        if (newTarget == null)
            return;

        currentTarget = newTarget;
        startPosition = transform.position;
        targetPosition = newTarget.transform.position;
        launchTime = Time.time;
    }

    private void Explode()
    {
        // Damage all enemies within the explosion radius.
        List<EnemyHealth> enemies = TowerTargetingUtility.FindEnemiesInRange(targetPosition, splashRadius, targetMask);
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            enemy.TakeDamage(damage);
            SpawnDamagePopup(enemy.transform.position, damage);
        }

        Destroy(gameObject);
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
}
