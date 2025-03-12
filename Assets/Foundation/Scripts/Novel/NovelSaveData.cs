using System;
using System.Collections.Generic;

[Serializable]
public class NovelSaveData
{
    public string sceneName;
    public float playTime;
    public Dictionary<string, int> variables = new Dictionary<string, int>();
}