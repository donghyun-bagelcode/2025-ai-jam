using UnityEngine;
using System.Collections.Generic;

using ProjectTools;

namespace Dunward.Capricorn
{
    [DisallowMultipleComponent]
    public class CapricornData : MonoBehaviour
    {
        public Dictionary<string, GameObject> characters = new Dictionary<string, GameObject>();

        public SerializableDictionary<string, Ref<int>> variables = new SerializableDictionary<string, Ref<int>>();

        public int GetValue(string key)
        {
            if (!variables.ContainsKey(key))
                variables[key] = new Ref<int>(0);

            return variables[key];
        }
    }
}