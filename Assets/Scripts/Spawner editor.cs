#if Unity_Editor
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Spawner))]
public class SpawnerEditor : Editor
{
    SerializedProperty route;
    void OnEnable(){
        route = serializedObject.FindProperty("MonsterPath");
    }
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();
    }

}
#endif