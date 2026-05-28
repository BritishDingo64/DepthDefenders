using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Manages spawning enemy waves, pathing, and tracking active enemies.
public class Spawner : MonoBehaviour {
    static int enemyCount;
    [SerializeField]
    List<Vector3> MonsterPath;
    [SerializeField]
    float arrowHeadSize = 1;
    [SerializeField]
    Color arrowColour;
    [Header("Starting Wave")]
    [Tooltip("Single starting wave template used for all later waves. Use the skeletons and knights slots.")]
    [SerializeField]
    Wave startingWave;

    [Header("Spawn Ramp")]
    [Tooltip("Skeletons added per wave for each spawner until the max enemy count is reached.")]
    [SerializeField, Min(1)]
    int skeletonsPerWavePerSpawner = 2;

    [Tooltip("Maximum enemies each spawner can create in a single wave.")]
    [SerializeField, Min(1)]
    int maxEnemiesPerSpawner = 20;

    [Tooltip("Knight share while the spawner is still below the enemy cap.")]
    [SerializeField, Range(0f, 1f)]
    float knightShareBeforeCap = 0.1f;

    [Tooltip("Knight share added each wave after the cap is reached.")]
    [SerializeField, Range(0f, 1f)]
    float knightShareIncreaseAfterCap = 0.1f;

    [Tooltip("Damage multiplier applied to knights each wave once the wave is 100% knights.")]
    [SerializeField, Min(1f)]
    float fullKnightDamageMultiplierPerWave = 1.1f;

    [SerializeField]
    float timeBetweenSpawns = 0.35f;
    public GameObject crystal;
    List<GameObject> instantiatedObjectPool = new();
    Crystal crystalComponent;
    bool isSpawningWave;
    int lastStartedWave;
    bool hasWarnedMissingEnemyPrefab;

    public static int ActiveEnemyCount => Mathf.Max(0, enemyCount);
    public bool IsSpawningWave => isSpawningWave;
    public int LastStartedWave => lastStartedWave;
    public int TotalWaves => startingWave == null ? 0 : 1;

    private void Awake() {
        // Cache the crystal component to know when waves should start.
        if (crystal != null) crystalComponent = crystal.GetComponent<Crystal>();
    }

    private void OnValidate() {
        ClampStartingWaveMonsterFields();
    }

    void Update() {
        if (CanStartWave()) {
            StartCoroutine(StartWave());
        }
    }

    bool CanStartWave() {
        // Only start a spawn coroutine when a new wave has been triggered by the crystal.
        return !isSpawningWave && crystalComponent != null && crystalComponent.waveStarted && crystalComponent.waveNumber > 0 && crystalComponent.waveNumber != lastStartedWave && startingWave != null;
    }

    public bool HasWaveConfigured(int waveNumber) {
        return waveNumber > 0 && startingWave != null;
    }

    public bool HasPendingOrActiveWave(int waveNumber) {
        if (!HasWaveConfigured(waveNumber)) return false;
        return isSpawningWave || lastStartedWave < waveNumber;
    }

    IEnumerator StartWave() {
        // Spawn enemies for the current wave using a fixed per-spawner ramp.
        isSpawningWave = true;
        int waveNumber = crystalComponent.waveNumber;
        Wave currentWave = startingWave;
        Dictionary<MonsterAndType, int> spawnCounts = BuildRequestedCounts(currentWave, waveNumber);

        foreach (MonsterAndType monsterType in GetMonsterSlots(currentWave)) {
            if (monsterType == null || !spawnCounts.ContainsKey(monsterType)) continue;

            for (int spawnIndex = 0; spawnIndex < spawnCounts[monsterType]; spawnIndex++) {
                bool isKnight = monsterType == currentWave.knights;
                SpawnMonster(monsterType.monster, waveNumber, isKnight);

                if (timeBetweenSpawns > 0f) {
                    yield return new WaitForSeconds(timeBetweenSpawns);
                }
            }
        }

        lastStartedWave = waveNumber;
        isSpawningWave = false;
        yield return null;
    }

    void SpawnMonster(GameObject monster, int waveNumber = 1, bool isKnight = false) {
        // Instantiate a monster prefab and initialize its behavior.
        if (monster == null) {
            if (!hasWarnedMissingEnemyPrefab) {
                Debug.LogWarning($"{name} is missing an enemy prefab to spawn.", this);
                hasWarnedMissingEnemyPrefab = true;
            }
            return;
        }

        Vector3 spawnPosition = transform.position;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(spawnPosition, out navHit, 2.5f, NavMesh.AllAreas)) {
            spawnPosition = navHit.position;
        }

        GameObject newMonster = Instantiate(monster, spawnPosition, monster.transform.rotation);
        instantiatedObjectPool.Add(newMonster);
        enemyCount += 1;

        Monster monsterComponent = newMonster.GetComponent<Monster>();
        if (monsterComponent == null) {
            monsterComponent = newMonster.GetComponentInChildren<Monster>();
        }
        if (monsterComponent == null) {
            monsterComponent = newMonster.AddComponent<Monster>();
        }

        float statMultiplier = isKnight ? GetKnightDamageMultiplier(waveNumber) : 1f;

        EnemyHealth enemyHealth = newMonster.GetComponent<EnemyHealth>();
        if (enemyHealth == null) {
            enemyHealth = newMonster.GetComponentInChildren<EnemyHealth>();
        }
        if (enemyHealth != null) {
            enemyHealth.maxHealth *= statMultiplier;
            enemyHealth.currentHealth = enemyHealth.maxHealth;
        }

        monsterComponent.Initialize(this, crystal != null ? crystal.transform : null);
        monsterComponent.MultiplyDamage(statMultiplier);
    }

    Dictionary<MonsterAndType, int> BuildRequestedCounts(Wave currentWave, int waveNumber) {
        Dictionary<MonsterAndType, int> requestedCounts = new();

        MonsterAndType skeletonSlot = currentWave != null ? currentWave.skeletons : null;
        MonsterAndType knightSlot = currentWave != null ? currentWave.knights : null;

        if (skeletonSlot == null && knightSlot == null) {
            return requestedCounts;
        }

        int totalCount = GetTotalEnemiesPerSpawner(waveNumber);
        int knightCount = GetKnightCount(waveNumber, totalCount);
        int skeletonCount = Mathf.Max(0, totalCount - knightCount);

        if (skeletonSlot != null) {
            requestedCounts[skeletonSlot] = skeletonCount;
        }

        if (knightSlot != null) {
            requestedCounts[knightSlot] = knightCount;
        }

        return requestedCounts;
    }

    int GetTotalEnemiesPerSpawner(int waveNumber) {
        int waves = Mathf.Max(1, waveNumber);
        int total = waves * Mathf.Max(1, skeletonsPerWavePerSpawner);
        return Mathf.Min(maxEnemiesPerSpawner, total);
    }

    int GetKnightCount(int waveNumber, int totalCount) {
        if (totalCount <= 0) {
            return 0;
        }

        int maxWaveToReachCap = GetWaveNumberAtMaxEnemyCount();
        if (waveNumber <= maxWaveToReachCap) {
            return Mathf.Clamp(Mathf.RoundToInt(totalCount * knightShareBeforeCap), 0, totalCount);
        }

        int wavesPastCap = Mathf.Max(0, waveNumber - maxWaveToReachCap);
        float knightRatio = Mathf.Clamp01(knightShareBeforeCap + (wavesPastCap * knightShareIncreaseAfterCap));
        return Mathf.Clamp(Mathf.RoundToInt(totalCount * knightRatio), 0, totalCount);
    }

    float GetKnightDamageMultiplier(int waveNumber) {
        int maxWaveToReachCap = GetWaveNumberAtMaxEnemyCount();
        int fullKnightWave = GetWaveNumberAtFullKnightRatio(maxWaveToReachCap);
        if (waveNumber < fullKnightWave) {
            return 1f;
        }

        int wavesPastFullKnightRatio = Mathf.Max(1, waveNumber - fullKnightWave + 1);
        return Mathf.Pow(fullKnightDamageMultiplierPerWave, wavesPastFullKnightRatio);
    }

    int GetWaveNumberAtMaxEnemyCount() {
        return Mathf.Max(1, Mathf.CeilToInt((float)maxEnemiesPerSpawner / Mathf.Max(1, skeletonsPerWavePerSpawner)));
    }

    int GetWaveNumberAtFullKnightRatio(int maxWaveToReachCap) {
        if (knightShareIncreaseAfterCap <= 0f) {
            return maxWaveToReachCap;
        }

        int wavesPastCapToReachFullKnights = Mathf.CeilToInt(Mathf.Max(0f, 1f - knightShareBeforeCap) / knightShareIncreaseAfterCap);
        return maxWaveToReachCap + wavesPastCapToReachFullKnights;
    }

    MonsterAndType[] GetMonsterSlots(Wave wave) {
        if (wave == null) return new MonsterAndType[0];

        return new[] { wave.skeletons, wave.knights };
    }

    void ClampStartingWaveMonsterFields() {
        if (startingWave == null) return;

        if (startingWave.skeletons != null && startingWave.skeletons.count < 0) {
            startingWave.skeletons.count = 0;
        }

        if (startingWave.knights != null && startingWave.knights.count < 0) {
            startingWave.knights.count = 0;
        }
    }

    private readonly struct SpawnerBudget {
        public readonly Spawner Spawner;
        public readonly int FloorBudget;
        public readonly float Remainder;

        public SpawnerBudget(Spawner spawner, int floorBudget, float remainder) {
            Spawner = spawner;
            FloorBudget = floorBudget;
            Remainder = remainder;
        }
    }

    public void NotifyMonsterDestroyed() {
        // Decrement the shared active enemy counter when a monster is destroyed.
        enemyCount = Mathf.Max(0, enemyCount - 1);
    }

    public int PathPointCount => MonsterPath == null ? 0 : MonsterPath.Count;

    public Vector3 GetPathPoint(int index) {
        return transform.position + MonsterPath[index];
    }

    public Transform GetCrystalTarget() {
        return crystal != null ? crystal.transform : null;
    }

    public Vector3 NextPointFromPosition(Vector3 position, Vector3 lastKnownPosition) {
        if (MonsterPath.Contains(lastKnownPosition)) return MonsterPath[MonsterPath.FindIndex(x => x == lastKnownPosition) + 1];
        if (MonsterPath.Count == 0) throw new KeyNotFoundException("MonsterPath needs to have at least one element in it");
        return MonsterPath[^1];
    }
    public Vector3 NextPointFromPreviousPoint(Vector3 priorPosition) {
        return MonsterPath.Contains(priorPosition) ? MonsterPath[MonsterPath.IndexOf(priorPosition) + 1] : crystal.transform.position;
    }
    #region DrawPath
    private void OnDrawGizmosSelected() {
        // Visualize the monster path in the editor when the spawner is selected.
        if (MonsterPath.Count == 0) return;
        for (int i = 0; i < MonsterPath.Count; i++) {
            if (i == 0) {
                DrawLine(new Vector3(transform.position.x, 5, transform.position.z), new Vector3(transform.position.x, 5, transform.position.z) + MonsterPath[0]);
                continue;
            }
            DrawLine(new Vector3(transform.position.x, 5, transform.position.z) + MonsterPath[i - 1], new Vector3(transform.position.x, 5, transform.position.z) + MonsterPath[i]);
        }
    }
    private void DrawLine(Vector3 pos1, Vector3 pos2) {
        arrowColour.a = 1;
        Gizmos.color = arrowColour;
        Gizmos.DrawLine(pos1, pos2);
        Vector3 lineDirection = (pos2 - pos1).normalized;
        Vector3 newLineDirection = new Vector3(-0.707f * lineDirection.x + 0.707f * lineDirection.z, -0.707f * lineDirection.y, -0.707f * lineDirection.x - 0.707f * lineDirection.z);
        Gizmos.DrawLine(pos2, pos2 + newLineDirection * arrowHeadSize);
        Vector3 lineDirection2 = (pos2 - pos1).normalized;
        Vector3 newLineDirection2 = new Vector3(-0.707f * lineDirection2.x - 0.707f * lineDirection2.z, -0.707f * lineDirection2.y, 0.707f * lineDirection2.x - 0.707f * lineDirection2.z);
        Gizmos.DrawLine(pos2, pos2 + newLineDirection2 * arrowHeadSize);
    }
    #endregion
}