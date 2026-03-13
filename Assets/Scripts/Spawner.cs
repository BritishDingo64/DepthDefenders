using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    List<GameObject> instantiatedObjectPool = new();
    private void Awake() {
        for (int i = 0; i < waves.Count(); i++)
            foreach (MonsterAndType a in waves[i].monsters)
                a.monsterComponent = a.monster.GetComponent<Monster>();
    }
    void Start() {
        StartCoroutine(PoolObjects(0));
    }
    IEnumerator PoolObjects(int wave) {
        float timeAtStart = Time.deltaTime;
        foreach (MonsterAndType monster in waves[wave].monsters) {

            while (instantiatedObjectPool.Where(x => x.name == monster.monsterComponent.name).Count() < monster.count) {
                Debug.Log(instantiatedObjectPool.Count() + ", " + instantiatedObjectPool.Where(x => x.name == monster.monsterComponent.name).Count() + ", " + monster.monsterComponent.name);
                GameObject instantiated = Instantiate(monster.monster);
                instantiated.SetActive(false);
                instantiated.transform.position = transform.position + Vector3.up;
                if (instantiated.GetComponent<Monster>() == null) instantiated.AddComponent<Monster>();
                instantiated.GetComponent<Monster>().name = monster.monsterComponent.name;
                instantiated.GetComponent<Monster>().spawner = gameObject;
                instantiated.GetComponent<Monster>().nextTarget = MonsterPath[0];
                instantiatedObjectPool.Add(instantiated);
                yield return null;
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
        for (int i = 0; i < waves[crystal.GetComponent<Crystal>().waveNumber - 1].numberOfSubWaves; i++) {
            foreach (MonsterAndType monsterType in waves[crystal.GetComponent<Crystal>().waveNumber - 1].monsters) {
                for (int j = 0; j < monsterType.count / (waves[crystal.GetComponent<Crystal>().waveNumber - 1].numberOfSubWaves - i); j++) {
                    SpawnMonster(monsterType.monster);
                }
            }
        }
        yield return null;
    }
    void SpawnMonster(GameObject monster) {
        GameObject newMonster = Instantiate(monster);
        instantiatedObjectPool.Add(newMonster);
        enemyCount += 1;
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