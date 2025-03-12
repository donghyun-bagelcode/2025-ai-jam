using System;
using UnityEngine;
using UnityEditor;

public class CommentaryWindow : EditorWindow
{
    private string commentary = "";
    private Action<string> onSubmit;
    GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
    

    private void OnEnable()
    {
        textAreaStyle.wordWrap = true;
    }
    public static void ShowWindow(Action<string> onSubmitCallback, string defaultText = "")
    {
        var window = GetWindow<CommentaryWindow>("Enter Commentary");
        window.onSubmit = onSubmitCallback;
        window.minSize = new Vector2(400, 200);
        window.commentary = defaultText;
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter your commentary below:", EditorStyles.boldLabel);
        commentary = EditorGUILayout.TextArea(commentary, textAreaStyle, GUILayout.ExpandHeight(true));

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(100)))
        {
            Close();
        }

        if (GUILayout.Button("OK", GUILayout.Width(100)))
        {
            if (onSubmit != null)
            {
                onSubmit.Invoke(commentary);
            }
            Close();
        }

        GUILayout.EndHorizontal();
    }
}