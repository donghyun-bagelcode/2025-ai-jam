using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Character")]
    public class DeleteCharacterUnit : FadeUnit
    {
        public string character;

#if UNITY_EDITOR
        protected override string info => "Delete Character";

        public override void OnGUI(Rect rect, ref float height)
        {
            base.OnGUI(rect, ref height);

            var dropDownRect = new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight);
            if (UnityEditor.EditorGUI.DropdownButton(dropDownRect, new GUIContent(character), FocusType.Passive))
            {
                ShowSearchWindow();
            }

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
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight;
        }
#endif

        public override IEnumerator Execute(params object[] args)
        {
            var time = 0f;
            var map = args[0] as Dictionary<string, GameObject>;
            var go = map[character];
            var image = go.GetComponent<UnityEngine.UI.Image>();
            var sprite = go.GetComponent<SpriteRenderer>();

            while (fade && time < elapsedTime)
            {
                var targetColor = Color.Lerp(Color.white, Color.black, lerpCurve.Evaluate(time / elapsedTime));
                time += Time.deltaTime;

                if (image != null)
                {
                    image.color = targetColor;
                }
                else if (sprite != null)
                {
                    sprite.color = targetColor;
                }

                yield return null;
            }
            
            Object.Destroy(go);
            map.Remove(character);
        }
    }
}