using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
// Tracks the current and maximum health of an enemy and exposes events for damage, healing, and death.
public class EnemyHealth : MonoBehaviour
{
    [System.Serializable]
    public class HealthValueEvent : UnityEvent<float> { }

    [Header("Enemy Health")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float currentHealth;
    [SerializeField] private bool canTakeDamage = true;
    [SerializeField, Min(0)] private int moneyRewardOnDeath = 15;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0f;
    [SerializeField] private bool disableCollidersOnDeath = true;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDied;
    public HealthValueEvent onHealthChanged;

    public bool IsDead => currentHealth <= 0f;
    public int MoneyRewardOnDeath => moneyRewardOnDeath;

    bool hasGrantedDeathReward;

    private void Awake()
    {
        // Ensure starting health is valid and notify listeners of the initial health ratio.
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public bool TakeDamage(float amount)
    {
        if (!canTakeDamage || IsDead) return false;
        if (amount <= 0f) return false;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onDamaged?.Invoke();
        onHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0f)
        {
            onDied?.Invoke();
            RunStats.RecordEnemyKilled();
            Die();
            return true;
        }

        return false;
    }

    public void Heal(float amount)
    {
        // Restore health without exceeding max, and notify listeners.
        if (IsDead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        onHealed?.Invoke();
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void SetCanTakeDamage(bool canTake)
    {
        canTakeDamage = canTake;
    }

    public void ResetHealthToMax()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        TryGrantDeathReward();
    }

    private void Die()
    {
        TryGrantDeathReward();

        // Disable colliders and optionally destroy the enemy object.
        if (disableCollidersOnDeath)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        canTakeDamage = false;

        if (destroyOnDeath)
        {
            if (destroyDelay > 0f)
            {
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void TryGrantDeathReward()
    {
        if (hasGrantedDeathReward) return;
        if (moneyRewardOnDeath <= 0) return;

        hasGrantedDeathReward = true;
        Currency.TryAddMoney(moneyRewardOnDeath);
    }
}
