using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class PlayerTouchMovement : MonoBehaviour
{
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

    private Rigidbody rb;
    private Animator animator;

    private Vector3 targetPosition;
    private bool hasTarget;
    private bool isGrounded = true;
    private bool isOnClimbable = false;
    private bool isJumping = false;
    private bool isFalling = false;

    private float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleTouchInput();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        HandleMovement();
        CheckGroundStatus();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0 || isJumping) return;

        Touch touch = Input.GetTouch(0);
        Ray ray = cameraManager.ActiveCamera.ScreenPointToRay(touch.position);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask | climbableMask))
        {
            float verticalDiff = hit.point.y - transform.position.y;
            float horizontalDist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                                    new Vector3(hit.point.x, 0, hit.point.z));

            // ?? Caso 1: caminar o moverse sobre mismo nivel
            if (Mathf.Abs(verticalDiff) < 0.1f)
            {
                SetTarget(new Vector3(hit.point.x, transform.position.y, hit.point.z), false);
                return;
            }

            // ?? Caso 2: subir a un objeto
            if (verticalDiff > 0.1f && verticalDiff <= maxVerticalClimbHeight && horizontalDist <= maxHorizontalJumpDistance)
            {
                StartCoroutine(JumpToTarget(new Vector3(hit.point.x, hit.point.y, hit.point.z)));
                return;
            }

            // ?? Caso 3: bajar desde un objeto
            if (verticalDiff < -0.2f && Mathf.Abs(verticalDiff) <= maxVerticalDropHeight)
            {
                StartCoroutine(DescendToGround(new Vector3(hit.point.x, hit.point.y, hit.point.z)));
                return;
            }

            // ?? Caso 4: destino demasiado lejos o alto ? ignorar
            hasTarget = false;
        }
    }

    private void SetTarget(Vector3 pos, bool climb)
    {
        targetPosition = pos;
        hasTarget = true;
        isOnClimbable = climb;
    }

    private void HandleMovement()
    {
        if (!hasTarget || isJumping || isFalling) return;

        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0;
        float distance = direction.magnitude;

        if (distance > stoppingDistance)
        {
            Vector3 moveDir = direction.normalized * moveSpeed;
            rb.velocity = new Vector3(moveDir.x, rb.velocity.y, moveDir.z);

            Quaternion lookRot = Quaternion.LookRotation(moveDir.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * 10f));
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            hasTarget = false;
        }

        currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
    }

    private IEnumerator JumpToTarget(Vector3 dest)
    {
        if (isJumping) yield break;
        isJumping = true;
        hasTarget = false;

        Vector3 direction = (dest - transform.position);
        direction.y = 0;
        direction.Normalize();

        rb.velocity = Vector3.zero;
        rb.AddForce(direction * (jumpForce * 0.4f) + Vector3.up * jumpForce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(0.5f);

        rb.MovePosition(new Vector3(dest.x, dest.y + 0.05f, dest.z));
        rb.velocity = Vector3.zero;

        isJumping = false;
        isGrounded = true;
        isOnClimbable = true;
    }

    private IEnumerator DescendToGround(Vector3 dest)
    {
        if (isFalling) yield break;
        isFalling = true;
        hasTarget = false;

        Vector3 start = transform.position;
        float t = 0f;
        float duration = 0.4f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, new Vector3(dest.x, dest.y + 0.05f, dest.z), t);
            yield return null;
        }

        isGrounded = true;
        isFalling = false;
        isOnClimbable = false;
    }

    private void CheckGroundStatus()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.3f, groundMask | climbableMask))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", currentSpeed);
    }
}