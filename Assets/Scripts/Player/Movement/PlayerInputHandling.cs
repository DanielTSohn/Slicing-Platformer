using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputHandling : MonoBehaviour
{
    /// <summary>
    /// Called when player inputs a move value, movement passed as Vector2 <br/>
    /// x = x axis (left-right), y = z axis (forward-back)
    /// </summary>
    public event Action<Vector2> MovementPerformedAction;
    [Header("Player Movement")]
    [SerializeField, Tooltip("Called when player inputs a move value, movement passed as Vector2\nx = x axis (left-right), y = z axis (forward-back)")] 
    private UnityEvent<Vector2> movementPerformedEvent;
    /// <summary>
    /// Called when the player has no movement inputs
    /// </summary>
    public event Action MovementStoppedAction;
    [SerializeField, Tooltip("Called when the player has no movement inputs")] private UnityEvent movementStoppedEvent;

    /// <summary>
    /// Called when the player inputs a camera move value, movement passed as Vector2 <br/>
    /// x = x axis (left-right), y = z axis (up-down)
    /// Actual camera movement handled by Cinemachine
    /// </summary>
    public event Action<Vector2> CameraMovementPerformedAction;
    [Header("Camera Movement")]
    [SerializeField, Tooltip("Called when player inputs a camera move value, movement passed as Vector2\nx = x axis (left-right), y = z axis (up-down)\nActual camera movement handled by Cinemachine")] 
    private UnityEvent<Vector2> CameraMovementPerformedEvent;
    /// <summary>
    /// Called when the player has no camera movement inputs
    /// </summary>
    public event Action CameraMovementStoppedAction;
    [SerializeField, Tooltip("Called when the player has no camera movement inputs")] private UnityEvent CameraMovementStoppedEvent;

    /// <summary>
    /// Called when the player inputs a jump action, state passed as bool<br/>
    /// True = player performed jump, False = player stopped performing jump
    /// </summary>
    public event Action<bool> JumpPerformedAction;
    [Header("Jump")]
    [SerializeField, Tooltip("Called when the player inputs a jump action, state passed as bool\nTrue = player performed jump, False = player stopped performing jump")]
    private UnityEvent<bool> jumpPerformedEvent;
    /// <summary>
    /// Called when the player inputs a sprint action, state passed as bool<br/>
    /// True = player performed sprint, False = player stopped performing sprint
    /// </summary>
    public event Action<bool> SprintActivatedAction;
    [Header("Sprint")]
    [SerializeField, Tooltip("Called when the player inputs a sprint action, state passed as bool\nTrue = player performed sprint, False = player stopped performing sprint")] 
    private UnityEvent<bool> sprintActivatedEvent;

    /// <summary>
    /// Read movement input values and relays if no input is performed
    /// Invokes valid actions and Unity Event with movement as Vector2
    /// </summary>
    /// <param name="move">The move action callback to read from</param>
    public void OnMovement(InputAction.CallbackContext move)
    {
        if(move.performed)
        {
            Vector2 movement = move.ReadValue<Vector2>();
            MovementPerformedAction?.Invoke(movement);
            movementPerformedEvent.Invoke(movement);
        }
        else
        {
            MovementStoppedAction?.Invoke();
            movementStoppedEvent.Invoke();
        }
    }

    /// <summary>
    /// Read camera input values and relays if no input is performed
    /// Invokes valid actions and Unity Event with movement as Vector2
    /// Cinemachine handles camera movement directly
    /// </summary>
    /// <param name="cameraMove">The camera move action callback to read from</param>
    public void OnCameraMovement(InputAction.CallbackContext cameraMove)
    {
        if(cameraMove.performed)
        {
            Vector2 movement = cameraMove.ReadValue<Vector2>();
            CameraMovementPerformedAction?.Invoke(movement);
            CameraMovementPerformedEvent.Invoke(movement);
        }
        else
        {
            CameraMovementStoppedAction?.Invoke();
            CameraMovementStoppedEvent.Invoke();
        }
    }

    /// <summary>
    /// Read jump input value as a button; trasmits true state depending if treshold is reached, false otherwise
    /// </summary>
    /// <param name="jump">The jump action callback to read from</param>
    public void OnJump(InputAction.CallbackContext jump) 
    {
        JumpPerformedAction?.Invoke(jump.performed);
        jumpPerformedEvent.Invoke(jump.performed);
    }

    /// <summary>
    /// Read sprint input value as a button; trasmits true state depending if treshold is reached, false otherwise
    /// </summary>
    /// <param name="sprint">The sprint action callback to read from</param>
    public void ReadSprint(InputAction.CallbackContext sprint)
    {
        SprintActivatedAction?.Invoke(sprint.performed);
        sprintActivatedEvent.Invoke(sprint.performed);
    }
}