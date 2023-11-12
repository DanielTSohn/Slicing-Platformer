using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneParameterGameEvent<T> : BaseGameEvent, IOneParameterGameEvent<T>
{
    public event Action<T> OnOneParameterEventTriggered;
    
    public virtual void TriggerEvent(T parameterOne)
    {
#if UNITY_EDITOR
        if (showDebug) Debug.Log(name + " event triggered with parameter " + parameterOne);
#endif
        OnOneParameterEventTriggered?.Invoke(parameterOne);
        TriggerEvent();
    }
}