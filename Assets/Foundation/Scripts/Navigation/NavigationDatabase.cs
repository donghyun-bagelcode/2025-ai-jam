using UnityEngine;

using ProjectTools;

[CreateAssetMenu(fileName = "NavigationDatabase", menuName = "NovelFoundation/NavigationDatabase", order = 1)]
public class NavigationDatabase : ScriptableObject
{
    public SerializableDictionary<string, Sprite> navigations = new SerializableDictionary<string, Sprite>();
}
