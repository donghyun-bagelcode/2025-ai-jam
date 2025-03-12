using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField]
    private NovelManager novelManager;

    [SerializeField]
    private GameObject saveLoadPopup;

    [SerializeField]
    private GameObject pausePopup;

    public void OpenSaveLoadPopup(bool isSave)
    {
        var popup = Instantiate(saveLoadPopup, transform);
        popup.GetComponent<PopupSaveLoad>().Open(isSave, novelManager);
    }
    
    public void OpenPausePopup()
    {
        var popup = Instantiate(pausePopup, transform);
        popup.GetComponent<PopupPause>().Open(
            () => OpenSaveLoadPopup(true),
            () => OpenSaveLoadPopup(false),
            () => CustomEvent.Trigger(ScreenFlow.Instance.gameObject, "OnChangeMainScreen"),
            () => Debug.LogError("OnClickSetting")
        );
    }
}
