using Dunward.Capricorn;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterDatabase))]
public class CharacterDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        var database = (CharacterDatabase)target;

        if (GUILayout.Button("Sync"))
        {
            database.Sync();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        serializedObject.ApplyModifiedProperties();
    }
}