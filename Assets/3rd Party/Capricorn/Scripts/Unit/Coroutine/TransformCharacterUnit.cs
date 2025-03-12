using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Character")]
    public class TransformCharacterUnit : FadeUnit
    {
        public string character;
        public CapricornVector2 position;
        public float scale = 1;

#if UNITY_EDITOR
        protected override string info => "Transform Character";

        public override void OnGUI(Rect rect, ref float height)
        {
            base.OnGUI(rect, ref height);

            var dropDownRect = new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight);
            if (UnityEditor.EditorGUI.DropdownButton(dropDownRect, new GUIContent(character), FocusType.Passive))
            {
                ShowSearchWindow();
            }
            height += UnityEditor.EditorGUIUtility.singleLineHeight;

            position = UnityEditor.EditorGUI.Vector2Field(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight * 2), "Position", position);
            height += UnityEditor.EditorGUIUtility.singleLineHeight * 2;

            scale = Mathf.Clamp(UnityEditor.EditorGUI.FloatField(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Scale", scale), 0, float.MaxValue);
            height += UnityEditor.EditorGUIUtility.singleLineHeight;
        }

        private void ShowSearchWindow()
        {
            var datas = Resources.Load<CharacterDatabase>("CharacterDatabase").characters.Keys;

            var menu = ScriptableObject.CreateInstance<CommonSearchWindow>();
            menu.Initialize(datas.OrderBy(c => c), "Characters", OnMenuSelected);

            var current = Event.current.mousePosition;
            var position = GUIUtility.GUIToScreenPoint(current);
            UnityEditor.Experimental.GraphView.SearchWindow.Open(new UnityEditor.Experimental.GraphView.SearchWindowContext(position), menu);
        }

        private void OnMenuSelected(string select)
        {
            character = select;
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight * 4;
        }
#endif

        public override IEnumerator Execute(params object[] args)
        {
            var map = args[0] as Dictionary<string, GameObject>;
            var go = map[character];

            map[character] = go;

            var startPosition = go.transform.position;
            var startScale = go.transform.localScale;

            if (go.transform is RectTransform)
            {
                var rt = go.transform as RectTransform;
                startPosition = rt.anchoredPosition;
                startScale = rt.localScale;
            }
            
            var time = 0f;

            while (fade && time < elapsedTime)
            {
                var t = lerpCurve.Evaluate(time / elapsedTime);
                var targetPosition = Vector3.Lerp(startPosition, position, t);
                var targetScale = Vector3.Lerp(startScale, new Vector3(scale, scale, 1), t);
                time += Time.deltaTime;

                if (go.transform is RectTransform)
                {
                    var rt = go.transform as RectTransform;
                    rt.anchoredPosition = targetPosition;
                    rt.localScale = targetScale;
                }
                else
                {
                    go.transform.position = targetPosition;
                    go.transform.localScale = targetScale;
                }

                yield return null;
            }
        }
    }
}