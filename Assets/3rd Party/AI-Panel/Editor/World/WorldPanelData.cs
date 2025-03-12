using System.Collections.Generic;
using System;

[Serializable]  // JSON 직렬화를 위해 Serializable 추가
public class WorldPanelData
{
    public string genres;
    public string keywords;
    public string logline;
    public List<WorldCharacter> mainCharacters;
    public List<WorldCharacterPlot> mainCharacterPlots;
    public List<WorldCharacter> subCharacters;
    public List<WorldCharacterPlot> subCharacterPlots;
}