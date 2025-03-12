using System.Collections;

using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Variable")]
    public class SetVariableUnit : CoroutineUnit
    {
        public string key;
        public OperationType operation;
        public int value;

    #if UNITY_EDITOR
        protected override string info => "Set variable";
        protected override bool supportWaitingFinish => false;
        
        public override void OnGUI(Rect rect, ref float height)
        {
            key = UnityEditor.EditorGUI.TextField(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Key", key);
            height += UnityEditor.EditorGUIUtility.singleLineHeight;
            operation = (OperationType)UnityEditor.EditorGUI.EnumPopup(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "", operation);
            height += UnityEditor.EditorGUIUtility.singleLineHeight;
            value = UnityEditor.EditorGUI.IntField(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Value", value);
            height += UnityEditor.EditorGUIUtility.singleLineHeight;
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight * 3;
        }
    #endif

        public override IEnumerator Execute(params object[] args)
        {
            var data = args[0] as CapricornData;
            data.Operator(operation, key, value);
            yield return null;
        }
    }
}
