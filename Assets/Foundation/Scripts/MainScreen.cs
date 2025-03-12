using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MainScreen : MonoBehaviour
{
    [SerializeField]
    private ScriptMachine mainFlow;

    [SerializeField]
    private GameObject novel;

    private void Awake()
    {
        novel.GetComponent<NovelManager>().Initialize();
    }

    public void StartNewGame()
    {
        gameObject.SetActive(false);
        novel.SetActive(true);
        mainFlow.StopAllCoroutines();
        CustomEvent.Trigger(mainFlow.gameObject, "OnStartNewGame");
    }
}
