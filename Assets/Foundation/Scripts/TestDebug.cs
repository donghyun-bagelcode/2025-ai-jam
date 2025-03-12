using System.Collections;
using System.Collections.Generic;
using Dunward.Capricorn;
using UnityEngine;

public class TestDebug : MonoBehaviour
{
    public TextAsset textAsset;

    private void Start()
    {
        GetComponent<NovelManager>().Initialize();
        StartCoroutine(GetComponent<NovelManager>().Load(textAsset));
        Debug.LogError("LOAD");
    }
}
