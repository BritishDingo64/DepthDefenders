using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Spawner : MonoBehaviour {
    //make an editor script that makes this run a line between the points.
    [SerializeField]
    List<Vector3> MonsterPath;
    [SerializeField]
    float arrowHeadSize;
    void Start() {

    }

    void Update() {

    }
    void SpawnMonster(Monster monster) {

    }
    private void OnDrawGizmos() {
        if (MonsterPath.Count == 0) return;
        for (int i = 0; i < MonsterPath.Count; i++) {
            if (i == 0) {
                DrawLine(transform.position, MonsterPath[0]);
                continue;
            }
            DrawLine(MonsterPath[i - 1], MonsterPath[i]);
        }
    }
    private void DrawLine(Vector3 pos1, Vector3 pos2) {
        Gizmos.DrawLine(pos1, pos2);
        Vector3 lineDirection = (pos2 - pos1).normalized;
        Vector3 newLineDirection = new Vector3(-0.707f * lineDirection.x + 0.707f * lineDirection.z, -0.707f * lineDirection.y, -0.707f * lineDirection.x - 0.707f * lineDirection.z);
        Gizmos.DrawLine(pos2, pos2 + newLineDirection * arrowHeadSize);
        Vector3 lineDirection2 = (pos2 - pos1).normalized;
        Vector3 newLineDirection2 = new Vector3(-0.707f * lineDirection2.x - 0.707f * lineDirection2.z, -0.707f * lineDirection2.y, 0.707f * lineDirection2.x - 0.707f * lineDirection2.z);
        Gizmos.DrawLine(pos2, pos2 + newLineDirection2 * arrowHeadSize);
    }
}
