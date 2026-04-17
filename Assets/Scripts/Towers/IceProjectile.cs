using UnityEngine;

public class IceProjectile : MonoBehaviour
{
    private EnemyHealth target;
    private float damage;
    private float slowMultiplier;
    private float slowDuration;
    private float speed;
    private Vector3 impactOffset;
    private bool initialized;

    public void Initialize(EnemyHealth projectileTarget, float projectileDamage, float projectileSlowMultiplier, float projectileSlowDuration, float projectileSpeed, Vector3 projectileImpactOffset)
    {
        target = projectileTarget;
        damage = projectileDamage;
        slowMultiplier = projectileSlowMultiplier;
        slowDuration = projectileSlowDuration;
        speed = Mathf.Max(0.01f, projectileSpeed);
        impactOffset = projectileImpactOffset;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 destination = target.transform.position + impactOffset;
        Vector3 toTarget = destination - transform.position;
        float step = speed * Time.deltaTime;

        if (toTarget.sqrMagnitude <= step * step)
        {
            transform.position = destination;
            ApplyHit();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        transform.position += moveDirection * step;
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
    }

    private void ApplyHit()
    {
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(damage);

            Monster monster = target.GetComponent<Monster>();
            if (monster != null)
            {
                monster.ApplySlow(slowMultiplier, slowDuration);
            }
        }

        Destroy(gameObject);
    }
}
