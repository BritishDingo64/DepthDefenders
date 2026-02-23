using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FishAI : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Renderer fishRenderer;
    public Health healthComponent;

    [Header("Movement")]
    public float swimSpeed = 3f;
    public float turnSpeed = 5f;
    public float stoppingDistance = 1.2f;
    public float verticalAdjustSpeed = 2f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;
    public float attackWindup = 0.5f;

    [Header("Knockback Reaction")]
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.4f;
    public Color hurtColor = Color.red;
    public float flashDuration = 0.15f;

    private float lastAttackTime = -999f;
    private bool dead = false;
    private bool isKnockedBack = false;
    private Vector3 knockbackVelocity;
    private Color originalColor;

    private void Start()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) playerTransform = player.transform;
        }

        if (healthComponent == null)
            healthComponent = GetComponent<Health>();

        if (fishRenderer == null)
            fishRenderer = GetComponentInChildren<Renderer>();

        if (fishRenderer != null)
            originalColor = fishRenderer.material.color;

        if (healthComponent != null)
        {
            healthComponent.onDamaged.AddListener(OnDamaged);
            healthComponent.onDied.AddListener(OnDeath);
        }
    }

    private void Update()
    {
        if (dead || playerTransform == null) return;

        if (isKnockedBack)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 4f);
            return;
        }

        Vector3 toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;

        // Rotate toward player
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        }

        // Smoothly match player’s height
        float targetY = playerTransform.position.y;
        transform.position = new Vector3(
            transform.position.x,
            Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * verticalAdjustSpeed),
            transform.position.z
        );

        // Stop moving when close enough
        if (distance > stoppingDistance)
        {
            transform.position += transform.forward * swimSpeed * Time.deltaTime;
        }
        else
        {
            // Within striking distance — stop moving
            TryAttack(distance);
        }
    }

    private void TryAttack(float currentDistance)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (currentDistance <= attackRange)
        {
            lastAttackTime = Time.time;
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        yield return new WaitForSeconds(attackWindup);

        if (isKnockedBack || dead) yield break;
        if (Vector3.Distance(transform.position, playerTransform.position) > attackRange) yield break;

        var player = playerTransform.GetComponent<Health>();
        if (player != null)
        {
            player.TakeDamage(attackDamage);
        }
    }

    private void OnDamaged()
    {
        if (dead) return;
        StartCoroutine(FlashAndKnockback());
    }

    private IEnumerator FlashAndKnockback()
    {
        if (fishRenderer != null)
            fishRenderer.material.color = hurtColor;

        if (playerTransform != null)
        {
            Vector3 away = (transform.position - playerTransform.position).normalized;
            knockbackVelocity = away * knockbackForce;
            knockbackVelocity.y = 1f;
        }

        isKnockedBack = true;

        yield return new WaitForSeconds(flashDuration);

        if (fishRenderer != null)
            fishRenderer.material.color = originalColor;

        yield return new WaitForSeconds(knockbackDuration - flashDuration);

        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
    }

    private void OnDeath()
    {
        dead = true;
        isKnockedBack = false;

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (fishRenderer != null)
            fishRenderer.material.color = Color.gray;

        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
