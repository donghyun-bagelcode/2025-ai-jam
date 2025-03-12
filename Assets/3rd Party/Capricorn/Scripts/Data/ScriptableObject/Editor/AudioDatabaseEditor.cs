using Dunward.Capricorn;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioDatabase))]
public class AudioDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        var database = (AudioDatabase)target;

        if (GUILayout.Button("Sync"))
        {
            database.Sync();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        serializedObject.ApplyModifiedProperties();
    }
}