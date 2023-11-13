using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TimeMultiplier
{
    public float Multiplier;
    public float Duration;
    public Action Callback;
    public GameObject Source;
    public Coroutine Timer;
    /// <summary>
    /// Data needed to sort out multiplying times together
    /// </summary>
    /// <param name="multiplier">The multiplier to apply, cannot be negative</param>
    /// <param name="duration">The duration of the multiplier, 0 means it lasts until removed manually</param>
    /// <param name="source">The game object that called the multiplier</param>
    public TimeMultiplier(float multiplier = 1, float duration = 0, Action callback = null, GameObject source = null, Coroutine timer = null)
    {
        Multiplier = Mathf.Abs(multiplier);
        Duration = duration;
        Callback = callback;
        Source = source;
        Timer = timer;
    }
}

public class TimeManager : MonoBehaviour
{
    private float baseFixedTimeStep;
    private float baseTimeScale;
    private Dictionary<UInt16, TimeMultiplier> timeMultipliers = new();
    private Queue<UInt16> freeIDs = new();
    private UInt16 currentMaxID = 0;

    public float TotalTimeMultiplier { get; private set; }
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
        TotalTimeMultiplier = 1;
    }

    private IEnumerator CountTimeMultiplier(UInt16 id, float duration)
    {
        for(float time = duration; time > 0; time -= baseFixedTimeStep)
        {
            yield return new WaitForFixedUpdate();
        }
        RemoveMultiplier(id);
    }

    private UInt16 GenerateID()
    {
        if (freeIDs.TryDequeue(out var oldID)) return oldID;
        else return ++currentMaxID;
    }    

    public void Pause()
    {
        Time.timeScale = 0;
    }
    
    public void Resume()
    {
        Time.timeScale = baseFixedTimeStep * TotalTimeMultiplier;
        Time.fixedDeltaTime = baseFixedTimeStep * TotalTimeMultiplier;
    }

    public void UpdateTimeScale()
    {
        Time.timeScale = baseTimeScale * TotalTimeMultiplier;
        Time.fixedDeltaTime = baseFixedTimeStep * TotalTimeMultiplier;
    }

    public UInt16 AddMultiplier(TimeMultiplier timeMultiplier)
    {
        UInt16 id = GenerateID();
        if (timeMultiplier.Duration > 0) timeMultiplier.Timer = StartCoroutine(CountTimeMultiplier(id, timeMultiplier.Duration));
        timeMultipliers.Add(id, timeMultiplier);
        TotalTimeMultiplier *= timeMultiplier.Multiplier;
        UpdateTimeScale();
        Debug.Log(id);
        return id;
    }

    public void RemoveMultiplier(UInt16 id)
    {
        if(timeMultipliers.TryGetValue(id, out TimeMultiplier timeMultiplier))
        {
            if(timeMultiplier.Timer != null) StopCoroutine(timeMultiplier.Timer);
            timeMultiplier.Callback?.Invoke();
            TotalTimeMultiplier /= timeMultiplier.Multiplier;
            UpdateTimeScale();

            timeMultipliers.Remove(id);
            freeIDs.Enqueue(id);
        }
    }

    public void ClearAndResetTime()
    {
        timeMultipliers.Clear();
        Time.timeScale = baseTimeScale;
        Time.fixedDeltaTime = baseFixedTimeStep;
    }
}