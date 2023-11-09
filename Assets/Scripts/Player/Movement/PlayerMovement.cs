using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField, Tooltip("Rigidbody for player movement")] 
    private Rigidbody rb;
    [SerializeField, Tooltip("Input Handler for parsing input")]
    private PlayerInputHandling inputHandler;
    [SerializeField, Tooltip("Movement root, should be the object script is on")] 
    private Transform movementRoot;
    [SerializeField, Tooltip("The camera movement should be relative to")] 
    private Transform movementCamera;

    [SerializeField, Min(0)] private float jumpMultiplier;
    [SerializeField, Min(0)] private float speedMultiplier;
    [SerializeField, Range(0, 1)] private float slowdownMultiplier;
    [SerializeField, Range(1, 5)] private float sprintMultiplier;

    public bool CanMove { get { return canMove; } private set { canMove = value; } }
    [SerializeField, Tooltip("Whether the player can move from input or not")] 
    private bool canMove = true;
    public bool Sprinting { get; private set; } = false;

    private Vector3 movementVector = Vector3.zero;
    private Vector3 movementForward = Vector3.forward;
    private Vector3 movementRight = Vector3.right;
    private float sprintMultiplierValue = 1;

    private void Start()
    {
        if (inputHandler == null) inputHandler = FindObjectOfType<PlayerInputHandling>();
        if (movementRoot == null) movementRoot = GetComponent<Transform>();
    }

    private void OnEnable()
    {
        inputHandler.MovementPerformedAction += ReadMovement;
        inputHandler.MovementStoppedAction += ReadStop;
        inputHandler.JumpPerformedAction += ReadJump;
        inputHandler.SprintActivatedAction += ReadSprint;
    }

    private void OnDisable()
    {
        inputHandler.MovementPerformedAction -= ReadMovement;
        inputHandler.MovementStoppedAction -= ReadStop;
        inputHandler.JumpPerformedAction -= ReadJump;
        inputHandler.SprintActivatedAction -= ReadSprint;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    public void ReadJump(bool jump)
    {
        if (jump) { rb.AddRelativeForce(Time.fixedDeltaTime * jumpMultiplier * transform.up, ForceMode.Impulse); }
    }

    public void ReadMovement(Vector2 movement)
    {
        movementVector = new Vector3(movement.x, 0, movement.y);
    }

    public void ReadStop()
    {
        movementVector = Vector3.zero;
    }

    public void ReadSprint(bool sprint)
    {
        Sprinting = sprint;
        if (sprint) sprintMultiplierValue = sprintMultiplier;
        else sprintMultiplierValue = 1;
    } 

    public void MovePlayer()
    {
        if(canMove)
        {
            movementForward = Vector3.ProjectOnPlane(movementCamera.position - transform.position, transform.up).normalized;
            movementRight = Vector3.Cross(movementForward, transform.up).normalized;
            Vector3 velocity = rb.velocity;
            velocity.x /= 1 + (slowdownMultiplier / sprintMultiplierValue);
            velocity.z /= 1 + (slowdownMultiplier / sprintMultiplierValue);
            rb.velocity = velocity;
            rb.AddForce(sprintMultiplierValue * speedMultiplier * Time.fixedDeltaTime * (-movementForward * movementVector.z + movementRight * movementVector.x).normalized, ForceMode.Impulse);
        }
    }
}