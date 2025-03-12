using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingFish : MonoBehaviour
{
    public RectTransform RectTransform
    {
        get => transform as RectTransform;
    }

    [Range(0, 1)]
    public float position;

    private float upPowerMin;
    private float upPowerMax;
    private float upSpeedMin;
    private float upSpeedMax;
    private int upWeight = 1;
    private float downPowerMin;
    private float downPowerMax;
    private float downSpeedMin;
    private float downSpeedMax;
    private int downWeight = 1;
    private float delay;

    public void Initialize(float upPowerMin, float upPowerMax, float upSpeedMin, float upSpeedMax, int upWeight,
                           float downPowerMin, float downPowerMax, float downSpeedMin, float downSpeedMax, int downWeight,
                           float delay)
    {
        this.upPowerMin = upPowerMin;
        this.upPowerMax = upPowerMax;
        this.upSpeedMin = upSpeedMin;
        this.upSpeedMax = upSpeedMax;
        this.upWeight = upWeight;
        this.downPowerMin = downPowerMin;
        this.downPowerMax = downPowerMax;
        this.downSpeedMin = downSpeedMin;
        this.downSpeedMax = downSpeedMax;
        this.downWeight = downWeight;
        this.delay = delay;
    }

    public void Run()
    {
        StartCoroutine(Move());
    }

    public void Stop()
    {
        StopAllCoroutines();
    }

    private IEnumerator Move()
    {
        while (true)
        {
            var totalWeight = upWeight + downWeight;
            var random = UnityEngine.Random.Range(0, totalWeight);

            if (random < upWeight)
            {
                // Up
                var power = UnityEngine.Random.Range(upPowerMin, upPowerMax);
                var speed = UnityEngine.Random.Range(upSpeedMin, upSpeedMax);

                var target = Mathf.Clamp01(position + power);
                var current = position;

                var time = 0f;
                while (time < 1)
                {
                    time += Time.deltaTime * speed;
                    position = Mathf.Lerp(current, target, time);
                    yield return null;
                }
            }
            else
            {
                // Down
                var power = UnityEngine.Random.Range(downPowerMin, downPowerMax);
                var speed = UnityEngine.Random.Range(downSpeedMin, downSpeedMax);

                var target = Mathf.Clamp01(position - power);
                var current = position;

                var time = 0f;
                while (time < 1)
                {
                    time += Time.deltaTime * speed;
                    position = Mathf.Lerp(current, target, time);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(delay);
        }
    }
}
