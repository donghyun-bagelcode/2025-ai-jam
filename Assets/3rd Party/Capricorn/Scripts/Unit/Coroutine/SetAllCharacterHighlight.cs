using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Character")]
    public class SetAllCharacterHighlight : FadeUnit
    {
        public bool value;

#if UNITY_EDITOR
        protected override string info => "Set All Character Highlight";

        public override void OnGUI(Rect rect, ref float height)
        {
            base.OnGUI(rect, ref height);
            
            value = UnityEditor.EditorGUI.Toggle(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Value", value);
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight;
        }
#endif

        private Dictionary<GameObject, Color> targetColors = new Dictionary<GameObject, Color>();

        public override IEnumerator Execute(params object[] args)
        {
            var time = 0f;
            var map = args[0] as Dictionary<string, GameObject>;

            if (fade)
            {
                foreach (var pair in map)
                {
                    var image = pair.Value.GetComponent<SpriteRenderer>();
                    var sprite = pair.Value.GetComponent<UnityEngine.UI.Image>();

                    if (image != null)
                    {
                        targetColors.Add(pair.Value, image.color);
                    }
                    else if (sprite != null)
                    {
                        targetColors.Add(pair.Value, sprite.color);
                    }
                }
            }

            while (fade && time < elapsedTime)
            {
                time += Time.deltaTime;

                foreach (var pair in map)
                {
                    var targetColor = Color.Lerp(targetColors[pair.Value], value ? Color.white : Color.gray, lerpCurve.Evaluate(time / elapsedTime));
                    var image = pair.Value.GetComponent<SpriteRenderer>();
                    var sprite = pair.Value.GetComponent<UnityEngine.UI.Image>();

                    if (image != null)
                    {
                        image.color = targetColor;
                    }
                    else if (sprite != null)
                    {
                        sprite.color = targetColor;
                    }
                }

                yield return null;
            }

            foreach (var pair in map)
            {
                var image = pair.Value.GetComponent<SpriteRenderer>();
                var sprite = pair.Value.GetComponent<UnityEngine.UI.Image>();

                if (image != null)
                {
                    image.color = value ? Color.white : Color.gray;
                }
                else if (sprite != null)
                {
                    sprite.color = value ? Color.white : Color.gray;
                }
            }
            
            yield return null;
        }
    }
}