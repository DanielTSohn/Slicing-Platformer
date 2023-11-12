using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OneParameterGameEventListener<T> : BaseGameEventListener
{
    [SerializeField] protected OneParameterGameEvent<T> targetOneParemeterGameEvent;
    [SerializeField] protected UnityEvent<T> onOneParameterGameEventTriggered;
    public event Action<T> OnOneParemeterGameEventAction;

    protected override void OnEnable()
    {
        base.OnEnable();
        targetOneParemeterGameEvent.OnOneParameterEventTriggered += OnEventTriggered;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        targetOneParemeterGameEvent.OnOneParameterEventTriggered -= OnEventTriggered;
    }

    public virtual void OnEventTriggered(T parameterOne)
    {
#if UNITY_EDITOR
        if (showDebug) Debug.Log(name + " heard " + targetOneParemeterGameEvent.name + " event with parameter " + parameterOne);
#endif
        OnOneParemeterGameEventAction?.Invoke(parameterOne);
        onOneParameterGameEventTriggered.Invoke(parameterOne);
    }
}
