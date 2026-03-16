using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [System.Serializable]
    public class HealthValueEvent : UnityEvent<float> { }

    [Header("Player Health")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float currentHealth;
    [SerializeField] private bool canTakeDamage = true;
    [SerializeField] private bool respawnOnDeath = false;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool fullHealOnRespawn = true;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDied;
    public HealthValueEvent onHealthChanged;

    private CharacterController characterController;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        characterController = GetComponent<CharacterController>();
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
            Die();
            return true;
        }

        return false;
    }

    public void Heal(float amount)
    {
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

    private void Die()
    {
        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
            return;
        }

        SetCanTakeDamage(false);
        Debug.Log("Player died.");
    }

    private IEnumerator RespawnRoutine()
    {
        SetCanTakeDamage(false);

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        Vector3 respawnPosition = respawnPoint != null ? respawnPoint.position : transform.position;

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = respawnPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (fullHealOnRespawn)
        {
            ResetHealthToMax();
        }

        SetCanTakeDamage(true);
    }
}
