using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack settings")]
    public float damage = 25f;
    public float attackRange = 3f; // how far the player's attack reaches
    public LayerMask hitMask = ~0; // which layers to hit (default: everything)
    public Camera playerCamera; // assign main camera or leave null to auto-find

    [Header("Feedback")]
    public float attackCooldown = 0.5f;
    private float lastAttackTime = -999f;

    private void Start()
    {
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main;
            else playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            DoAttack();
        }
    }

    private void DoAttack()
    {
        Ray ray;
        // prefer firing from camera center so mouse pointer/crosshair works reliably
        if (playerCamera != null)
            ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        else
            ray = new Ray(transform.position, transform.forward);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, attackRange, hitMask))
        {
            EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            // optional: add hit VFX / sound here
        }
        else
        {
            // optional: miss feedback (swing, sound)
        }
    }

    // debug draw
    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.forward * attackRange);
        }
    }
}
