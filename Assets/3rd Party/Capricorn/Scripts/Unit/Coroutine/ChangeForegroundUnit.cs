using System.Collections;
using System.Linq;
using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Image")]
    public class ChangeForegroundUnit : FadeUnit
    {
        public string backgroundImage;
        public CapricornVector2 position;
        public float scale = 1;

#if UNITY_EDITOR
        protected override string info => "Change Foreground";

        public override void OnGUI(Rect rect, ref float height)
        {
            base.OnGUI(rect, ref height);

            var dropDownRect = new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight);
            if (UnityEditor.EditorGUI.DropdownButton(dropDownRect, new GUIContent(backgroundImage), FocusType.Passive))
            {
                ShowSearchWindow();
            }

            height += UnityEditor.EditorGUIUtility.singleLineHeight;

            position = UnityEditor.EditorGUI.Vector2Field(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Position", position);

            height += UnityEditor.EditorGUIUtility.singleLineHeight * 2;

            scale = Mathf.Clamp(UnityEditor.EditorGUI.FloatField(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Scale", scale), 0f, float.MaxValue);

            height += UnityEditor.EditorGUIUtility.singleLineHeight;
        }

        private void ShowSearchWindow()
        {
            var datas = Resources.Load<BackgroundDatabase>("BackgroundDatabase").backgrounds.Keys;

            var menu = ScriptableObject.CreateInstance<CommonSearchWindow>();
            menu.Initialize(datas.OrderBy(c => c), "Backgrounds", OnMenuSelected);

            var current = Event.current.mousePosition;
            var position = GUIUtility.GUIToScreenPoint(current);
            UnityEditor.Experimental.GraphView.SearchWindow.Open(new UnityEditor.Experimental.GraphView.SearchWindowContext(position), menu);
        }

        private void OnMenuSelected(string select)
        {
            backgroundImage = select;
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight * 4;
        }
#endif

        public override IEnumerator Execute(params object[] args)
        {
            var database = Resources.Load<BackgroundDatabase>("BackgroundDatabase");
            var background = database.backgrounds[backgroundImage];
            var prefab = database.backgroundPrefab;

            var parent = args[0] as Transform;
            
            var go = Object.Instantiate(prefab, parent);
            go.name = backgroundImage;

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

            if (image != null)
            {
                image.sprite = background;
            }
            else if (sprite != null)
            {
                sprite.sprite = background;
            }

            while (fade && time < elapsedTime)
            {
                var targetColor = new Color(1, 1, 1, lerpCurve.Evaluate(time / elapsedTime));
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

            if (image != null)
            {
                image.color = Color.white;
            }
            else if (sprite != null)
            {
                sprite.color = Color.white;
            }

            var goRef = args[1] as Ref<GameObject>;
            Object.Destroy(goRef.Value);
            goRef.Value = go;
        }
    }
}