using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTouchMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;   // qué tan rápido llega a la velocidad deseada
    [SerializeField] private float deceleration = 15f;   // qué tan rápido frena
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("Input")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private CameraManager cameraManager;

    private Rigidbody rb;
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private Vector3 currentVelocity = Vector3.zero; // para suavizar

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        HandleTouchInput();
    }

    void FixedUpdate()
    {
        if (!hasTarget)
        {
            // Frena suavemente cuando no hay target
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
            return;
        }

        Vector3 direction = (targetPosition - rb.position);
        float distance = direction.magnitude;

        if (distance > stoppingDistance)
        {
            // Velocidad objetivo
            Vector3 desiredVelocity = direction.normalized * moveSpeed;

            // Suavizar aceleración
            rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, acceleration * Time.fixedDeltaTime);

            // Rotación hacia dirección de movimiento
            if (rb.velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);
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