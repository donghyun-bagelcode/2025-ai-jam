using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class ClaudeSonnetSetting : EditorWindow
{
    private const string modelName = "ClaudeSonnet";
    private string apiKey = "";
    private float temperature = 0.5f;

    //[MenuItem("Leman/Settings/Claude Sonnet Settings", false, 102)]
    public static void OpenWindow()
    {
        var window = GetWindow<ClaudeSonnetSetting>(true, "Claude Sonnet Settings", true);
        window.LoadSettings();

    }

    void OnGUI()
    {
        GUILayout.Label("Claude Sonnet Settings", EditorStyles.boldLabel);

        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        temperature = EditorGUILayout.Slider("Temperature", temperature, 0.0f, 1.0f);

        if (GUILayout.Button("Save"))
        {
            SaveSettings();
            EditorUtility.DisplayDialog("Settings Saved", "Your settings have been saved successfully.", "OK");
        }
    }
    private void SaveSettings()
    {
        SecurePlayerPrefs.SetString("ClaudeSonnet_ApiKey", apiKey);
        SecurePlayerPrefs.SetFloat("ClaudeSonnet_Temperature", temperature);
        SecurePlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        apiKey = SecurePlayerPrefs.GetString("ClaudeSonnet_ApiKey", "None");
        temperature = SecurePlayerPrefs.GetFloat("ClaudeSonnet_Temperature", 0.5f);
    }
}