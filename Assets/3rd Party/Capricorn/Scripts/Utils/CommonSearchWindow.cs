#if UNITY_EDITOR
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace Dunward.Capricorn
{
    public class CommonSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private Action<string> onSelectEntity;

        private IEnumerable<string> datas;
        private string windowName;

        public void Initialize(IEnumerable<string> datas, string windowName, Action<string> onSelectEntity)
        {
            this.datas = datas;
            this.windowName = windowName;
            this.onSelectEntity = onSelectEntity;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var emptyTexture = new Texture2D(1, 1);
            emptyTexture.SetPixel(0, 0, new Color(0, 0, 0, 0));
            emptyTexture.Apply();

            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent(windowName)),
            };

            foreach (var data in datas)
            {
                entries.Add(new SearchTreeEntry(new GUIContent(data, emptyTexture))
                {
                    level = 1,
                    userData = data,
                });
            }
            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            onSelectEntity?.Invoke(SearchTreeEntry.userData as string);
            return true;
        }
    }
}
#endif