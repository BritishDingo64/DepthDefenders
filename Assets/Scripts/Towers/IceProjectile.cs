using TMPro;
using UnityEngine;

// Projectile used by ice towers; it seeks a target, deals damage, and applies a slow effect.
public class IceProjectile : MonoBehaviour
{
    private EnemyHealth target;
    private float damage;
    private float slowMultiplier;
    private float slowDuration;
    private float speed;
    private Vector3 impactOffset;
    private bool initialized;
    private DamagePopup damagePopupPrefab;
    private Vector3 damagePopupOffset;

    public void Initialize(EnemyHealth projectileTarget, float projectileDamage, float projectileSlowMultiplier, float projectileSlowDuration, float projectileSpeed, Vector3 projectileImpactOffset, DamagePopup popupPrefab = null, Vector3 popupOffset = default)
    {
        // Store the projectile parameters and start flying toward the target.
        target = projectileTarget;
        damage = projectileDamage;
        slowMultiplier = projectileSlowMultiplier;
        slowDuration = projectileSlowDuration;
        speed = Mathf.Max(0.01f, projectileSpeed);
        impactOffset = projectileImpactOffset;
        initialized = true;
        damagePopupPrefab = popupPrefab;
        damagePopupOffset = popupOffset == default ? new Vector3(0f, 1f, 0f) : popupOffset;
    }

    private void Update()
    {
        // Move the projectile toward the target each frame.
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
        // Apply damage and slow to the target when the projectile reaches it.
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(damage);

            SpawnDamagePopup(target.transform.position, damage);

            Monster monster = target.GetComponent<Monster>();
            if (monster != null)
            {
                monster.ApplySlow(slowMultiplier, slowDuration);
            }
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
