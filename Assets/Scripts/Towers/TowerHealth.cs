using UnityEngine;

// Lightweight health component for placeable towers so enemies can target and destroy them.
[DisallowMultipleComponent]
public class TowerHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 200f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool destroyOnDeath = true;

    public bool IsDestroyed => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
    }

    public bool TakeDamage(float amount)
    {
        if (IsDestroyed || amount <= 0f)
        {
            return false;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth <= 0f)
        {
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }

            return true;
        }

        return false;
    }
}
