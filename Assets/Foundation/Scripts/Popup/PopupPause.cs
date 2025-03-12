using System;
using UnityEngine;
using UnityEngine.UI;

public class PopupPause : MonoBehaviour
{
    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private Button loadButton;

    [SerializeField]
    private Button homeButton;

    [SerializeField]
    private Button settingButton;

    public void Open(Action onClickSave, Action onClickLoad, Action onClickHome, Action onClickSetting)
    {
        saveButton.onClick.AddListener(() => 
        {
            onClickSave();
            Close();
        });

        loadButton.onClick.AddListener(() => 
        {
            onClickLoad();
            Close();
        });

        homeButton.onClick.AddListener(() => 
        {
            onClickHome();
            Close();
        });

        settingButton.onClick.AddListener(() => 
        {
            onClickSetting();
            Close();
        });

        gameObject.SetActive(true);
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}