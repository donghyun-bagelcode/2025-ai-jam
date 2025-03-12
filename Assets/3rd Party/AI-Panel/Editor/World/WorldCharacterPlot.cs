using System;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class WorldCharacterPlot
{
    public string Name;
    public string Plot;
    
    private TextField plot;

    // 매개변수 없는 생성자 (JSON 디시리얼라이즈용)
    public WorldCharacterPlot() {}

    public WorldCharacterPlot(TemplateContainer templateContainer, string name, string plot)
    {
        var nameLabel = templateContainer.Q<Label>("name");
        if (nameLabel == null)
        {
            Debug.LogError("Name label not found in templateContainer.");
            return;
        }
        
        nameLabel.text = name;

        this.plot = templateContainer.Q<TextField>("input");
        if (this.plot == null)
        {
            Debug.LogError("Plot TextField not found in templateContainer.");
            return;
        }
        this.plot.value = plot;

        this.Name = name;
        this.Plot = plot;
    }
}