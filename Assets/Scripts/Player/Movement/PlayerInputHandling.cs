using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputHandling : MonoBehaviour
{
    public PlayerInput playerInput;

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
    /// Called when the player inputs an aim mode action, state passed as bool<br/>
    /// True = player performed aim mode, False = player stopped performing aim mode
    /// </summary>
    public event Action<bool> AimModeActivatedAction;
    [Header("AimMode")]
    [SerializeField, Tooltip("Called when the player inputs an aim mode action, state passed as bool\nTrue = player performed aim mode, False = player stopped performing aim mode")]
    private UnityEvent<bool> aimModeActivatedEvent;

    /// <summary>
    /// Called when the player inputs an action action, state passed as bool<br/>
    /// True = player performed action, False = player stopped performing action
    /// </summary>
    public event Action<bool> ActionActivatedAction;
    [SerializeField, Tooltip("Called when the player inputs an action action, state passed as bool\nTrue = player performed action, False = player stopped performing action")]
    private UnityEvent<bool> actionActivatedEvent;

    /// <summary>
    /// Called when the player inputs an aim mode slection change action, request passed as float<br/>
    /// </summary>
    public event Action<int> AimModeSelectionActivatedAction;
    [SerializeField, Tooltip("Called when the player inputs an aim mode slection change action, request passed as float")]
    private UnityEvent<int> aimModeSelectionActivatedEvent;

    /// <summary>
    /// Called when the player inputs an slicing aim action, request passed as Vector2<br/>
    /// </summary>
    public event Action<Vector2> SliceAimActivatedAction;
    [SerializeField, Tooltip("Called when the player inputs an slicing aim action, request passed as Vector2")]
    private UnityEvent<Vector2> sliceAimActivatedEvent;



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
    /// Read jump input value as a button; trasmits true state depending if threshold is reached, false otherwise
    /// </summary>
    /// <param name="jump">The jump action callback to read from</param>
    public void OnJump(InputAction.CallbackContext jump) 
    {
        JumpPerformedAction?.Invoke(jump.performed);
        jumpPerformedEvent.Invoke(jump.performed);
    }

    /// <summary>
    /// Read sprint input value as a button; trasmits true state depending if threshold is reached, false otherwise
    /// </summary>
    /// <param name="sprint">The sprint action callback to read from</param>
    public void ReadSprint(InputAction.CallbackContext sprint)
    {
        SprintActivatedAction?.Invoke(sprint.performed);
        sprintActivatedEvent.Invoke(sprint.performed);
    }

    /// <summary>
    /// Read aim mode input value as a button; trasmits true state depending if threshold is reached, false otherwise
    /// </summary>
    /// <param name="aimMode">The aim mode action callback to read from</param>
    public void ReadAimMode(InputAction.CallbackContext aimMode)
    {
        AimModeActivatedAction?.Invoke(aimMode.performed);
        aimModeActivatedEvent.Invoke(aimMode.performed);
    }

    /// <summary>
    /// Read action input value as button; transmits true state depending if threshold is reached, false otherwise
    /// </summary>
    /// <param name="action">The action action callback to read from</param>
    public void ReadAction(InputAction.CallbackContext action)
    {
        ActionActivatedAction?.Invoke(action.performed);
        actionActivatedEvent.Invoke(action.performed);
    }

    public void ReadAimModeSelection(InputAction.CallbackContext aimModeSelect)
    {
        if(aimModeSelect.performed)
        {
            float select = aimModeSelect.ReadValue<float>();
            int selectValue = 0;
            if (select < 0)
            {
                selectValue = -1;
            }
            else if (select > 0)
            {
                selectValue = 1;
            }
            AimModeSelectionActivatedAction?.Invoke(selectValue);
            aimModeSelectionActivatedEvent.Invoke(selectValue);
        }
    }

    public void ReadSliceAim(InputAction.CallbackContext sliceAim)
    {
        if(sliceAim.performed)
        {
            Vector2 aim = sliceAim.ReadValue<Vector2>();
            if(playerInput.currentControlScheme == "KeyboardMouse")
            {
                aim -= new Vector2(Screen.width/2, Screen.height/2);
            }
            aim.Normalize();
            aim.x *= -1;
            Debug.Log(aim);
            SliceAimActivatedAction?.Invoke(aim);
            sliceAimActivatedEvent.Invoke(aim);
        }
    }
}