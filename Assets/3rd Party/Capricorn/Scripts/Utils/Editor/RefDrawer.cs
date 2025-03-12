using Dunward.Capricorn;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Ref<>), true)]
public class RefDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty valueProp = property.FindPropertyRelative("_value");

        if (valueProp != null)
        {
            EditorGUI.PropertyField(position, valueProp, GUIContent.none);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Unsupported Type");
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty valueProp = property.FindPropertyRelative("value");
        return valueProp != null ? EditorGUI.GetPropertyHeight(valueProp) : base.GetPropertyHeight(property, label);
    }
}
