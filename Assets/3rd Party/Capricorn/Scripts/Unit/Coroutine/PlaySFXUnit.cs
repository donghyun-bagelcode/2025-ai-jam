using System.Collections;
using System.Linq;
using UnityEngine;

namespace Dunward.Capricorn
{
    [System.Serializable]
    [UnitDirectory("Sound")]
    public class PlaySFXUnit : CoroutineUnit
    {
        public string sfx;

#if UNITY_EDITOR
        protected override string info => "Play SFX";
        protected override bool supportWaitingFinish => false;

        public override void OnGUI(Rect rect, ref float height)
        {
            base.OnGUI(rect, ref height);

            var dropDownRect = new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight);
            if (UnityEditor.EditorGUI.DropdownButton(dropDownRect, new GUIContent(sfx), FocusType.Passive))
            {
                ShowDropDown(dropDownRect);
            }

            height += UnityEditor.EditorGUIUtility.singleLineHeight;
        }

        private void ShowDropDown(Rect rect)
        {
            var datas = Resources.Load<AudioDatabase>("AudioDatabase").sfxs.Keys;

            var menu = ScriptableObject.CreateInstance<CommonSearchWindow>();
            menu.Initialize(datas.OrderBy(c => c), "SFX", OnMenuSelected);

            var current = Event.current.mousePosition;
            var position = GUIUtility.GUIToScreenPoint(current);
            UnityEditor.Experimental.GraphView.SearchWindow.Open(new UnityEditor.Experimental.GraphView.SearchWindowContext(position), menu);
        }

        private void OnMenuSelected(string select)
        {
            sfx = select;
        }

        public override float GetHeight()
        {
            return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight;
        }
#endif

        public override IEnumerator Execute(params object[] args)
        {
            var info = Resources.Load<AudioDatabase>("AudioDatabase");

            var go = Object.Instantiate(info.sfxPrefab);
            var audio = go.GetComponent<AudioSource>();

            go.name = sfx;

            var target = info.sfxs[sfx];

            audio.clip = target.clip;
            audio.volume = target.maxVolume;

            audio.Play();
            
            Object.Destroy(go, target.clip.length);

            return null;
        }
    }
}