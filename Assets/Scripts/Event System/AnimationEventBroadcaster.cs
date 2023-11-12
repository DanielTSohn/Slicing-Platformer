using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventBroadcaster : MonoBehaviour
{
    [SerializeField] private BaseGameEvent gameEvent;

    public void TriggerGameEvent()
    {
        if(gameEvent != null ) gameEvent.TriggerEvent();
    }
}