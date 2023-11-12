using Cinemachine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AimModeParameters
{
    public float Duration;
    public float Multiplier;
    public AimModeParameters(float duration, float slowdown)
    {
        Duration = duration;
        Multiplier = slowdown;
    }
}

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
    [SerializeField, Tooltip("Visual root, rotation is handled on this")]
    private Transform visualRoot;
    [SerializeField, Tooltip("The camera movement should be relative to")] 
    private Transform movementCamera;
    [SerializeField, Tooltip("The camera used for aiming")] 
    private Transform aimCamera;
    [SerializeField, Tooltip("The point the player should face")]
    private Transform aimingPoint;

    [Header("Parameters")]
    [SerializeField, Min(0), Tooltip("The height (meters) of the character, used for ground and ceiling checks")]
    private float playerHeight = 2;
    [SerializeField, Tooltip("The area to check for ground")]
    private float groundCheckSize = 1;
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
    [SerializeField, Tooltip("The parameters for when aim mode is activated")]
    private AimModeParameters aimModeParameters;

    public bool CanMove { get { return canMove; } private set { canMove = value; } }
    [SerializeField, Tooltip("Whether the player can move from input or not")] 
    private bool canMove = true;
    public bool CanJump { get { return canJump; } private set { canJump = value; } }
    [SerializeField, Tooltip("Whether the player can jump or not")]
    private bool canJump = true;
    public bool Sprinting { get; private set; } = false;
    public bool Grounded { get; private set; } = false;
    public bool Aiming { get; private set; } = false;

    private bool jumpReleased = false;
    private Vector3 movementVector = Vector3.zero;
    private Vector3 movementForward = Vector3.forward;
    private Vector3 movementRight = Vector3.right;
    private float sprintMultiplierValue = 1;
    private Coroutine jumpCooldownTimer;
    private RaycastHit groundCheckInfo;
    private TimeMultiplier aimModeTimeMultiplier;
    private Collider[] groundDetections = new Collider[1];

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
        GroundCheck();
        if (Aiming)
        {
            aimingPoint.position = movementRoot.position + (movementRoot.position - aimCamera.position);
        }
        else
        {
            if (movementVector.magnitude < 0.001f) aimingPoint.position = movementRoot.position - movementForward;
            else aimingPoint.position = movementRoot.position + (-movementForward * movementVector.z + movementRight * movementVector.x).normalized;
        }
        visualRoot.LookAt(aimingPoint);
    }

    public void Initialize()
    {
        inputHandler.MovementPerformedAction += ReadMovement;
        inputHandler.MovementStoppedAction += ReadStop;
        inputHandler.JumpPerformedAction += ReadJump;
        inputHandler.CameraMovementPerformedAction += ReadCameraMovement;
        inputHandler.AimModeActivatedAction += ReadAimMode;
        inputHandler.SprintActivatedAction += ReadSprint;

        if (jumpCooldownTimer != null) StopCoroutine(jumpCooldownTimer);
        jumpCooldownTimer = StartCoroutine(WaitForJumpCooldown());

        aimModeTimeMultiplier.Multiplier = aimModeParameters.Multiplier;
        aimModeTimeMultiplier.Duration = aimModeParameters.Duration;
        aimModeTimeMultiplier.Source = gameObject;
    }

    public void Uninitialize()
    {
        inputHandler.MovementPerformedAction -= ReadMovement;
        inputHandler.MovementStoppedAction -= ReadStop;
        inputHandler.JumpPerformedAction -= ReadJump;
        inputHandler.CameraMovementPerformedAction -= ReadCameraMovement;
        inputHandler.AimModeActivatedAction -= ReadAimMode;
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
                jumpReleased = true;
                CanJump = false;
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

    public void ReadCameraMovement(Vector2 cameraMovement)
    {

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

    public void ReadAimMode(bool aimMode)
    {
        if(aimMode)
        {
            EnterAimMode();
        }
        else
        {
            EnterMovementMode();
        }
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

    public void EnterAimMode()
    {
        if(!Aiming)
        {
            canMove = false;
            canJump = false;
            movementCamera.gameObject.SetActive(false);
            aimCamera.gameObject.SetActive(true);
            Aiming = true;
            TimeManager.Instance.AddMultiplier(aimModeTimeMultiplier);
            if(!Grounded) StartCoroutine(WaitForGrounded());
        }
    }

    public void EnterMovementMode()
    {
        if(Aiming)
        {
            canMove = true;
            canJump = true;
            aimCamera.gameObject.SetActive(false);
            movementCamera.gameObject.SetActive(true);
            Aiming = false;
            TimeManager.Instance.RemoveMultiplier(aimModeTimeMultiplier);
            StopCoroutine(WaitForGrounded());
        }
    }

    private void GroundCheck()
    {
        groundDetections[0] = null;
        Physics.OverlapSphereNonAlloc(movementRoot.position - movementRoot.up * playerHeight / 2, groundCheckSize, groundDetections, groundLayers);
        if (groundDetections[0] != null) Grounded = true;
        else Grounded = false;
    }

    private IEnumerator WaitForJumpCooldown()
    {
        yield return new WaitForSeconds(jumpCooldown);
        yield return new WaitUntil(() => Grounded);
        CanJump = true;
    }

    private IEnumerator WaitForGrounded()
    {
        yield return new WaitUntil(() => Grounded);
        EnterMovementMode();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(aimingPoint.position, 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(movementRoot.position - (movementRoot.up * playerHeight / 2), groundCheckSize);
    }
}