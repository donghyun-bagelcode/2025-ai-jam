using System.Collections;

using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Variable")]
    public class SetRandomVariableUnit : CoroutineUnit
    {
        public string key;
        public OperationType operation;
        public int min;
        public int max;

    #if UNITY_EDITOR
        protected override string info => "Set random variable";
        protected override bool supportWaitingFinish => false;

        public override void OnGUI(Rect rect, ref float height)
        {
            key = UnityEditor.EditorGUI.TextField(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Key", key);
            height += UnityEditor.EditorGUIUtility.singleLineHeight;
            operation = (OperationType)UnityEditor.EditorGUI.EnumPopup(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "", operation);
            height += UnityEditor.EditorGUIUtility.singleLineHeight;
            UnityEditor.EditorGUI.LabelField(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Value Range");
            height += UnityEditor.EditorGUIUtility.singleLineHeight;

            var width = rect.width / 3;
            var style = new GUIStyle(UnityEditor.EditorStyles.label);
            style.alignment = TextAnchor.MiddleCenter;

            min = UnityEditor.EditorGUI.IntField(new Rect(rect.x, rect.y + height, width, UnityEditor.EditorGUIUtility.singleLineHeight), "", min);
            UnityEditor.EditorGUI.LabelField(new Rect(rect.x + width, rect.y + height, width, UnityEditor.EditorGUIUtility.singleLineHeight), "~", style);
            max = UnityEditor.EditorGUI.IntField(new Rect(rect.x + width * 2, rect.y + height, width, UnityEditor.EditorGUIUtility.singleLineHeight), "", max);
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight * 4;
        }
    #endif

        public override IEnumerator Execute(params object[] args)
        {
            var data = args[0] as CapricornData;
            var value = Random.Range(min, max + 1);
            data.Operator(operation, key, value);
            yield return null;
        }
    }
}
