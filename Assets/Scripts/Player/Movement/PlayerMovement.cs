using System;
using System.Collections;
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

public enum PlayerMovementStates
{
    Ragdoll,
    Walking,
    Sprinting
}

public enum PlayerActionStates
{
    Passive,
    Aiming,
    FreeFall,
    SpecialAction,
}

public enum PlayerAimModes
{
    Grapple,
    Shoot,
    Cut
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
    [SerializeField, Tooltip("Main camera used for vision by the player")]
    private Camera visualCamera;
    [SerializeField, Tooltip("The camera movement should be relative to")]
    private Transform orbitVirtualCamera;
    [SerializeField, Tooltip("The camera used for aiming")]
    private Transform aimVirtualCamera;
    [SerializeField, Tooltip("The point the player should face")]
    private Transform aimingPoint;
    [SerializeField, Tooltip("The point where the grapple attracts the player")]
    private GameObject grapplePoint;
    [SerializeField, Tooltip("The maximum range the player can grapple")]
    private float grappleRange;
    [SerializeField]
    private LineRenderer grappleRenderer;
    [SerializeField, Tooltip("The UI root aim mode elements")]
    private GameObject AimModeUI;

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
    [SerializeField, Tooltip("The upward direction the player defaults to")]
    private Vector3 characterWorldUp = Vector3.up;

    public bool CanMove { get { return canMove; } private set { canMove = value; } }
    [SerializeField, Tooltip("Whether the player can move from input or not")]
    private bool canMove = true;
    public bool CanJump { get { return canJump; } private set { canJump = value; } }
    [SerializeField, Tooltip("Whether the player can jump or not")]
    private bool canJump = true;
    public bool Grounded { get; private set; } = false;

    public PlayerMovementStates MovementState
    {
        get => movementState;
        set
        {
            switch (value)
            {
                case PlayerMovementStates.Ragdoll:
                    break;
                case PlayerMovementStates.Walking:
                    break;
                case PlayerMovementStates.Sprinting:
                    break;
            }
            movementState = value;
        }
    }

    private PlayerMovementStates movementState = PlayerMovementStates.Walking;
    public PlayerActionStates ActionState
    {
        get => actionState;
        set
        {
            switch (value)
            {
                case PlayerActionStates.Passive:
                    break;
                case PlayerActionStates.Aiming:
                    break;
                case PlayerActionStates.FreeFall:
                    break;
                case PlayerActionStates.SpecialAction:
                    break;
            }
            actionState = value;
        }
    }
    private PlayerActionStates actionState = PlayerActionStates.Passive;

    public PlayerAimModes AimMode
    {
        get => aimMode;
        set
        {
            switch(value)
            {
                case PlayerAimModes.Grapple:
                    break;
                case PlayerAimModes.Shoot:
                    break;
                case PlayerAimModes.Cut:
                    break;
            }
            aimMode = value;
        }
    }
    private PlayerAimModes aimMode = PlayerAimModes.Grapple;


    private bool jumpReleased = false;
    private bool selfRighted = false;
    private Vector3 movementVector = Vector3.zero;
    private Vector3 movementForward = Vector3.forward;
    private Vector3 movementRight = Vector3.right;
    private float sprintMultiplierValue = 1;
    private Coroutine jumpCooldownTimer;
    private RaycastHit groundCheckInfo;
    private TimeMultiplier aimModeTimeMultiplier;
    private UInt32 aimModeSlowID;

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
    }

    public void Initialize()
    {
        inputHandler.MovementPerformedAction += ReadMovement;
        inputHandler.MovementStoppedAction += ReadStop;
        inputHandler.JumpPerformedAction += ReadJump;
        inputHandler.CameraMovementPerformedAction += ReadCameraMovement;
        inputHandler.AimModeActivatedAction += ReadAimMode;
        inputHandler.SprintActivatedAction += ReadSprint;
        inputHandler.ActionActivatedAction += ReadAction;

        if (jumpCooldownTimer != null) StopCoroutine(jumpCooldownTimer);
        jumpCooldownTimer = StartCoroutine(WaitForJumpCooldown());

        aimModeTimeMultiplier.Multiplier = aimModeParameters.Multiplier;
        aimModeTimeMultiplier.Duration = aimModeParameters.Duration;
        aimModeTimeMultiplier.Source = gameObject;
        Cursor.lockState = CursorLockMode.Confined;
        visualCamera.transform.parent = null;
    }

    public void Uninitialize()
    {
        inputHandler.MovementPerformedAction -= ReadMovement;
        inputHandler.MovementStoppedAction -= ReadStop;
        inputHandler.JumpPerformedAction -= ReadJump;
        inputHandler.CameraMovementPerformedAction -= ReadCameraMovement;
        inputHandler.AimModeActivatedAction -= ReadAimMode;
        inputHandler.SprintActivatedAction -= ReadSprint;
        inputHandler.ActionActivatedAction -= ReadAction;
        Cursor.lockState = CursorLockMode.None;

        StopAllCoroutines();
    }

    public void ReadJump(bool jump)
    {
        if (jump)
        {
            if (CanJump && Grounded)
            {
                rb.AddRelativeForce(jumpForce * characterWorldUp, ForceMode.Impulse);
                jumpReleased = true;
                CanJump = false;
                if (jumpCooldownTimer != null) StopCoroutine(jumpCooldownTimer);
                jumpCooldownTimer = StartCoroutine(WaitForJumpCooldown());
            }
        }
        else if (!Grounded && jumpReleased)
        {
            rb.AddRelativeForce(-jumpDownMultiplier * jumpForce * characterWorldUp, ForceMode.Impulse);
            jumpReleased = false;
        }
    }

    public void ReadMovement(Vector2 movement)
    {
        movementVector = new Vector3(movement.x, 0, movement.y);
    }

    public void ReadCameraMovement(Vector2 cameraMovement)
    {
        if (actionState == PlayerActionStates.Aiming)
        {
            aimingPoint.position = movementRoot.position + (movementRoot.position - aimVirtualCamera.position);
            visualRoot.LookAt(aimingPoint);
        }
    }

    public void ReadStop()
    {
        movementVector = Vector3.zero;
    }
    public void ReadSprint(bool sprint)
    {
        if (sprint)
        {
            movementState = PlayerMovementStates.Sprinting;
            sprintMultiplierValue = sprintMultiplier;
        }
        else
        {
            movementState = PlayerMovementStates.Walking;
            sprintMultiplierValue = 1;
        }
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

    public void ReadAction(bool action)
    {
        switch(aimMode)
        {
            case PlayerAimModes.Grapple:
                if (action)
                {
                    ShootGrapple();
                }
                else
                {
                    ReleaseGrapple();
                }
                break;
            case PlayerAimModes.Shoot:
                break;
            case PlayerAimModes.Cut:
                break;
        }
    }

    public void MovePlayer()
    {
        Vector3 velocity = rb.velocity;
        velocity.x /= 1 + (slowdownMultiplier / sprintMultiplierValue);
        velocity.z /= 1 + (slowdownMultiplier / sprintMultiplierValue);
        rb.velocity = velocity;
        if (canMove)
        {
            Vector3 direction;
            if (Grounded)
            {
                movementForward = -Vector3.ProjectOnPlane(orbitVirtualCamera.position - movementRoot.position, characterWorldUp).normalized;
                movementRight = -Vector3.Cross(movementForward, characterWorldUp).normalized;
                direction = (movementForward * movementVector.z + movementRight * movementVector.x).normalized;
            }
            else
            {
                movementForward = visualCamera.transform.forward;
                movementRight = visualCamera.transform.right;
                direction = (movementForward * movementVector.z + movementRight * movementVector.x).normalized;
            }
            rb.AddForce(sprintMultiplierValue * speedMultiplier * direction, ForceMode.Impulse);
            aimingPoint.position = movementRoot.position + direction;
            visualRoot.LookAt(aimingPoint);
        }
    }

    public void EnterAimMode()
    {
        if(actionState != PlayerActionStates.Aiming)
        {
            actionState = PlayerActionStates.Aiming;
            canMove = false;
            canJump = false;
            orbitVirtualCamera.gameObject.SetActive(false);
            aimVirtualCamera.gameObject.SetActive(true);
            aimModeSlowID = TimeManager.Instance.AddMultiplier(aimModeTimeMultiplier);
            if(!Grounded) StartCoroutine(WaitForGrounded());
            StartCoroutine(WaitForFrame());
            AimModeUI.SetActive(true);
        }
    }

    private IEnumerator WaitForFrame()
    {
        yield return new WaitForEndOfFrame();
        aimingPoint.position = movementRoot.position + (movementRoot.position - aimVirtualCamera.position);
        visualRoot.LookAt(aimingPoint);
    }

    public void EnterMovementMode()
    {
        if(actionState == PlayerActionStates.Aiming)
        {
            actionState = PlayerActionStates.Passive;
            canMove = true;
            canJump = true;
            aimVirtualCamera.gameObject.SetActive(false);
            orbitVirtualCamera.gameObject.SetActive(true);
            TimeManager.Instance.RemoveMultiplier(aimModeSlowID);
            StopCoroutine(WaitForGrounded());
            aimingPoint.position = movementRoot.position - movementForward;
            visualRoot.LookAt(aimingPoint);
            AimModeUI.SetActive(false);
        }
    }

    public void ShootGrapple()
    {
        if (Physics.Raycast(movementRoot.transform.position, visualCamera.transform.forward, out groundCheckInfo, grappleRange, groundLayers))
        {
            grapplePoint.transform.position = groundCheckInfo.point;
            if (!grapplePoint.activeSelf)
            { 
                grapplePoint.SetActive(true);
                StartCoroutine(UpdateGrapple());
                selfRighted = false;
            }
        }
    }
    public void ReleaseGrapple()
    {
        if (grapplePoint.activeSelf) grapplePoint.SetActive(false);
    }

    private IEnumerator UpdateGrapple()
    {
        while(grapplePoint.activeSelf) 
        {
            grappleRenderer.SetPosition(0, movementRoot.position);
            grappleRenderer.SetPosition(1, groundCheckInfo.point);
            yield return new WaitForFixedUpdate();
        }
    }



    private void GroundCheck()
    {
        if (Physics.CheckSphere(movementRoot.position - characterWorldUp * playerHeight / 2, groundCheckSize, groundLayers))
        {
            Grounded = true;
            if(!selfRighted)
            {
                aimingPoint.position = movementRoot.position + Vector3.ProjectOnPlane(movementRoot.forward, characterWorldUp).normalized;
                visualRoot.LookAt(aimingPoint);
                selfRighted = true;
            }
        }
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
        if(actionState == PlayerActionStates.Aiming) Gizmos.DrawRay(movementRoot.transform.position, visualCamera.transform.forward * grappleRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(movementRoot.position - (movementRoot.up * playerHeight / 2), groundCheckSize);
        Gizmos.DrawWireSphere(groundCheckInfo.point, 0.5f);
    }
}