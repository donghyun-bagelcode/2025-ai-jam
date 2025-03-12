#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;

namespace Dunward.Capricorn
{
    public class CoroutineSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private ReorderableList list;

        public void Initialize(ReorderableList list)
        {
            this.list = list;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var emptyTexture = new Texture2D(1, 1);
            emptyTexture.SetPixel(0, 0, new Color(0, 0, 0, 0));
            emptyTexture.Apply();

            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Add Coroutines")),
            };

            var assembly = Assembly.GetAssembly(typeof(CoroutineUnit));
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(CoroutineUnit)))
                .ToList();

            var groupedTypes = types.Select(t => new
                                    {
                                        Type = t,
                                        Directory = t.GetCustomAttribute<UnitDirectory>()?.Directory ?? "",
                                    })
                                    .GroupBy(t => t.Directory)
                                    .OrderBy(t => t.Key);

            var folderEntries = new List<SearchTreeEntry>();
            var ungroupedEntries = new List<SearchTreeEntry>();

            foreach (var group in groupedTypes)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    foreach (var type in group)
                    {
                        ungroupedEntries.Add(new SearchTreeEntry(new GUIContent(type.Type.Name, emptyTexture))
                        {
                            level = 1,
                            userData = type.Type,
                        });
                    }
                }
                else
                {
                    var pathParts = group.Key.Split('/');
                    AddGroupEntries(folderEntries, pathParts, 1);

                    foreach (var type in group)
                    {
                        folderEntries.Add(new SearchTreeEntry(new GUIContent(type.Type.Name, emptyTexture))
                        {
                            level = pathParts.Length + 1,
                            userData = type.Type,
                        });
                    }
                }
            }

            entries.AddRange(folderEntries);
            entries.AddRange(ungroupedEntries);

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            list.list.Add((CoroutineUnit)System.Activator.CreateInstance(SearchTreeEntry.userData as System.Type));
            return true;
        }

        private void AddGroupEntries(List<SearchTreeEntry> entries, string[] pathParts, int level)
        {
            string currentPath = "";
            foreach (var part in pathParts)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                if (!entries.Any(e => e.name == currentPath))
                {
                    entries.Add(new SearchTreeGroupEntry(new GUIContent(part))
                    {
                        level = level
                    });
                }
                level++;
            }
    }
    }
}
#endif