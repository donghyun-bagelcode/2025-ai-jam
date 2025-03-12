using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour
{
    public Button button;
    public Text playTimeText;

    public void Initialize(bool isSave, int index, Action<int> onClick)
    {
        var saveData = PlayerPrefs.GetString($"SaveData{index}");
        var data = JsonConvert.DeserializeObject<NovelSaveData>(saveData);

        if (data == null)
        {
            button.interactable = isSave;
            playTimeText.text = "Empty";
        }
        else
        {
            playTimeText.text = TimeSpan.FromSeconds(data.playTime).ToString("hh\\:mm\\:ss");
        }
        
        button.onClick.AddListener(() => onClick(index));
    }
}