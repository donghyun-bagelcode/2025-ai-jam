using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NumpadButton : MonoBehaviour
{
    public Action<string> onNumberInput;
    public Action onDelete;
    public Action onEnter;

    [SerializeField]
    private List<KeyCode> keyCode;

    [SerializeField]
    private string keyCodeString;

    [SerializeField]
    private bool isDelete;

    [SerializeField]
    private bool isEnter;

    public void Initialize()
    {
        if (keyCode.Count == 2)
            GetComponent<Button>().onClick.AddListener(() => onNumberInput?.Invoke(keyCodeString));

        if (isDelete)
            GetComponent<Button>().onClick.AddListener(() => onDelete?.Invoke());

        if (isEnter)
            GetComponent<Button>().onClick.AddListener(() => onEnter?.Invoke());
    }

    private void Update()
    {
        CheckNumberInput();

        if (isDelete)
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                onDelete?.Invoke();
            }
        }

        if (isEnter)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                onEnter?.Invoke();
            }
        }
    }

    private void CheckNumberInput()
    {
        if (keyCode.Count == 0) return;

        if (Input.GetKeyDown(keyCode[0]) || Input.GetKeyDown(keyCode[1]))
        {
            onNumberInput?.Invoke(keyCodeString);
        }
    }
}
