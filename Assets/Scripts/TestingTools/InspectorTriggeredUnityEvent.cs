using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InspectorTriggeredUnityEvent : MonoBehaviour
{
    public UnityEvent GenericEvent;

    public void TriggerEvent()
    {
        GenericEvent.Invoke();
    }
}
