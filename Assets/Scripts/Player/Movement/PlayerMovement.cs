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
    private UInt16 aimModeSlowID;

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
            if(CanJump && Grounded)
            {
                rb.AddRelativeForce(jumpForce * movementRoot.up, ForceMode.Impulse);
                jumpReleased = true;
                CanJump = false;
                if (jumpCooldownTimer != null) StopCoroutine(jumpCooldownTimer);
                jumpCooldownTimer = StartCoroutine(WaitForJumpCooldown());
            }
        }
        else if(!Grounded && jumpReleased)
        {
            rb.AddRelativeForce(-jumpDownMultiplier * jumpForce * movementRoot.up, ForceMode.Impulse);
            jumpReleased = false;
        }
    }

    public void ReadMovement(Vector2 movement)
    {
        movementVector = new Vector3(movement.x, 0, movement.y);
        aimingPoint.position = movementRoot.position - movementForward * movementVector.z + movementRight * movementVector.x;
    }

    public void ReadCameraMovement(Vector2 cameraMovement)
    {
        if (Aiming)
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

    public void ReadAction(bool action)
    {
        if(action)
        {
            ShootGrapple();
        }
        else
        {
            ReleaseGrapple();
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
            movementForward = Vector3.ProjectOnPlane(orbitVirtualCamera.position - movementRoot.position, movementRoot.up).normalized;
            movementRight = Vector3.Cross(movementForward, movementRoot.up).normalized;
            Vector3 direction = (-movementForward * movementVector.z + movementRight * movementVector.x).normalized;
            rb.AddForce(sprintMultiplierValue * speedMultiplier * direction, ForceMode.Impulse);
            aimingPoint.position = movementRoot.position + direction;
            visualRoot.LookAt(aimingPoint);
        }
    }

    public void EnterAimMode()
    {
        if(!Aiming)
        {
            canMove = false;
            canJump = false;
            orbitVirtualCamera.gameObject.SetActive(false);
            aimVirtualCamera.gameObject.SetActive(true);
            Aiming = true;
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
        if(Aiming)
        {
            canMove = true;
            canJump = true;
            aimVirtualCamera.gameObject.SetActive(false);
            orbitVirtualCamera.gameObject.SetActive(true);
            Aiming = false;
            TimeManager.Instance.RemoveMultiplier(aimModeSlowID);
            StopCoroutine(WaitForGrounded());
            aimingPoint.position = movementRoot.position - movementForward;
            visualRoot.LookAt(aimingPoint);
            AimModeUI.SetActive(false);
        }
    }

    public void ShootGrapple()
    {
        if (Aiming && Physics.Raycast(visualCamera.transform.position, visualCamera.transform.forward, out groundCheckInfo, grappleRange, groundLayers))
        {
            grapplePoint.transform.position = groundCheckInfo.point;
            if (!grapplePoint.activeSelf)
            { 
                grapplePoint.SetActive(true);
                StartCoroutine(UpdateGrapple());
            }
        }
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

    public void ReleaseGrapple()
    {
        if(grapplePoint.activeSelf) grapplePoint.SetActive(false);
    }

    private void GroundCheck()
    {
        if(Physics.CheckSphere(movementRoot.position - movementRoot.up * playerHeight / 2, groundCheckSize, groundLayers)) Grounded = true;
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
        if(Aiming) Gizmos.DrawRay(visualCamera.transform.position, visualCamera.transform.forward * grappleRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(movementRoot.position - (movementRoot.up * playerHeight / 2), groundCheckSize);
        Gizmos.DrawWireSphere(groundCheckInfo.point, 0.5f);
    }
}