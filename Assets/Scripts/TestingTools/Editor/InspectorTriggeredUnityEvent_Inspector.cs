using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(InspectorTriggeredUnityEvent))]
public class InspectorTriggeredUnityEvent_Inspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        InspectorTriggeredUnityEvent targetScript = (InspectorTriggeredUnityEvent)target;
        if(GUILayout.Button("Trigger Unity Event"))
        {
            targetScript.TriggerEvent();
        }    
    }
}
