using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishingCatcher : MonoBehaviour
{
    public RectTransform RectTransform
    {
        get => transform as RectTransform;
    }

    [Range(0, 1)]
    public float value;

    private float power = 1.5f;
    private float acceleration = 0.15f;
    private float currentSpeed = 0f;
    private bool isBouncing = false;

    private bool isRunning = true;

    public void Stop()
    {
        isRunning = false;
    }

    public void FixedUpdate()
    {
        if (!isRunning) return;

        if (Input.GetMouseButton(0))
        {
            value = Mathf.Clamp01(value + power * Time.fixedDeltaTime);
            currentSpeed = power * Time.fixedDeltaTime;
            isBouncing = false;
        }
        else
        {
            if (value <= 0 && !isBouncing)
            {
                currentSpeed = -currentSpeed * 2f;
                isBouncing = true;
            }
            else
            {
                if (currentSpeed < 0)
                {
                    isBouncing = false;
                }

                currentSpeed += acceleration;
                value = Mathf.Clamp01(value - (power + currentSpeed) * Time.fixedDeltaTime);
            }
        }
    }
}
