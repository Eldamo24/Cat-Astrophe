using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class PlayerTouchMovement : MonoBehaviour
{
    private const float LevelTolerance = 0.1f;
    private const float MinDropThreshold = -0.2f;
    private const float RaycastDistance = 100f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("Jump Parameters")]
    [SerializeField] private float maxHorizontalJumpDistance = 2.5f;
    [SerializeField] private float maxVerticalClimbHeight = 1.2f;
    [SerializeField] private float maxVerticalDropHeight = 2f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask climbableMask;

    [Header("References")]
    [SerializeField] private CameraManager cameraManager;

    private readonly List<IMovementAction> movementActions = new List<IMovementAction>();

    private Rigidbody rb;
    private Animator animator;

    private Vector3 targetPosition;
    private bool hasTarget;
    private bool isGrounded = true;
    private bool isOnClimbable;
    private MovementState currentState = MovementState.Idle;

    private float currentSpeed;

    private enum MovementState
    {
        Idle,
        Moving,
        Jumping,
        Falling
    }

    private void Awake()
    {
        CacheComponents();
        ConfigurePhysics();
        RegisterMovementActions();
    }

    private void Update()
    {
        ProcessTouchInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        CheckGroundStatus();
    }

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void ConfigurePhysics()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void RegisterMovementActions()
    {
        movementActions.Add(new WalkAction(this));
        movementActions.Add(new ClimbAction(this));
        movementActions.Add(new DescendAction(this));
    }

    private void ProcessTouchInput()
    {
        if (!CanProcessTouch())
        {
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (IsPointerOverUI(touch.position))
        {
            return;
        }

        if (!TryGetTouchHit(touch.position, out RaycastHit hit))
        {
            ClearTarget();
            return;
        }

        foreach (IMovementAction action in movementActions)
        {
            if (action.TryExecute(hit))
            {
                return;
            }
        }

        ClearTarget();
    }

    private bool CanProcessTouch()
    {
        return Input.touchCount > 0
            && currentState != MovementState.Jumping
            && currentState != MovementState.Falling;
    }

    private bool TryGetTouchHit(Vector2 touchPosition, out RaycastHit hit)
    {
        Ray ray = cameraManager.ActiveCamera.ScreenPointToRay(touchPosition);
        LayerMask hitMask = groundMask | climbableMask;
        return Physics.Raycast(ray, out hit, RaycastDistance, hitMask);
    }

    private void HandleMovement()
    {
        if (currentState == MovementState.Jumping || currentState == MovementState.Falling)
        {
            return;
        }

        if (!hasTarget)
        {
            SetHorizontalVelocity(Vector3.zero);
            currentState = MovementState.Idle;
            return;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.magnitude > stoppingDistance)
        {
            MoveTowards(direction.normalized * moveSpeed);
            currentState = MovementState.Moving;
        }
        else
        {
            SetHorizontalVelocity(Vector3.zero);
            ClearTarget();
            currentState = MovementState.Idle;
        }

        currentSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
    }

    private void MoveTowards(Vector3 desiredVelocity)
    {
        Vector3 velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);
        rb.velocity = velocity;

        Vector3 lookDirection = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
        if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, Time.fixedDeltaTime * 10f));
    }

    private void SetHorizontalVelocity(Vector3 velocity)
    {
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);
    }

    private IEnumerator JumpToTarget(Vector3 destination)
    {
        if (currentState == MovementState.Jumping)
        {
            yield break;
        }

        currentState = MovementState.Jumping;
        ClearTarget();

        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        direction.Normalize();

        rb.velocity = Vector3.zero;
        rb.AddForce(direction * (jumpForce * 0.4f) + Vector3.up * jumpForce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(0.5f);

        rb.MovePosition(new Vector3(destination.x, destination.y + 0.05f, destination.z));
        rb.velocity = Vector3.zero;

        currentState = MovementState.Idle;
        isGrounded = true;
        isOnClimbable = true;
    }

    private IEnumerator DescendToGround(Vector3 destination)
    {
        if (currentState == MovementState.Falling)
        {
            yield break;
        }

        currentState = MovementState.Falling;
        ClearTarget();

        Vector3 start = transform.position;
        float elapsed = 0f;
        const float duration = 0.4f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, new Vector3(destination.x, destination.y + 0.05f, destination.z), elapsed);
            yield return null;
        }

        currentState = MovementState.Idle;
        isGrounded = true;
        isOnClimbable = false;
    }

    private void CheckGroundStatus()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out _, 0.3f, groundMask | climbableMask))
        {
            isGrounded = true;
            return;
        }

        isGrounded = false;
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", currentSpeed);
    }

    private void ClearTarget()
    {
        hasTarget = false;
        isOnClimbable = false;
    }

    private void SetTarget(Vector3 position, bool onClimbable)
    {
        targetPosition = position;
        hasTarget = true;
        isOnClimbable = onClimbable;
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    private interface IMovementAction
    {
        bool TryExecute(RaycastHit hit);
    }

    private sealed class WalkAction : IMovementAction
    {
        private readonly PlayerTouchMovement controller;

        public WalkAction(PlayerTouchMovement controller)
        {
            this.controller = controller;
        }

        public bool TryExecute(RaycastHit hit)
        {
            float verticalDiff = hit.point.y - controller.transform.position.y;

            if (Mathf.Abs(verticalDiff) >= LevelTolerance)
            {
                return false;
            }

            Vector3 position = hit.point;
            position.y = controller.transform.position.y;
            controller.SetTarget(position, false);
            return true;
        }
    }

    private sealed class ClimbAction : IMovementAction
    {
        private readonly PlayerTouchMovement controller;

        public ClimbAction(PlayerTouchMovement controller)
        {
            this.controller = controller;
        }

        public bool TryExecute(RaycastHit hit)
        {
            float verticalDiff = hit.point.y - controller.transform.position.y;
            if (!IsValidJump(verticalDiff, hit.point, controller.transform.position))
            {
                return false;
            }

            controller.StartCoroutine(controller.JumpToTarget(hit.point));
            return true;
        }

        private bool IsValidJump(float verticalDiff, Vector3 target, Vector3 origin)
        {
            if (verticalDiff <= LevelTolerance || verticalDiff > controller.maxVerticalClimbHeight)
            {
                return false;
            }

            float horizontalDistance = Vector3.Distance(new Vector3(origin.x, 0f, origin.z), new Vector3(target.x, 0f, target.z));
            return horizontalDistance <= controller.maxHorizontalJumpDistance;
        }
    }

    private sealed class DescendAction : IMovementAction
    {
        private readonly PlayerTouchMovement controller;

        public DescendAction(PlayerTouchMovement controller)
        {
            this.controller = controller;
        }

        public bool TryExecute(RaycastHit hit)
        {
            float verticalDiff = hit.point.y - controller.transform.position.y;

            if (!IsValidDrop(verticalDiff))
            {
                return false;
            }

            controller.StartCoroutine(controller.DescendToGround(hit.point));
            return true;
        }

        private bool IsValidDrop(float verticalDiff)
        {
            float dropHeight = Mathf.Abs(verticalDiff);
            return verticalDiff < MinDropThreshold && dropHeight <= controller.maxVerticalDropHeight;
        }
    }
}
