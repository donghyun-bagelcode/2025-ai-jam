using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupSaveLoad : MonoBehaviour
{
    [SerializeField]
    private List<SaveSlot> saveSlots;

    private NovelManager novelManager;
    private bool isSaveMode = false;

    public void Open(bool isSave, NovelManager novel)
    {
        for (int i = 0; i < saveSlots.Count; i++)
        {
            saveSlots[i].Initialize(isSave, i, OnClickSaveSlot);
        }

        novelManager = novel;
        isSaveMode = isSave;
        gameObject.SetActive(true);
    }

    private void OnClickSaveSlot(int index)
    {
        if (isSaveMode)
        {
            novelManager.SaveGame(index);
        }
        else
        {
            novelManager.LoadGame(index);
        }
        Close();
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
