using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BaseGameEvent", menuName = "Base Game Event", order = 0)]
public class BaseGameEvent : ScriptableObject, IGameEvent
{
#if UNITY_EDITOR
    [Tooltip("Whether to show debug messages in the console"), SerializeField] protected bool showDebug = true;
#endif
    public event Action OnGameEventTriggered;

    public virtual void TriggerEvent()
    {
#if UNITY_EDITOR
        if (showDebug) Debug.Log(name + " event triggered");
#endif
        OnGameEventTriggered?.Invoke();
    }
}