using UnityEngine;

using ProjectTools;

namespace Dunward.Capricorn
{
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Capricorn/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        public SerializableDictionary<string, GameObject> characters = new SerializableDictionary<string, GameObject>();

#if UNITY_EDITOR
        public void Sync()
        {
            var assets = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new [] { "Assets/Sprites/Characters" });

            foreach (var asset in assets)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                var img = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (characters.ContainsKey(img.name)) continue;

                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Foundation/Prefabs/Character/Character Template.prefab");
                obj.name = img.name;
                obj.GetComponent<UnityEngine.UI.Image>().sprite = img;
                var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, $"Assets/Foundation/Prefabs/Character/{img.name}.prefab");

                characters[img.name] = prefab;

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
    }
}