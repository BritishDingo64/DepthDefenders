using UnityEngine;
using TMPro;

// Handles player melee attacks, target selection, damage application, and feedback effects.
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack settings")]
    public float damage = 25f;
    public float attackRange = 3f; // how far the player's attack reaches
    public float attackRadius = 1f; // width of melee detection in front of player
    public LayerMask hitMask = ~0; // which layers to hit (default: everything)
    public Camera playerCamera; // assign main camera or leave null to auto-find
    [Range(-1f, 1f)] public float minFacingDot = 0.15f; // higher value = stricter "in front"

    [Header("Feedback")]
    public float attackCooldown = 0.5f;
    public Animator animator;
    public string attackTriggerParameter = "attack";
    public DamagePopup damagePopupPrefab;
    public Vector3 damagePopupOffset = new Vector3(0f, 1.5f, 0f);
    private float lastAttackTime = -999f;
    private int attackTriggerHash;

    private void Start()
    {
        // Cache the camera and animator used for attack targeting and animations.
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main;
            else playerCamera = GetComponentInChildren<Camera>();
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        attackTriggerHash = Animator.StringToHash(attackTriggerParameter);
    }

    private void Update()
    {
        // Fire an attack when the left mouse button is pressed and cooldown allows.
        if (Input.GetMouseButtonDown(0) && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            DoAttack();
        }
    }

    private void DoAttack()
    {
        // Trigger attack animation and attempt to damage a valid enemy target.
        if (animator != null && !string.IsNullOrWhiteSpace(attackTriggerParameter))
            animator.SetTrigger(attackTriggerHash);

        EnemyHealth enemy = FindBestEnemyInFront();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            SpawnDamagePopup(enemy.transform.position, damage);
            return;
        }

        // Fallback ray check for crosshair-targeted hits.
        Ray ray;
        if (playerCamera != null)
            ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        else
            ray = new Ray(transform.position + Vector3.up, transform.forward);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, attackRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                SpawnDamagePopup(hit.point, damage);
            }
        }
    }

    private EnemyHealth FindBestEnemyInFront()
    {
        // Search for the best enemy within the attack radius in front of the player.
        Vector3 origin = transform.position + Vector3.up;
        Vector3 center = origin + transform.forward * Mathf.Max(0.1f, attackRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f, attackRadius), hitMask, QueryTriggerInteraction.Ignore);

        EnemyHealth bestEnemy = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead) continue;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > attackRange * attackRange) continue;

            float facingDot = Vector3.Dot(transform.forward.normalized, toEnemy.normalized);
            if (facingDot < minFacingDot) continue;

            // Prefer enemies closer to centerline and closer distance.
            float score = facingDot * 3f - toEnemy.magnitude;
            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private void SpawnDamagePopup(Vector3 worldPosition, float amount)
    {
        // Spawn a floating damage number at the hit position.
        DamagePopup popup;
        if (damagePopupPrefab != null)
        {
            popup = Instantiate(damagePopupPrefab, worldPosition + damagePopupOffset, Quaternion.identity);
        }
        else
        {
            GameObject popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = worldPosition + damagePopupOffset;

            TextMeshPro tmp = popupObject.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4f;
            tmp.color = new Color(1f, 0.25f, 0.25f, 1f);
            tmp.outlineWidth = 0.2f;

            popup = popupObject.AddComponent<DamagePopup>();
            popup.text = tmp;
        }

        popup.Initialize(Mathf.RoundToInt(amount).ToString());
    }

    // Editor-only gizmos: visualize attack reach and detection region for tuning in the scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up;
        Gizmos.DrawLine(origin, origin + transform.forward * attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Vector3 center = origin + transform.forward * Mathf.Max(0.1f, attackRange * 0.5f);
        Gizmos.DrawWireSphere(center, Mathf.Max(0.1f, attackRadius));
    }
}
