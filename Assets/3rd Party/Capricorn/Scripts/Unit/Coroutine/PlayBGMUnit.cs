using System.Collections;
using System.Linq;
using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Sound")]
    public class PlayBGMUnit : FadeUnit
    {
        public string bgm;

#if UNITY_EDITOR
        protected override string info => "Play BGM";

        public override void OnGUI(Rect rect, ref float height)
        {
            base.OnGUI(rect, ref height);

            var dropDownRect = new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight);
            if (UnityEditor.EditorGUI.DropdownButton(dropDownRect, new GUIContent(bgm), FocusType.Passive))
            {
                ShowSearchWindow();
            }

            height += UnityEditor.EditorGUIUtility.singleLineHeight;
        }

        private void ShowSearchWindow()
        {
            var datas = Resources.Load<AudioDatabase>("AudioDatabase").bgms.Keys;

            var menu = ScriptableObject.CreateInstance<CommonSearchWindow>();
            menu.Initialize(datas.OrderBy(c => c), "BGM", OnMenuSelected);

            var current = Event.current.mousePosition;
            var position = GUIUtility.GUIToScreenPoint(current);
            UnityEditor.Experimental.GraphView.SearchWindow.Open(new UnityEditor.Experimental.GraphView.SearchWindowContext(position), menu);
        }

        private void OnMenuSelected(string select)
        {
            bgm = select;
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight;
        }
#endif

        public override IEnumerator Execute(params object[] args)
        {
            var info = Resources.Load<AudioDatabase>("AudioDatabase");
            var goRef = args[0] as Ref<GameObject>;
            
            Object.Destroy(goRef.Value);

            var go = Object.Instantiate(info.bgmPrefab);
            var audio = go.GetComponent<AudioSource>();

            go.name = bgm;

            var target = info.bgms[bgm];

            audio.clip = target.clip;
            audio.Play();

            var time = 0f;

            while (fade && time < elapsedTime)
            {
                var targetVolume = Mathf.Lerp(0, target.maxVolume, lerpCurve.Evaluate(time / elapsedTime));
                time += Time.deltaTime;

                audio.volume = targetVolume;
                yield return null;
            }

            audio.volume = target.maxVolume;

            goRef.Value = go;
        }
    }
}