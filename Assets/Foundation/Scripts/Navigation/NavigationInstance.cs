using System.Collections;
using System.Collections.Generic;
using Dunward.Capricorn;
using UnityEngine;
using UnityEngine.UI;

public class NavigationInstance : MonoBehaviour
{
    [SerializeField]
    private Image icon;

    [SerializeField]
    private Text valueText;

    public void Initialize(Sprite iconImage, Ref<int> value)
    {
        icon.sprite = iconImage;

        value.onValueChanged += (v) =>
        {
            valueText.text = v.ToString();
        };
    }
}
