using UnityEngine;
using UnityEngine.AI;

// Controls individual enemy movement, target selection, combat, and animations.
public class Monster : MonoBehaviour {
    enum AttackTargetType {
        None = 0,
        Player = 1,
        Barricade = 2,
        Crystal = 3
    }

    [SerializeField]
    float moveSpeed = 2.5f;
    [SerializeField]
    float distanceToReachAGivenPoint = 0.25f;
    [SerializeField]
    float attackRange = 1.5f;
    [SerializeField]
    float damagePerAttack = 10f;
    [SerializeField]
    float detectionRange = 7f;
    [SerializeField]
    float attackInterval = 0.25f;
    //[SerializeField]
    //float playerStopBuffer = 0.35f;
    [Header("Animation Combat")]
    [SerializeField]
    Animator animator;
    [SerializeField]
    bool useAnimationEventsForDamage = true;
    [SerializeField]
    float attackAnimationInterval = 0.7f;
    [SerializeField]
    string isMovingParam = "IsMoving";
    [SerializeField]
    string attackTriggerParam = "Attack";
    [SerializeField]
    string attackTargetParam = "AttackTarget";
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
    bool wasInattackRange;
    int nextPathIndex;
    AttackTargetType queuedAttackTarget = AttackTargetType.None;
    float nextAnimationAttackTime;
    bool hasIsMovingParam;
    bool hasAttackTriggerParam;
    bool hasAttackTargetParam;

    void Awake() {
        // Initialize components and links for enemy behavior.
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null) {
            enemyHealth.onDamaged.AddListener(HandleDamaged);
        }

        // get the animator component of this enemy if it is not set.
        if (animator == null) {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimatorParameters();

        // get the navmesh agent of this enemy if it exists and the  collider of it
        navMeshAgent = GetComponent<NavMeshAgent>();
        ownCollider = GetComponent<Collider>();

        // not sure, ai says that changing avoidance priority can make the increased number of enemies less likely to get stuck on each other.
        if (navMeshAgent != null) {
            navMeshAgent.updateRotation = false;
            navMeshAgent.avoidancePriority = Random.Range(30, 70);
        }
    }

    void OnDestroy() {
        // Unsubscribe to prevent dangling event listeners and notify the spawner when destroyed.
        if (enemyHealth != null) {
            enemyHealth.onDamaged.RemoveListener(HandleDamaged);
        }

        if (!notifiedSpawnerDestroyed && spawner != null) {
            notifiedSpawnerDestroyed = true;
            spawner.NotifyMonsterDestroyed();
        }
    }

    public void Initialize(Spawner owningSpawner, Transform crystal) {
        // Set references used during movement and combat.
        spawner = owningSpawner;
        crystalTarget = crystal;
        nextPathIndex = 0;

        if (playerTarget == null) {
            FindPlayerTarget();
        }
    }

    void Update() {
        // Ensure current targets are known, then choose movement and attack behavior.
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

        if (ShouldUseAnimationAttackFlow()) {
            TryStartAnimatedAttack();
        }
        else {
            TryAttackPlayer();
            TryAttackBarricade();
            TryAttackCrystal();
        }
    }

    float GetCurrentStoppingDistance() {
        float stoppingDistance = distanceToReachAGivenPoint;

        if (ShouldChasePlayer()) {
            stoppingDistance = GetPlayerStoppingDistance();
        }

        if (HasBarricadeTarget()) {
            stoppingDistance = attackRange;
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

        return Mathf.Max(attackRange, targetRadius + selfRadius);
    }

    Vector3 GetCurrentDestination() {
        // Decide whether to chase player, barricade, follow path, or move toward the crystal.
        if (ShouldChasePlayer()) {
            // Stop moving if within attack range of player
            if (IsWithinAttackRange(playerTarget, attackRange)) {
                return transform.position;
            }
            return SnapToNavMesh(playerTarget.position);
        }

        if (HasBarricadeTarget()) {
            return SnapToNavMesh(barricadeTarget.transform.position);
        }

        if (spawner != null && nextPathIndex < spawner.PathPointCount) {
            Vector3 waypoint = SnapToNavMesh(spawner.GetPathPoint(nextPathIndex));
            if (Vector3.Distance(transform.position, waypoint) <= distanceToReachAGivenPoint) {
                nextPathIndex++;
                if (nextPathIndex < spawner.PathPointCount) {
                    waypoint = SnapToNavMesh(spawner.GetPathPoint(nextPathIndex));
                }
            }

            if (nextPathIndex < spawner.PathPointCount) {
                return waypoint;
            }
        }

        return crystalTarget != null ? SnapToNavMesh(crystalTarget.position) : transform.position;
    }

    Vector3 SnapToNavMesh(Vector3 worldPosition, float sampleDistance = 3f) {
        if (navMeshAgent != null && navMeshAgent.enabled) {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(worldPosition, out hit, sampleDistance, navMeshAgent.areaMask)) {
                return hit.position;
            }
        }

        return worldPosition;
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

        if (toPlayer.sqrMagnitude > Mathf.Pow(playerAggroDistance, 2)) {
            return false;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f) return true;

        float angleToPlayer = Vector3.Angle(forward.normalized, toPlayer.normalized);
        return angleToPlayer <= playerAggroConeAngle * 0.5f;
    }

    void HandleDamaged() {
        // When damaged, the monster becomes aggressive and may play a hit animation.
        hasBeenDamaged = true;

        if (animator != null) {
            animator.SetTrigger("GotHit");
        }
    }

    void FindPlayerTarget() {
        // Find the player by tag or fall back to other player-related components.
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) {
            playerTarget = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
            }

            if (debugLogs && playerHealth == null) {
                Debug.Log($"{name}: Found player object by tag but no PlayerHealth on it.");
            }
            return;
        }

        Movement playerMovement = FindFirstObjectByType<Movement>();
        if (playerMovement != null) {
            playerTarget = playerMovement.transform;
            playerHealth = playerMovement.GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = playerMovement.GetComponentInChildren<PlayerHealth>();
            }

            if (debugLogs && playerHealth == null) {
                Debug.Log($"{name}: Found Movement but no PlayerHealth nearby.");
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
        // Move the monster toward the current destination, using NavMesh when available.
        Vector3 direction = destination - transform.position;
        bool isMoving = direction.sqrMagnitude > stoppingDistance * stoppingDistance;

        UpdateAnimatorMove(isMoving);

        if (!isMoving) {
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
        // Rotate toward the current attack target for a more natural combat orientation.
        Transform attackTarget = null;

        if (playerHealth != null && IsWithinAttackRange(playerTarget, GetPlayerStoppingDistance())) {
            attackTarget = playerTarget;
        }
        else if (HasBarricadeTarget() && IsWithinAttackRange(barricadeTarget.transform, attackRange)) {
            attackTarget = barricadeTarget.transform;
        }
        else if (!HasBarricadeTarget() && crystalTarget != null && IsWithinAttackRange(crystalTarget, attackRange)) {
            attackTarget = crystalTarget;
        }

        if (attackTarget == null) return;

        Vector3 lookDirection = attackTarget.position - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.001f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 12f * Time.deltaTime);
    }

    bool ShouldUseAnimationAttackFlow() {
        // Determine whether this monster uses animation events to deal damage.
        return useAnimationEventsForDamage && animator != null;
    }

    void TryStartAnimatedAttack() {
        AttackTargetType currentTarget = ResolveAttackTargetInRange();
        if (currentTarget == AttackTargetType.None) return;
        if (Time.time < nextAnimationAttackTime) return;

        queuedAttackTarget = currentTarget;
        nextAnimationAttackTime = Time.time + Mathf.Max(0.05f, attackAnimationInterval);

        if (hasAttackTargetParam) {
            animator.SetInteger(attackTargetParam, (int)currentTarget);
        }

        if (hasAttackTriggerParam) {
            animator.SetTrigger(attackTriggerParam);
        }
    }

    AttackTargetType ResolveAttackTargetInRange() {
        // Choose the highest-priority target that is currently within attack range.
        if (playerHealth != null && IsWithinAttackRange(playerTarget, GetPlayerStoppingDistance())) {
            return AttackTargetType.Player;
        }

        if (HasBarricadeTarget() && IsWithinAttackRange(barricadeTarget.transform, attackRange)) {
            return AttackTargetType.Barricade;
        }

        if (!HasBarricadeTarget() && !ShouldChasePlayer() && crystalTarget != null && IsWithinAttackRange(crystalTarget, attackRange)) {
            return AttackTargetType.Crystal;
        }

        return AttackTargetType.None;
    }

    void UpdateAnimatorMove(bool isMoving) {
        if (animator == null) return;

        if (hasIsMovingParam) {
            animator.SetBool(isMovingParam, isMoving);
        }
    }

    void CacheAnimatorParameters() {
        hasIsMovingParam = HasAnimatorParameter(isMovingParam, AnimatorControllerParameterType.Bool);
        hasAttackTriggerParam = HasAnimatorParameter(attackTriggerParam, AnimatorControllerParameterType.Trigger);
        hasAttackTargetParam = HasAnimatorParameter(attackTargetParam, AnimatorControllerParameterType.Int);
    }

    bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType expectedType) {
        if (animator == null || string.IsNullOrWhiteSpace(paramName)) {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++) {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter != null && parameter.name == paramName && parameter.type == expectedType) {
                return true;
            }
        }

        return false;
    }

    void TryAttackPlayer() {
        // If a player is in range, damage them over time based on attack interval.
        if (playerHealth == null || damagePerAttack <= 0f) return;
        bool inRange = IsWithinAttackRange(playerTarget, GetPlayerStoppingDistance());

        if (debugLogs && inRange != wasInattackRange) {
            wasInattackRange = inRange;
            Debug.Log($"{name}: Player attack range {(inRange ? "entered" : "exited")}. Distance={Vector3.Distance(transform.position, playerTarget.position):0.00}");
        }

        if (!inRange) return;

        if (Time.time < nextPlayerAttackTime) return;

        float tick = Mathf.Max(0.05f, attackInterval);
        float damageThisHit = damagePerAttack * tick;
        nextPlayerAttackTime = Time.time + tick;

        bool killed = playerHealth.TakeDamage(damageThisHit);
        if (debugLogs) {
            Debug.Log($"{name}: Hit player for {damageThisHit:0.0}. Player HP now {playerHealth.currentHealth:0.0}/{playerHealth.maxHealth:0.0}. Killed={killed}");
        }
    }

    void TryAttackCrystal() {
        // Deal damage to the crystal only when not distracted by player or barricade.
        if (ShouldChasePlayer() || HasBarricadeTarget()) return;
        if (crystalTarget == null || damagePerAttack <= 0f) return;
        if (!IsWithinAttackRange(crystalTarget, attackRange)) return;

        if (crystalComponent == null) {
            crystalComponent = crystalTarget.GetComponent<Crystal>();
            if (crystalComponent == null) return;
        }

        crystalComponent.TakeDamage(damagePerAttack * Time.deltaTime);
    }

    void TryAttackBarricade() {
        // If a barricade is the current target, deal damage to it.
        if (!HasBarricadeTarget() || damagePerAttack <= 0f) return;
        if (!IsWithinAttackRange(barricadeTarget.transform, attackRange)) return;

        barricadeTarget.TakeDamage(damagePerAttack * Time.deltaTime);
    }

    void RefreshBarricadeTarget() {
        // Update barricade target only when the monster is not chasing the player.
        if (ShouldChasePlayer()) {
            barricadeTarget = null;
            return;
        }

        if (HasBarricadeTarget()) {
            float sqrDistance = (barricadeTarget.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= detectionRange * detectionRange) {
                return;
            }
        }

        barricadeTarget = BarricadeDefenseTower.GetClosestActiveBarricade(transform.position, detectionRange);
    }

    bool HasBarricadeTarget() {
        return barricadeTarget != null && !barricadeTarget.IsDestroyed;
    }

    void UpdateStatusEffects() {
        // Restore normal speed when slow effects expire.
        if (Time.time > slowEndsAt) {
            speedMultiplier = 1f;
        }

        if (navMeshAgent != null && navMeshAgent.enabled) {
            navMeshAgent.speed = moveSpeed * speedMultiplier;
        }
    }

    public void ApplySlow(float multiplier, float duration) {
        // Apply a temporary slow effect, reducing movement speed.
        speedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        slowEndsAt = Mathf.Max(slowEndsAt, Time.time + Mathf.Max(0f, duration));
    }

    public void AnimationEvent_DealDamage() {
        // This method is called by animation events to apply damage at the correct frame.
        if (!ShouldUseAnimationAttackFlow()) return;

        switch (queuedAttackTarget) {
            case AttackTargetType.Player:
                DealDamageToPlayerHit();
                break;
            case AttackTargetType.Barricade:
                DealDamageToBarricadeHit();
                break;
            case AttackTargetType.Crystal:
                DealDamageToCrystalHit();
                break;
        }
    }

    public void AnimationEvent_ClearQueuedAttack() {
        // Clear the stored attack target when the animation finishes.
        queuedAttackTarget = AttackTargetType.None;
    }

    // Animation event aliases for clips that use common method names.
    public void Attack() {
        AnimationEvent_DealDamage();
    }

    public void DealDamage() {
        AnimationEvent_DealDamage();
    }

    public void EndAttack() {
        AnimationEvent_ClearQueuedAttack();
    }

    void DealDamageToPlayerHit() {
        if (playerHealth == null || damagePerAttack <= 0f) return;
        if (!IsWithinAttackRange(playerTarget, GetPlayerStoppingDistance())) return;

        float damage = damagePerAttack * Mathf.Max(0.05f, attackAnimationInterval);
        bool killed = playerHealth.TakeDamage(damage);

        if (debugLogs) {
            Debug.Log($"{name}: Animated hit player for {damage:0.0}. Player HP now {playerHealth.currentHealth:0.0}/{playerHealth.maxHealth:0.0}. Killed={killed}");
        }
    }

    void DealDamageToBarricadeHit() {
        if (!HasBarricadeTarget() || damagePerAttack <= 0f) return;
        if (!IsWithinAttackRange(barricadeTarget.transform, attackRange)) return;

        float damage = damagePerAttack * Mathf.Max(0.05f, attackAnimationInterval);
        barricadeTarget.TakeDamage(damage);
    }

    void DealDamageToCrystalHit() {
        if (ShouldChasePlayer() || HasBarricadeTarget()) return;
        if (crystalTarget == null || damagePerAttack <= 0f) return;
        if (!IsWithinAttackRange(crystalTarget, attackRange)) return;

        if (crystalComponent == null) {
            crystalComponent = crystalTarget.GetComponent<Crystal>();
            if (crystalComponent == null) return;
        }

        float damage = damagePerAttack * Mathf.Max(0.05f, attackAnimationInterval);
        crystalComponent.TakeDamage(damage);
    }

    bool IsWithinAttackRange(Transform target, float range) {
        // Check horizontal distance against attack range.
        if (target == null) return false;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= range * range;
    }
}