using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Rigidbody targetRb;   // en lugar de Transform
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
    [SerializeField] private float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (!targetRb) return;

        Vector3 desiredPosition = targetRb.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // (opcional) mantener la rotación fija isométrica:
        transform.rotation = Quaternion.Euler(45, 0, 0);
    }
}