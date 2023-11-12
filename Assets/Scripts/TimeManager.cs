using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public struct TimeMultiplier
{
    public float Multiplier;
    public float Duration;
    public GameObject Source;
    /// <summary>
    /// Data needed to sort out multiplying times together
    /// </summary>
    /// <param name="multiplier">The multiplier to apply, cannot be negative</param>
    /// <param name="duration">The duration of the multiplier, 0 means it lasts until removed manually</param>
    /// <param name="source">The game object that called the multiplier</param>
    public TimeMultiplier(float multiplier = 1, float duration = 0, GameObject source = null)
    {
        Multiplier = Mathf.Abs(multiplier);
        Duration = duration;
        Source = source;
    }
}


public class TimeManager : MonoBehaviour
{
    private float baseFixedTimeStep;
    private float baseTimeScale;
    private float totalTimeMultiplier;
    private Dictionary<TimeMultiplier, Coroutine> timeMultipliers = new();

    public static TimeManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(this);
        }
        baseFixedTimeStep = Time.fixedDeltaTime;
        baseTimeScale = Time.timeScale;
        totalTimeMultiplier = 1;
    }

    private IEnumerator CountTimeMultiplier(TimeMultiplier multiplier)
    {
        for(float time = multiplier.Duration; time > 0; time -= baseFixedTimeStep)
        {
            yield return new WaitForFixedUpdate();
        }
        RemoveMultiplier(multiplier);
    }

    public void Pause()
    {
        Time.timeScale = 0;
    }
    
    public void Resume()
    {
        Time.timeScale = baseFixedTimeStep * totalTimeMultiplier;
        Time.fixedDeltaTime = baseFixedTimeStep * totalTimeMultiplier;
    }

    public void UpdateTimeScale()
    {
        Time.timeScale = baseTimeScale * totalTimeMultiplier;
        Time.fixedDeltaTime = baseFixedTimeStep * totalTimeMultiplier;
    }

    public void AddMultiplier(TimeMultiplier timeMultiplier)
    {
        Coroutine timer = null;
        if (timeMultiplier.Duration > 0) timer = StartCoroutine(CountTimeMultiplier(timeMultiplier));
        timeMultipliers.Add(timeMultiplier, timer);
        totalTimeMultiplier *= timeMultiplier.Multiplier;
        UpdateTimeScale();
    }
    public void RemoveMultiplier(TimeMultiplier timeMultiplier)
    {
        if(timeMultipliers.TryGetValue(timeMultiplier, out Coroutine timer))
        {
            if(timer != null) StopCoroutine(timer);
            timeMultipliers.Remove(timeMultiplier);
            totalTimeMultiplier /= timeMultiplier.Multiplier;
            UpdateTimeScale();
        }
    }

    public void ClearAndResetTime()
    {
        timeMultipliers.Clear();
        Time.timeScale = baseTimeScale;
        Time.fixedDeltaTime = baseFixedTimeStep;
    }
}