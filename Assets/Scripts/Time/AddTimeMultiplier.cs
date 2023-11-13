using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AddTimeMultiplier : MonoBehaviour
{
    [SerializeField, Range(0, 1), Tooltip("The time multiplier to add to the total")]
    private float multiplier;
    [SerializeField, Min(0), Tooltip("The time the multiplier lasts, 0 lasts until removed")]
    private float duration;
    [SerializeField]
    private UnityEvent onMultiplierFinishedUnityEvent;
    [SerializeField]
    private BaseGameEvent onMultiplierFinishedGameEvent;

    private UInt32 multiplierID;
    private bool assignedID = false;

    public void AddMultiplier()
    {
        if (!assignedID) assignedID = true;
        else RemoveMultiplier();
        multiplierID = TimeManager.Instance.AddMultiplier(new TimeMultiplier(multiplier, duration, () => 
        { onMultiplierFinishedUnityEvent.Invoke(); if(onMultiplierFinishedGameEvent != null) onMultiplierFinishedGameEvent.TriggerEvent(); }, gameObject));
    }

    public void RemoveMultiplier()
    {
        if(assignedID) TimeManager.Instance.RemoveMultiplier(multiplierID);
    }
}