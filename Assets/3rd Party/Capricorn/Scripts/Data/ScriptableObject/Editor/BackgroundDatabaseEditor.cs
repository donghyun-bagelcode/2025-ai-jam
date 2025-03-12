using Dunward.Capricorn;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BackgroundDatabase))]
public class BackgroundDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        var database = (BackgroundDatabase)target;

        if (GUILayout.Button("Sync"))
        {
            database.Sync();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        serializedObject.ApplyModifiedProperties();
    }
}