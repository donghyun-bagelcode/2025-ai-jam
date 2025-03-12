using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class D20Manager : MiniGameBehaviour, IResult<bool>
{
    [SerializeField]
    private Button dice;

    [SerializeField]
    private Text difficultText;

    [SerializeField]
    private Sprite[] diceFaces;

    [SerializeField]
    private Image diceResult;

    private bool isSuccess = false;

    private int difficult = 10;

    public void SetDifficult(int value)
    {
        difficult = value;
        difficultText.text = difficult.ToString();
    }

    public bool GetResult()
    {
        return isSuccess;
    }

    public void Roll()
    {
        dice.interactable = false;
        GetComponent<Animator>().SetTrigger("Roll");
        StartCoroutine(RollCoroutine());
    }

    private IEnumerator RollCoroutine()
    {
        yield return new WaitForSeconds(1.34f);

        // 1 ~ 20
        var result = Random.Range(1, 21);
        diceResult.sprite = diceFaces[result - 1];
        
        switch (result)
        {
            case 1:
                GetComponent<Animator>().SetInteger("Result", 0);
                isSuccess = false;
                break;
            
            case 20:
                GetComponent<Animator>().SetInteger("Result", 3);
                isSuccess = true;
                break;

            case int n when n < difficult:
                GetComponent<Animator>().SetInteger("Result", 1);
                isSuccess = false;
                break;

            case int n when n >= difficult:
                GetComponent<Animator>().SetInteger("Result", 2);
                isSuccess = true;
                break;
        }
        
        yield return new WaitForSeconds(1f);

        Exit();
    }
}
