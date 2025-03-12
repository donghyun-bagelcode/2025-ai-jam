using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Numpad : MiniGameBehaviour
{
    [SerializeField]
    private Text displayText;

    [SerializeField]
    private List<NumpadButton> numpadButtons;

    [SerializeField]
    private NumpadButton deleteButton;

    [SerializeField]
    private NumpadButton enterButton;

    private List<string> inputs = new List<string>();

    private string password;
    private bool multipleTry = false;
    private bool isSuccess = false;

    private bool isFinished = false;
    
    public void Initialize(string password, bool multipleTry)
    {
        foreach (var numpadButton in numpadButtons)
        {
            numpadButton.Initialize();
            numpadButton.onNumberInput += OnNumberInput;
        }

        deleteButton.Initialize();
        deleteButton.onDelete += OnDelete;

        enterButton.Initialize();
        enterButton.onEnter += OnEnter;

        this.password = password;
        this.multipleTry = multipleTry;
    }

    public bool GetResult()
    {
        return isSuccess;
    }

    private void OnEnter()
    {
        if (isFinished) return;

        if (GetDisplayString() == password)
        {
            GetComponent<Animator>().SetTrigger("Success");
            isFinished = true;
            isSuccess = true;
            Invoke(nameof(Exit), 1f);
        }
        else
        {
            GetComponent<Animator>().SetTrigger("Fail");
            if (!multipleTry)
            {
                isFinished = true;
                isSuccess = false;
                Invoke(nameof(Exit), 1f);
            }
        }
    }

    private void OnDelete()
    {
        if (isFinished) return;
        if (inputs.Count == 0) return;

        inputs.RemoveAt(inputs.Count - 1);
        UpdateDisplay();
    }

    private void OnNumberInput(string number)
    {
        if (isFinished) return;
        if (inputs.Count >= 4) return;

        inputs.Add(number);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayText.text = GetDisplayString();
    }

    private string GetDisplayString()
    {
        return string.Join("", inputs);
    }
}
