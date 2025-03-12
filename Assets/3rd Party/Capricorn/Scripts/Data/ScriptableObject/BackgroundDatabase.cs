using UnityEngine;

using ProjectTools;

namespace Dunward.Capricorn
{
    [CreateAssetMenu(fileName = "BackgroundDatabase", menuName = "Capricorn/BackgroundDatabase", order = 1)]
    public class BackgroundDatabase : ScriptableObject
    {
        public GameObject backgroundPrefab;
        public SerializableDictionary<string, Sprite> backgrounds = new SerializableDictionary<string, Sprite>();

#if UNITY_EDITOR
        public void Sync()
        {
            backgrounds.Clear();
            var assets = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new [] { "Assets/Sprites/Backgrounds" });
            
            foreach (var asset in assets)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                var img = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);

                backgrounds[img.name] = img;
            }
        }
#endif
    }

    [System.Serializable]
    public class BackgroundTest
    {
        public string name;
        public Sprite sprite;
    }
}