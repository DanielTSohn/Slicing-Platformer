using System;
using UnityEngine;
using UnityEngine.Events;

public class BaseGameEventListener : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField, Tooltip("Whether to log debug messages")] protected bool showDebug;
#endif
    [SerializeField] protected BaseGameEvent targetGameEvent;
    [SerializeField] protected UnityEvent onGameEventTriggered;
    public event Action OnGameEventAction;

    protected virtual void OnEnable()
    {
        targetGameEvent.OnGameEventTriggered += OnEventTriggered;
    }

    protected virtual void OnDisable()
    {
        targetGameEvent.OnGameEventTriggered -= OnEventTriggered;
    }

    public virtual void OnEventTriggered()
    {
#if UNITY_EDITOR
        if (showDebug) Debug.Log(name + " heard " + targetGameEvent.name + " event");
#endif
        OnGameEventAction?.Invoke();
        onGameEventTriggered.Invoke();
    }
}