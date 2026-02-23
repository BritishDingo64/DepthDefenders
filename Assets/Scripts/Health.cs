using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDied;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage. Returns true if this call caused death.
    /// </summary>
    public bool TakeDamage(float amount)
    {
        if (amount <= 0) return false;
        currentHealth -= amount;
        onDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            onDied?.Invoke();
            Die();
            return true;
        }

        return false;
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        onHealed?.Invoke();
    }

    protected virtual void Die()
    {
        // Default behaviour: destroy the GameObject.
        // Override or subscribe to onDied to play animation/sfx instead.
        Destroy(gameObject);
    }
}
