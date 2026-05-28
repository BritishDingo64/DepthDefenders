using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
// Represents a destructible barricade that enemies can attack and which can draw aggro.
public class BarricadeDefenseTower : MonoBehaviour
{
    // Global registry of active barricades used by enemy AI to find nearby barricades.
    // Entries are added/removed in OnEnable/OnDisable so other systems can query them.
    public static readonly List<BarricadeDefenseTower> ActiveBarricades = new List<BarricadeDefenseTower>();

    [System.Serializable]
    public class BarricadeHealthChangedEvent : UnityEvent<float> { }

    [Header("Barricade Health")]
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool destroyOnDeath = true;

    [Header("Aggro")]
    [SerializeField] private float aggroRadius = 7f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDied;
    public BarricadeHealthChangedEvent onHealthChanged;

    public bool IsDestroyed => currentHealth <= 0f;
    public float AggroRadius => aggroRadius;

    private void Awake()
    {
        // Initialize barricade health and notify listeners.
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void OnEnable()
    {
        if (!ActiveBarricades.Contains(this))
        {
            ActiveBarricades.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveBarricades.Remove(this);
    }

    public bool TakeDamage(float amount)
    {
        // Reduce barricade health and destroy if depleted.
        if (IsDestroyed || amount <= 0f)
        {
            return false;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onDamaged?.Invoke();
        onHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0f)
        {
            onDied?.Invoke();
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }

            return true;
        }

        return false;
    }

    public static BarricadeDefenseTower GetClosestActiveBarricade(Vector3 fromPosition, float maxDetectionRange)
    {
        // Return the closest active barricade within range for enemy targeting.
        BarricadeDefenseTower closestBarricade = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < ActiveBarricades.Count; i++)
        {
            BarricadeDefenseTower barricade = ActiveBarricades[i];
            if (barricade == null || barricade.IsDestroyed)
            {
                continue;
            }

            float effectiveRange = Mathf.Min(maxDetectionRange, barricade.aggroRadius);
            float sqrDistance = (barricade.transform.position - fromPosition).sqrMagnitude;
            if (sqrDistance > effectiveRange * effectiveRange)
            {
                continue;
            }

            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                closestBarricade = barricade;
            }
        }

        return closestBarricade;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
    }
}
