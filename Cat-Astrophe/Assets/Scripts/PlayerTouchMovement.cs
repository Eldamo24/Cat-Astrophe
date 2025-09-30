using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTouchMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private CameraManager cameraManager;

    private Rigidbody rb;
    private Vector3 targetPosition;
    private bool hasTarget = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // clave
    }

    void Update()
    {
        HandleTouchInput();
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        float distance = Vector3.Distance(rb.position, targetPosition);

        if (distance > stoppingDistance)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);

            // Rotación hacia dirección
            Vector3 direction = (targetPosition - rb.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.2f));
            }
        }
        else
        {
            hasTarget = false;
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = cameraManager.ActiveCamera.ScreenPointToRay(touch.position);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundMask))
            {
                targetPosition = new Vector3(hit.point.x, rb.position.y, hit.point.z);
                hasTarget = true;
            }
        }
    }
}