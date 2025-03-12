using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupHistory : MonoBehaviour
{
    [SerializeField]
    private GameObject historyPrefab;

    [SerializeField]
    private Transform historyArea;

    public void Open(List<(string, string, string)> history)
    {
        foreach (var (name, subName, script) in history)
        {
            var obj = Instantiate(historyPrefab, historyArea);
            obj.GetComponent<History>().SetHistory(name, subName, script);
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
