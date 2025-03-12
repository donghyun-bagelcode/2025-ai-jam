using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class FishingManager : MiniGameBehaviour
{
    [SerializeField]
    private FishingCatcher catcher;

    [SerializeField]
    private FishingFish fish;

    private float catcherSize;

    [SerializeField]
    private RectTransform spot;

    [SerializeField]
    private Image gauge;
    [SerializeField]
    private Text resultText;

    private float catchAmount = 0.35f;
    private float catchStrength = 0.003f;
    private float fishStrength = 0.003f;

    private bool isRunning = true;
    private bool isSuccess = false;

    public void Initialize(float catcherSize, float catchStrength, float fishStrength,
                            float fishUpPowerMin, float fishUpPowerMax, float fishUpSpeedMin, float fishUpSpeedMax, int fishUpWeight,
                            float fishDownPowerMin, float fishDownPowerMax, float fishDownSpeedMin, float fishDownSpeedMax, int fishDownWeight,
                            float delay)
    {
        catcher.RectTransform.sizeDelta = new Vector2(catcher.RectTransform.sizeDelta.x, catcherSize);
        this.catcherSize = catcherSize;
        this.catchStrength *= catchStrength;
        this.fishStrength *= fishStrength;
        fish.Initialize(fishUpPowerMin, fishUpPowerMax, fishUpSpeedMin, fishUpSpeedMax, fishUpWeight,
                        fishDownPowerMin, fishDownPowerMax, fishDownSpeedMin, fishDownSpeedMax, fishDownWeight,
                        delay);
        fish.Run();
    }

    public bool GetResult()
    {
        return isSuccess;
    }
    
    private void FixedUpdate()
    {
        if (!isRunning) return;
        UpdateElementPosition();

        if (catcher.RectTransform.anchoredPosition.y - catcherSize / 2 < fish.RectTransform.anchoredPosition.y &&
            catcher.RectTransform.anchoredPosition.y + catcherSize / 2 > fish.RectTransform.anchoredPosition.y)
        {
            catchAmount = Mathf.Clamp01(catchAmount + catchStrength);
        }
        else
        {
            catchAmount = Mathf.Clamp01(catchAmount - fishStrength);
        }
        
        gauge.fillAmount = catchAmount;
        CheckFishingResult();
    }

    private void UpdateElementPosition()
    {
        var catcherArea = spot.rect.height - catcherSize;
        var fishArea = spot.rect.height - fish.RectTransform.rect.height;

        catcher.RectTransform.anchoredPosition = new Vector2(0, -catcherArea / 2 + catcherArea * catcher.value);
        fish.RectTransform.anchoredPosition = new Vector2(0, -fishArea / 2 + fishArea * fish.position);
    }

    private void CheckFishingResult()
    {
        if (catchAmount == 0)
        {
            // Fail
            isRunning = false;
            isSuccess = false;
            resultText.text = "실패";
            fish.Stop();
            catcher.Stop();
            Invoke(nameof(Exit), 1f);
        }
        else if (catchAmount == 1)
        {
            // Success
            isRunning = false;
            isSuccess = true;
            resultText.text = "성공";
            fish.Stop();
            catcher.Stop();
            Invoke(nameof(Exit), 1f);
        }
    }
}
