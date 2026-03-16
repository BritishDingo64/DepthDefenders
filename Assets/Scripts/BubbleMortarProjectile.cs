using System.Collections.Generic;
using UnityEngine;

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

    public void Initialize(Vector3 destination, float projectileDamage, float projectileSplashRadius, float projectileTravelTime, float projectileArcHeight, LayerMask hitMask)
    {
        startPosition = transform.position;
        targetPosition = destination;
        damage = projectileDamage;
        splashRadius = projectileSplashRadius;
        travelTime = Mathf.Max(0.05f, projectileTravelTime);
        arcHeight = projectileArcHeight;
        targetMask = hitMask;
        launchTime = Time.time;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
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

    private void Explode()
    {
        List<EnemyHealth> enemies = TowerTargetingUtility.FindEnemiesInRange(targetPosition, splashRadius, targetMask);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
