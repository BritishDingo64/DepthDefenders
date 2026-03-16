using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour {
    [SerializeField]
    float moveSpeed = 2.5f;
    [SerializeField]
    float waypointReachedDistance = 0.25f;
    [SerializeField]
    float crystalReachedDistance = 1f;
    [SerializeField]
    float crystalAttackRange = 1.5f;
    [SerializeField]
    float crystalDamagePerSecond = 10f;
    [SerializeField]
    float barricadeDetectionRange = 7f;
    [SerializeField]
    float barricadeAttackRange = 1.4f;
    [SerializeField]
    float barricadeDamagePerSecond = 20f;
    [SerializeField]
    float playerAttackRange = 1.5f;
    [SerializeField]
    float playerDamagePerSecond = 12f;
    [SerializeField]
    float playerAttackTickInterval = 0.25f;
    [SerializeField]
    float playerStopBuffer = 0.35f;
    [Header("Movement")]
    [SerializeField]
    bool useNavMeshAgent = true;
    [SerializeField]
    float navMeshRepathInterval = 0.15f;
    [Header("Debug")]
    [SerializeField]
    bool debugLogs;
    [Header("Aggro")]
    [SerializeField]
    float playerAggroDistance = 6f;
    [SerializeField, Range(1f, 180f)]
    float playerAggroConeAngle = 45f;
    [SerializeField]
    string playerTag = "Player";
    [SerializeField]
    bool stayAggroAfterDamaged = true;

    Spawner spawner;
    Transform crystalTarget;
    Transform playerTarget;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;
    BarricadeDefenseTower barricadeTarget;
    Crystal crystalComponent;
    NavMeshAgent navMeshAgent;
    Collider ownCollider;
    bool hasBeenDamaged;
    bool notifiedSpawnerDestroyed;
    float speedMultiplier = 1f;
    float slowEndsAt;
    float nextPlayerAttackTime;
    float nextRepathTime;
    bool wasInPlayerAttackRange;
    int nextPathIndex;

    void Awake() {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null) {
            enemyHealth.onDamaged.AddListener(HandleDamaged);
        }

        navMeshAgent = GetComponent<NavMeshAgent>();
        ownCollider = GetComponent<Collider>();

        if (navMeshAgent != null) {
            navMeshAgent.updateRotation = false;
            navMeshAgent.avoidancePriority = Random.Range(30, 70);
        }
    }

    void OnDestroy() {
        if (enemyHealth != null) {
            enemyHealth.onDamaged.RemoveListener(HandleDamaged);
        }

        if (!notifiedSpawnerDestroyed && spawner != null) {
            notifiedSpawnerDestroyed = true;
            spawner.NotifyMonsterDestroyed();
        }
    }

    public void Initialize(Spawner owningSpawner, Transform crystal) {
        spawner = owningSpawner;
        crystalTarget = crystal;
        nextPathIndex = 0;

        if (playerTarget == null) {
            FindPlayerTarget();
        }
    }

    void Update() {
        if (crystalTarget == null && spawner != null) {
            crystalTarget = spawner.GetCrystalTarget();
        }

        if (playerTarget == null) {
            FindPlayerTarget();
        }

        RefreshBarricadeTarget();
        UpdateStatusEffects();

        Vector3 destination = GetCurrentDestination();
        MoveTowards(destination, GetCurrentStoppingDistance());
        FaceCurrentAttackTarget();
        TryAttackPlayer();
        TryAttackBarricade();
        TryAttackCrystal();
    }

    float GetCurrentStoppingDistance() {
        float stoppingDistance = nextPathIndex < (spawner != null ? spawner.PathPointCount : 0) ? waypointReachedDistance : crystalReachedDistance;

        if (ShouldChasePlayer()) {
            stoppingDistance = GetPlayerStoppingDistance();
        }

        if (HasBarricadeTarget()) {
            stoppingDistance = barricadeAttackRange;
        }

        return Mathf.Max(0.1f, stoppingDistance);
    }

    float GetPlayerStoppingDistance() {
        float targetRadius = 0.35f;
        if (playerTarget != null) {
            Collider playerCollider = playerTarget.GetComponent<Collider>();
            if (playerCollider != null) {
                targetRadius = Mathf.Max(targetRadius, playerCollider.bounds.extents.x, playerCollider.bounds.extents.z);
            }
        }

        float selfRadius = 0.35f;
        if (ownCollider != null) {
            selfRadius = Mathf.Max(selfRadius, ownCollider.bounds.extents.x, ownCollider.bounds.extents.z);
        }

        return Mathf.Max(playerAttackRange, targetRadius + selfRadius + playerStopBuffer);
    }

    Vector3 GetCurrentDestination() {
        if (ShouldChasePlayer()) {
            return playerTarget.position;
        }

        if (HasBarricadeTarget()) {
            return barricadeTarget.transform.position;
        }

        if (spawner != null && nextPathIndex < spawner.PathPointCount) {
            Vector3 waypoint = spawner.GetPathPoint(nextPathIndex);
            if (Vector3.Distance(transform.position, waypoint) <= waypointReachedDistance) {
                nextPathIndex++;
                if (nextPathIndex < spawner.PathPointCount) {
                    waypoint = spawner.GetPathPoint(nextPathIndex);
                }
            }

            if (nextPathIndex < spawner.PathPointCount) {
                return waypoint;
            }
        }

        return crystalTarget != null ? crystalTarget.position : transform.position;
    }

    bool ShouldChasePlayer() {
        if (playerTarget == null) return false;
        if (hasBeenDamaged && stayAggroAfterDamaged) return true;
        return IsPlayerInsideAggroCone();
    }

    bool IsPlayerInsideAggroCone() {
        if (playerTarget == null) return false;

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > playerAggroDistance * playerAggroDistance) {
            return false;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f) return true;

        float angleToPlayer = Vector3.Angle(forward.normalized, toPlayer.normalized);
        return angleToPlayer <= playerAggroConeAngle * 0.5f;
    }

    void HandleDamaged() {
        hasBeenDamaged = true;
    }

    void FindPlayerTarget() {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) {
            playerTarget = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
            }

            if (debugLogs && playerHealth == null) {
                Debug.LogWarning($"{name}: Found player object by tag but no PlayerHealth on it.");
            }
            return;
        }

        AdvancedMovement playerMovement = FindFirstObjectByType<AdvancedMovement>();
        if (playerMovement != null) {
            playerTarget = playerMovement.transform;
            playerHealth = playerMovement.GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = playerMovement.GetComponentInChildren<PlayerHealth>();
            }

            if (debugLogs && playerHealth == null) {
                Debug.LogWarning($"{name}: Found AdvancedMovement but no PlayerHealth nearby.");
            }
        }

        if (playerHealth == null) {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null && playerTarget == null) {
                playerTarget = playerHealth.transform;
            }
        }

        if (debugLogs && playerTarget != null && playerHealth != null) {
            Debug.Log($"{name}: Linked to player '{playerTarget.name}' and PlayerHealth.");
        }
    }

    void MoveTowards(Vector3 destination, float stoppingDistance) {
        Vector3 direction = destination - transform.position;

        if (direction.sqrMagnitude <= stoppingDistance * stoppingDistance) {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh) {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
            return;
        }

        if (useNavMeshAgent && navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh) {
            navMeshAgent.speed = moveSpeed * speedMultiplier;
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.isStopped = false;

            if (Time.time >= nextRepathTime) {
                navMeshAgent.SetDestination(destination);
                nextRepathTime = Time.time + Mathf.Max(0.05f, navMeshRepathInterval);
            }

            return;
        }

        Vector3 movement = direction.normalized * moveSpeed * speedMultiplier * Time.deltaTime;
        transform.position += movement;

        Vector3 lookDirection = new Vector3(direction.x, 0f, direction.z);
        if (lookDirection.sqrMagnitude > 0.001f) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 10f * Time.deltaTime);
        }
    }

    void FaceCurrentAttackTarget() {
        Transform attackTarget = null;

        if (ShouldChasePlayer() && IsWithinAttackRange(playerTarget, playerAttackRange)) {
            attackTarget = playerTarget;
        }
        else if (HasBarricadeTarget() && IsWithinAttackRange(barricadeTarget.transform, barricadeAttackRange)) {
            attackTarget = barricadeTarget.transform;
        }
        else if (!HasBarricadeTarget() && crystalTarget != null && IsWithinAttackRange(crystalTarget, crystalAttackRange)) {
            attackTarget = crystalTarget;
        }

        if (attackTarget == null) return;

        Vector3 lookDirection = attackTarget.position - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.001f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 12f * Time.deltaTime);
    }

    void TryAttackPlayer() {
        if (!ShouldChasePlayer() || playerHealth == null || playerDamagePerSecond <= 0f) return;
        bool inRange = IsWithinAttackRange(playerTarget, GetPlayerStoppingDistance());

        if (debugLogs && inRange != wasInPlayerAttackRange) {
            wasInPlayerAttackRange = inRange;
            Debug.Log($"{name}: Player attack range {(inRange ? "entered" : "exited")}. Distance={Vector3.Distance(transform.position, playerTarget.position):0.00}");
        }

        if (!inRange) return;

        if (Time.time < nextPlayerAttackTime) return;

        float tick = Mathf.Max(0.05f, playerAttackTickInterval);
        float damageThisHit = playerDamagePerSecond * tick;
        nextPlayerAttackTime = Time.time + tick;

        bool killed = playerHealth.TakeDamage(damageThisHit);
        if (debugLogs) {
            Debug.Log($"{name}: Hit player for {damageThisHit:0.0}. Player HP now {playerHealth.currentHealth:0.0}/{playerHealth.maxHealth:0.0}. Killed={killed}");
        }
    }

    void TryAttackCrystal() {
        if (ShouldChasePlayer() || HasBarricadeTarget()) return;
        if (crystalTarget == null || crystalDamagePerSecond <= 0f) return;
        if (!IsWithinAttackRange(crystalTarget, crystalAttackRange)) return;

        if (crystalComponent == null) {
            crystalComponent = crystalTarget.GetComponent<Crystal>();
            if (crystalComponent == null) return;
        }

        crystalComponent.TakeDamage(crystalDamagePerSecond * Time.deltaTime);
    }

    void TryAttackBarricade() {
        if (!HasBarricadeTarget() || barricadeDamagePerSecond <= 0f) return;
        if (!IsWithinAttackRange(barricadeTarget.transform, barricadeAttackRange)) return;

        barricadeTarget.TakeDamage(barricadeDamagePerSecond * Time.deltaTime);
    }

    void RefreshBarricadeTarget() {
        if (ShouldChasePlayer()) {
            barricadeTarget = null;
            return;
        }

        if (HasBarricadeTarget()) {
            float sqrDistance = (barricadeTarget.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= barricadeDetectionRange * barricadeDetectionRange) {
                return;
            }
        }

        barricadeTarget = BarricadeDefenseTower.GetClosestActiveBarricade(transform.position, barricadeDetectionRange);
    }

    bool HasBarricadeTarget() {
        return barricadeTarget != null && !barricadeTarget.IsDestroyed;
    }

    void UpdateStatusEffects() {
        if (Time.time > slowEndsAt) {
            speedMultiplier = 1f;
        }

        if (navMeshAgent != null && navMeshAgent.enabled) {
            navMeshAgent.speed = moveSpeed * speedMultiplier;
        }
    }

    public void ApplySlow(float multiplier, float duration) {
        speedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        slowEndsAt = Mathf.Max(slowEndsAt, Time.time + Mathf.Max(0f, duration));
    }

    bool IsWithinAttackRange(Transform target, float range) {
        if (target == null) return false;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= range * range;
    }
}