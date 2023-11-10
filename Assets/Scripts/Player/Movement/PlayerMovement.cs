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

    [Header("Parameters")]
    [SerializeField, Min(0), Tooltip("The height (meters) of the character, used for ground and ceiling checks")]
    private float playerHeight = 2;
    [SerializeField, Tooltip("The layers used for refreshing jump")]
    private LayerMask groundLayers;
    [SerializeField, Min(0), Tooltip("The force the character jumps with (newtons)")] 
    private float jumpForce;
    [SerializeField, Range(0, 1), Tooltip("The down multiplier when jump is released midair")]
    private float jumpDownMultiplier;
    [SerializeField, Tooltip("The time it takes to be able to jump again")]
    private float jumpCooldown = 0.2f;
    [SerializeField, Min(0), Tooltip("The force the character moves with (newtons)")] 
    private float speedMultiplier;
    [SerializeField, Range(0, 1), Tooltip("The percent decrease in the XZ velocity every frame")] 
    private float slowdownMultiplier;
    [SerializeField, Range(1, 5), Tooltip("The amount increase in force and reduction in slowdown")] 
    private float sprintMultiplier;

    public bool CanMove { get { return canMove; } private set { canMove = value; } }
    [SerializeField, Tooltip("Whether the player can move from input or not")] 
    private bool canMove = true;
    public bool CanJump { get { return canJump; } private set { canJump = value; } }
    [SerializeField, Tooltip("Whether the player can jump or not")]
    private bool canJump = true;
    public bool Sprinting { get; private set; } = false;
    public bool Grounded { get; private set; } = false;

    private bool jumpReleased = false;
    private Vector3 movementVector = Vector3.zero;
    private Vector3 movementForward = Vector3.forward;
    private Vector3 movementRight = Vector3.right;
    private float sprintMultiplierValue = 1;
    private Coroutine jumpCooldownTimer;
    private RaycastHit groundCheckInfo;

    private void Start()
    {
        if (inputHandler == null) inputHandler = FindObjectOfType<PlayerInputHandling>();
        if (movementRoot == null) movementRoot = GetComponent<Transform>();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        Uninitialize();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    public void Initialize()
    {
        inputHandler.MovementPerformedAction += ReadMovement;
        inputHandler.MovementStoppedAction += ReadStop;
        inputHandler.JumpPerformedAction += ReadJump;
        inputHandler.SprintActivatedAction += ReadSprint;

        if (jumpCooldownTimer != null) StopCoroutine(jumpCooldownTimer);
        jumpCooldownTimer = StartCoroutine(WaitForJumpCooldown());
    }

    public void Uninitialize()
    {
        inputHandler.MovementPerformedAction -= ReadMovement;
        inputHandler.MovementStoppedAction -= ReadStop;
        inputHandler.JumpPerformedAction -= ReadJump;
        inputHandler.SprintActivatedAction -= ReadSprint;

        StopAllCoroutines();
    }

    public void ReadJump(bool jump)
    {
        if (jump) 
        {
            if(CanJump && Grounded)
            {
                rb.AddRelativeForce(Time.fixedDeltaTime * jumpForce * movementRoot.up, ForceMode.Impulse);
                Grounded = false;
                jumpReleased = true;
                if (jumpCooldownTimer != null) StopCoroutine(jumpCooldownTimer);
                jumpCooldownTimer = StartCoroutine(WaitForJumpCooldown());
            }
        }
        else if(!Grounded && jumpReleased)
        {
            rb.AddRelativeForce(-jumpDownMultiplier * jumpForce * Time.fixedDeltaTime * movementRoot.up, ForceMode.Impulse);
            jumpReleased = false;
        }
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
            movementForward = Vector3.ProjectOnPlane(movementCamera.position - movementRoot.position, movementRoot.up).normalized;
            movementRight = Vector3.Cross(movementForward, movementRoot.up).normalized;
            Vector3 velocity = rb.velocity;
            velocity.x /= 1 + (slowdownMultiplier / sprintMultiplierValue);
            velocity.z /= 1 + (slowdownMultiplier / sprintMultiplierValue);
            rb.velocity = velocity;
            rb.AddForce(sprintMultiplierValue * speedMultiplier * Time.fixedDeltaTime * (-movementForward * movementVector.z + movementRight * movementVector.x).normalized, ForceMode.Impulse);
        }
    }

    private void GroundCheck()
    {
        if (Physics.Raycast(movementRoot.position, movementRoot.up * -1, out groundCheckInfo, playerHeight / 2, groundLayers))
        {
            Grounded = true;
        }
    }

    private IEnumerator WaitForJumpCooldown()
    {
        yield return new WaitForSeconds(jumpCooldown);
        while(!Grounded)
        {
            GroundCheck();
            yield return new WaitForFixedUpdate();
        }
    }

}