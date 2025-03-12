using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class History : MonoBehaviour
{
    [SerializeField]
    private Text nameText;

    [SerializeField]
    private Text subNameText;

    [SerializeField]
    private Text scriptText;

    public void SetHistory(string name, string subName, string script)
    {
        if (nameText != null) nameText.text = name;
        if (subNameText != null) subNameText.text = subName;
        if (scriptText != null) scriptText.text = script;
    }
}
