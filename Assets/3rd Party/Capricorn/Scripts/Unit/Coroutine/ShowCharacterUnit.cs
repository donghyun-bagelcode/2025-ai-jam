using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Character")]
    public class ShowCharacterUnit : FadeUnit
    {
        public string character;
        public CapricornVector2 position;
        public float scale = 1;

#if UNITY_EDITOR
        protected override string info => "Show Character";

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
            var characters = Resources.Load<CharacterDatabase>("CharacterDatabase").characters;
            var prefab = characters[character];
            var parent = args[0] as Transform;

            var go = Object.Instantiate(prefab, parent);
            go.name = character;

            var map = args[1] as Dictionary<string, GameObject>;

            if (map.ContainsKey(character))
            {
                Object.Destroy(map[character]);
            }

            foreach (var pair in map)
            {
                if (pair.Key != character)
                {
                    var temp1 = pair.Value.GetComponent<SpriteRenderer>();
                    var temp2 = pair.Value.GetComponent<UnityEngine.UI.Image>();

                    if (temp1 != null)
                    {
                        temp1.color = Color.gray;
                    }
                    else if (temp2 != null)
                    {
                        temp2.color = Color.gray;
                    }
                }
            }

            map[character] = go;

            if (go.transform is RectTransform)
            {
                var rt = go.transform as RectTransform;
                rt.anchoredPosition = position;
                rt.localScale = new Vector3(scale, scale, 1);
            }
            else
            {
                go.transform.position = position;
                go.transform.localScale = new Vector3(scale, scale, 1);
            }
            
            var time = 0f;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            var sprite = go.GetComponent<SpriteRenderer>();

            while (fade && time < elapsedTime)
            {
                var targetColor = Color.Lerp(Color.black, Color.white, lerpCurve.Evaluate(time / elapsedTime));
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
        }
    }
}