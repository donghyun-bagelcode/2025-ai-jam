using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NovelHistory : MonoBehaviour
{
    [SerializeField]
    private GameObject historyPopup;

    private List<(string, string, string)> history = new List<(string, string, string)>();

    public void OpenHistoryPopup()
    {
        var history = Instantiate(historyPopup, transform);
        history.GetComponent<PopupHistory>().Open(this.history);
    }

    public void AddHistory(string name, string subName, string script)
    {
        history.Add((name, subName, script));
    }

    public void Clear()
    {
        history.Clear();
    }
}
