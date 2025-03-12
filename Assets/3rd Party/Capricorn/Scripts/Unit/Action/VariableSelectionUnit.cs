using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dunward.Capricorn
{
    [System.Serializable]
    public class VariableSelectionUnit : ActionUnit
    {
        public List<VariableSelectionData> selections = new List<VariableSelectionData>();

#if UNITY_EDITOR
        public override void OnGUI()
        {
            var newCount = EditorGUILayout.IntSlider("Selection Count", SelectionCount, 1, 4);
            
            if (newCount != SelectionCount)
            {
                SelectionCount = newCount;
                
                if (selections.Count < SelectionCount)
                {
                    selections.AddRange(Enumerable.Range(0, SelectionCount - selections.Count).Select(_ => new VariableSelectionData()));
                }
                else if (selections.Count > SelectionCount)
                {
                    selections = selections.Take(SelectionCount).ToList();
                }
            }

            for (int i = 0; i < SelectionCount; i++)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField($"Selection {i + 1}");
                selections[i].script = EditorGUILayout.TextArea(selections[i].script);
                selections[i].key = EditorGUILayout.TextField("Key", selections[i].key);
                EditorGUILayout.BeginHorizontal();
                selections[i].condition = (ConditionMode)EditorGUILayout.EnumPopup(selections[i].condition);
                selections[i].value = EditorGUILayout.IntField(selections[i].value, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                selections[i].conditionResult = (ConditionResult)EditorGUILayout.EnumPopup("When Mismatched", selections[i].conditionResult);
            }

            EditorGUILayout.Separator();
            selections = selections.Take(SelectionCount).ToList();
        }

        public override void InitializeOnCreate()
        {
            selections.Add(new VariableSelectionData());
        }
#endif

        public override IEnumerator Execute(params object[] args)
        {
            isComplete = false;
            
            var buttons = args[0] as List<Button>;
            var data = args[1] as CapricornData;
            var selectionDestroyAfterDelay = (float)args[2];

            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                buttons[i].onClick.AddListener(() =>
                {
                    isComplete = true;
                    nextConnection = index;
                });

                if (string.IsNullOrEmpty(selections[i].key))
                    continue;

                if (!CheckCondition(selections[i], data))
                {
                    switch (selections[i].conditionResult)
                    {
                        case ConditionResult.DisableInteraction:
                            buttons[i].interactable = false;
                            break;
                        case ConditionResult.Destroy:
                            Object.Destroy(buttons[i].gameObject);
                            break;
                    }
                }
            }

            yield return new WaitUntil(() => isComplete);
            
            yield return new WaitForSeconds(selectionDestroyAfterDelay);
        }

        private bool CheckCondition(VariableSelectionData selection, CapricornData data)
        {
            var value = data.GetValue(selection.key);
            switch (selection.condition)
            {
                case ConditionMode.Greater:
                    return value > selection.value;
                case ConditionMode.GreaterOrEqual:
                    return value >= selection.value;
                case ConditionMode.Less:
                    return value < selection.value;
                case ConditionMode.LessOrEqual:
                    return value <= selection.value;
                case ConditionMode.Equals:
                    return value == selection.value;
                case ConditionMode.NotEqual:
                    return value != selection.value;
            }
            return false;
        }
    }
}