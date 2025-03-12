using UnityEngine;
using ProjectTools;

namespace Dunward.Capricorn
{
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Capricorn/AudioDatabase", order = 2)]
    public class AudioDatabase : ScriptableObject
    {
        public GameObject bgmPrefab;
        public GameObject sfxPrefab;
        public SerializableDictionary<string, AudioData> bgms = new SerializableDictionary<string, AudioData>();
        public SerializableDictionary<string, AudioData> sfxs = new SerializableDictionary<string, AudioData>();

#if UNITY_EDITOR
        public void Sync()
        {
            bgms.Clear();
            sfxs.Clear();

            var bgmAssets = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new [] { "Assets/Sounds/Bgms" });
            var sfxAssets = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new [] { "Assets/Sounds/Sfxs" });

            foreach (var bgmAsset in bgmAssets)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(bgmAsset);
                var bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                bgms[bgm.name] = new AudioData() { clip = bgm, maxVolume = 1 };
            }

            foreach (var sfxAsset in sfxAssets)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(sfxAsset);
                var sfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                sfxs[sfx.name] = new AudioData() { clip = sfx, maxVolume = 1 };
            }
        }
#endif
    }

    [System.Serializable]
    public class AudioData
    {
        public AudioClip clip;
        [Range(0, 1)]
        public float maxVolume = 1;
    }
}