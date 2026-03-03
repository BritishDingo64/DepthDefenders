using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Spawner : MonoBehaviour {
    //make an editor script that makes this run a line between the points.
    static int enemyCount;
    [SerializeField]
    List<Vector3> MonsterPath;
    [SerializeField]
    float arrowHeadSize = 1;
    [SerializeField]
    Color arrowColour;
    [SerializeField]
    List<Wave> waves;
    public GameObject crystal;
    List<GameObject> instantiatedObjectPool;
    void Start() {
        StartCoroutine(nameof(PoolObjects));
    }
    IEnumerator PoolObjects(int wave) {
        float timeAtStart = Time.deltaTime;
        foreach (MonsterAndType monster in waves[wave].monsters) {
            while (timeAtStart + 0.005f / monster.count < Time.deltaTime) {
                GameObject instantiated = Instantiate(monster.Monster);
                instantiated.SetActive(false);
                instantiated.transform.position = transform.position + Vector3.up;
                instantiatedObjectPool.Add(instantiated);
            }
        }
        yield return null;
    }

    void Update() {
        if (HasWaveStarted()) {
            StartCoroutine(nameof(StartWave));
        }
    }
    bool HasWaveStarted() {
        return crystal.GetComponent<Crystal>().waveStarted;
    }
    IEnumerator StartWave() {
        for (int i = 0; i < waves[crystal.GetComponent<Crystal>().waveNumber].numberOfSubWaves; i++) {
            foreach (MonsterAndType monsterType in waves[crystal.GetComponent<Crystal>().waveNumber].monsters) { 
                for (int j = 0; j < monsterType.count / (waves[crystal.GetComponent<Crystal>().waveNumber].numberOfSubWaves - i); j++) {
                    SpawnMonster(monsterType.Monster.GetComponent<Monster>());
                }
            }
        }
        yield return null;
    }
    void SpawnMonster(Monster monster) {
        
        enemyCount += 1;
    }

    public Vector3 NextPointFromPosition(Vector3 position, Vector3 lastKnownPosition) {
        if (MonsterPath.Contains(lastKnownPosition))return MonsterPath[MonsterPath.FindIndex(x => x == lastKnownPosition) + 1];
        if (MonsterPath.Count == 0) throw new KeyNotFoundException("MonsterPath needs to have at least one element in it");
        return MonsterPath[^1];
    }
    private void OnDrawGizmos() {
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
}
