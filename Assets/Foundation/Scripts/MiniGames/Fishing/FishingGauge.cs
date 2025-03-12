using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class FishingGauge : MonoBehaviour
{
    [SerializeField]
    private Image image;

    private void Update()
    {
        image.material.SetFloat("_Value", image.fillAmount);
    }
}
