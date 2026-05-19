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
    [SerializeField]
    List<Wave> waves;

    [Header("Scaling Settings")]
    [Tooltip("Percent increase in enemy counts per wave (0.08 = 8% per wave)")]
    [SerializeField]
    float enemyIncreasePercentPerWave = 0.08f;

    [Tooltip("Color used when stat scaling starts (tintFactor approaching 0)")]
    [SerializeField]
    Color tintDark = new Color(0.5f, 0.05f, 0.05f);

    [Tooltip("Color used at maximum stat scaling (tintFactor == 1)")]
    [SerializeField]
    Color tintBright = new Color(1f, 0.05f, 0.05f);

    [Header("Stat Multipliers")]
    [Tooltip("Maximum multiplier applied to enemy health at highest waves")]
    [SerializeField]
    float maxHealthMultiplier = 2.5f;

    [Tooltip("Maximum multiplier applied to enemy damage at highest waves")]
    [SerializeField]
    float maxDamageMultiplier = 2.5f;

    [SerializeField]
    float timeBetweenSpawns = 0.35f;
    [SerializeField]
    float timeBetweenSubWaves = 1f;
    public GameObject crystal;
    List<GameObject> instantiatedObjectPool = new();
    Crystal crystalComponent;
    bool isSpawningWave;
    int lastStartedWave;
    bool hasWarnedMissingEnemyPrefab;

    public static int ActiveEnemyCount => Mathf.Max(0, enemyCount);
    public bool IsSpawningWave => isSpawningWave;
    public int LastStartedWave => lastStartedWave;
    public int TotalWaves => waves == null ? 0 : waves.Count;

    private void Awake() {
        // Cache the crystal component to know when waves should start.
        if (crystal != null) crystalComponent = crystal.GetComponent<Crystal>();
    }

    void Update() {
        if (CanStartWave()) {
            StartCoroutine(StartWave());
        }
    }

    bool CanStartWave() {
        // Only start a spawn coroutine when a new wave has been triggered by the crystal.
        return !isSpawningWave && crystalComponent != null && crystalComponent.waveStarted && crystalComponent.waveNumber > 0 && crystalComponent.waveNumber != lastStartedWave && (waves != null && waves.Count > 0);
    }

    public bool HasWaveConfigured(int waveNumber) {
        // We allow infinite waves by treating the last configured wave as a template
        return waveNumber > 0 && TotalWaves > 0;
    }

    public bool HasPendingOrActiveWave(int waveNumber) {
        if (!HasWaveConfigured(waveNumber)) return false;
        return isSpawningWave || lastStartedWave < waveNumber;
    }

    IEnumerator StartWave() {
        // Spawn enemies across subwaves for the current wave.
        isSpawningWave = true;
        int waveNumber = crystalComponent.waveNumber;
        // Use last defined wave as template when waveNumber exceeds configured waves
        int templateIndex = Mathf.Clamp(waveNumber, 1, waves.Count) - 1;
        Wave currentWave = waves[templateIndex];
        int subWaveCount = Mathf.Max(1, currentWave.numberOfSubWaves);
        Dictionary<MonsterAndType, int> remainingCounts = new();

        // Support infinite waves: when waveNumber exceeds configured waves, use the last configured wave
        // as a template and scale counts by waveMultiplier so enemy numbers grow each wave.
        float waveMultiplier = 1f + Mathf.Max(0, waveNumber - 1) * enemyIncreasePercentPerWave;

        foreach (MonsterAndType monsterType in currentWave.monsters) {
            if (monsterType == null) continue;
            int baseCount = Mathf.Max(0, monsterType.count);
            remainingCounts[monsterType] = Mathf.CeilToInt(baseCount * waveMultiplier);
        }

        for (int subWaveIndex = 0; subWaveIndex < subWaveCount; subWaveIndex++) {
            int remainingSubWaves = subWaveCount - subWaveIndex;

            foreach (MonsterAndType monsterType in currentWave.monsters) {
                if (monsterType == null || !remainingCounts.ContainsKey(monsterType)) continue;

                int spawnCountThisSubWave = Mathf.CeilToInt((float)remainingCounts[monsterType] / remainingSubWaves);

                for (int spawnIndex = 0; spawnIndex < spawnCountThisSubWave; spawnIndex++) {
                    SpawnMonster(monsterType.monster, waveNumber);

                    if (timeBetweenSpawns > 0f) {
                        yield return new WaitForSeconds(timeBetweenSpawns);
                    }
                }

                remainingCounts[monsterType] -= spawnCountThisSubWave;
            }

            if (subWaveIndex < subWaveCount - 1 && timeBetweenSubWaves > 0f) {
                yield return new WaitForSeconds(timeBetweenSubWaves);
            }
        }

        lastStartedWave = waveNumber;
        isSpawningWave = false;
        yield return null;
    }

    void SpawnMonster(GameObject monster, int waveNumber = 1) {
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
        // Apply stat scaling based on wave progression
        // Tinting is naturally driven by the stat multiplier itself
        int waveForScaling = Mathf.Max(1, waveNumber);
        float tintFactor = Mathf.Clamp01((float)(waveForScaling - 1) / 10f); // Full tint by wave 11
        float healthMultiplier = Mathf.Lerp(1f, maxHealthMultiplier, tintFactor);
        float damageMultiplier = Mathf.Lerp(1f, maxDamageMultiplier, tintFactor);

        EnemyHealth eh = newMonster.GetComponent<EnemyHealth>();
        if (eh != null) {
            eh.maxHealth *= healthMultiplier;
            eh.currentHealth = eh.maxHealth;
        }

        monsterComponent.Initialize(this, crystal != null ? crystal.transform : null);
        monsterComponent.MultiplyDamage(damageMultiplier);

        // Apply tint to renderers based on stat scaling (tint starts when stats scale)
        if (tintFactor > 0f) {
            Renderer[] rends = newMonster.GetComponentsInChildren<Renderer>();
            Color tint = Color.Lerp(tintDark, tintBright, tintFactor);
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            for (int i = 0; i < rends.Length; i++) {
                Renderer r = rends[i];
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", tint);
                mpb.SetColor("_BaseColor", tint);
                r.SetPropertyBlock(mpb);
            }
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