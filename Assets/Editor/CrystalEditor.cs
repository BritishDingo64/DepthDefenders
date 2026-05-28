using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Crystal))]
public class CrystalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

        GUI.enabled = Application.isPlaying;
        bool triggerGameOver = EditorGUILayout.ToggleLeft("Trigger Game Over", false);
        if (triggerGameOver)
        {
            ((Crystal)target).TriggerGameOverForTesting();
            GUI.changed = true;
        }
        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the Trigger Game Over checkbox.", MessageType.Info);
        }
    }
}
