using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Manages the player's health, death behavior, ragdoll, and optional respawn sequence.
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
    [SerializeField] private TMP_Text respawnCountdownText;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDied;
    public HealthValueEvent onHealthChanged;

    private CharacterController characterController;
    private Rigidbody rigidbodyComponent;
    private Animator animator;
    private Collider rootCollider;
    private Rigidbody[] ragdollBodies = System.Array.Empty<Rigidbody>();
    private Collider[] ragdollColliders = System.Array.Empty<Collider>();
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool ragdollActive;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        // Initialize health and cache components used for ragdoll and respawn.
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        characterController = GetComponent<CharacterController>();
        rigidbodyComponent = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>(true);
        rootCollider = GetComponent<Collider>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);
        EnsureRespawnCountdownText();
        ragdollActive = true;
        SetRagdollActive(false);
    }

    public bool TakeDamage(float amount)
    {
        // Apply damage to the player and trigger death if health reaches zero.
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
        // Restore player health while alive.
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
        // Activate ragdoll and optionally start a respawn routine.
        SetRagdollActive(true);

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
            return;
        }

        SetCanTakeDamage(false);
    }

    private IEnumerator RespawnRoutine()
    {
        // Countdown and restore the player at the respawn point.
        SetCanTakeDamage(false);

        if (respawnCountdownText != null)
        {
            respawnCountdownText.gameObject.SetActive(respawnDelay > 0f);
        }

        float timeRemaining = respawnDelay;

        while (timeRemaining > 0f)
        {
            UpdateRespawnCountdownText(timeRemaining);
            yield return null;
            timeRemaining -= Time.deltaTime;
        }

        if (respawnCountdownText != null)
        {
            respawnCountdownText.text = string.Empty;
            respawnCountdownText.gameObject.SetActive(false);
        }

        Vector3 respawnPosition = respawnPoint != null ? respawnPoint.position : spawnPosition;
        Quaternion respawnRotation = respawnPoint != null ? respawnPoint.rotation : spawnRotation;

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
            rigidbodyComponent.position = respawnPosition;
            rigidbodyComponent.rotation = respawnRotation;
        }

        transform.SetPositionAndRotation(respawnPosition, respawnRotation);
        SetRagdollActive(false);

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (fullHealOnRespawn)
        {
            ResetHealthToMax();
        }

        SetCanTakeDamage(true);
    }

    private void EnsureRespawnCountdownText()
    {
        // Ensure a UI text element exists to show the respawn timer. If none is
        // assigned we try to synthesize a TextMeshProUGUI under an existing canvas
        // so the player receives visual feedback during the respawn countdown.
        if (respawnCountdownText != null)
        {
            respawnCountdownText.gameObject.SetActive(false);
            return;
        }

        HealthBarUI healthBarUi = FindFirstObjectByType<HealthBarUI>();
        Canvas targetCanvas = healthBarUi != null ? healthBarUi.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (targetCanvas == null)
            return;

        GameObject textObject = new GameObject("Respawn Countdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(targetCanvas.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 130f);
        rectTransform.sizeDelta = new Vector2(420f, 60f);

        respawnCountdownText = textObject.GetComponent<TextMeshProUGUI>();
        TMP_Text sourceText = healthBarUi != null ? healthBarUi.healthText : FindFirstObjectByType<TMP_Text>();
        if (sourceText != null)
        {
            respawnCountdownText.font = sourceText.font;
            respawnCountdownText.fontSharedMaterial = sourceText.fontSharedMaterial;
            respawnCountdownText.fontSize = sourceText.fontSize;
        }

        respawnCountdownText.alignment = TextAlignmentOptions.Center;
        respawnCountdownText.color = Color.white;
        respawnCountdownText.textWrappingMode = TextWrappingModes.NoWrap;
        respawnCountdownText.text = string.Empty;
        respawnCountdownText.gameObject.SetActive(false);
    }

    private void UpdateRespawnCountdownText(float timeRemaining)
    {
        if (respawnCountdownText == null)
            return;

        int secondsRemaining = Mathf.Max(1, Mathf.CeilToInt(timeRemaining));
        respawnCountdownText.text = $"Respawning in {secondsRemaining}";
    }

    private void SetRagdollActive(bool active)
    {
        // Toggle ragdoll mode by enabling/disabling animated bones and physics bodies.
        // When enabling ragdoll we restore physics on child rigidbodies; when disabling we kinematize them
        // and restore animator/character controller. Velocities are cleared to avoid physics glitches.
        if (ragdollActive == active)
            return;

        ragdollActive = active;

        if (animator != null)
        {
            animator.enabled = !active;
            if (!active)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        if (characterController != null)
        {
            characterController.enabled = !active;
        }

        if (rootCollider != null)
        {
            rootCollider.enabled = !active;
        }

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null || body == rigidbodyComponent)
                continue;

            if (active)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.detectCollisions = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            else
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
            }
        }

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
            rigidbodyComponent.isKinematic = active;
            rigidbodyComponent.useGravity = !active;
            rigidbodyComponent.detectCollisions = !active;
        }

        foreach (Collider collider in ragdollColliders)
        {
            if (collider == null || collider == rootCollider)
                continue;

            collider.enabled = active;
        }

    }
}
