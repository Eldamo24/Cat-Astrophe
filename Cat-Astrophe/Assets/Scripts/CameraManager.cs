using UnityEngine;
using UnityEngine.UI; // Necesario para Button

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera followCamera;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Button switchCameraButton; // Referencia al botón

    private Camera activeCamera;
    public Camera ActiveCamera => activeCamera;

    void Start()
    {
        SetActiveCamera(followCamera);

        if (switchCameraButton != null)
            switchCameraButton.onClick.AddListener(SwitchCamera);
    }

    private void SwitchCamera()
    {
        if (activeCamera == followCamera)
            SetActiveCamera(overviewCamera);
        else
            SetActiveCamera(followCamera);
    }

    private void SetActiveCamera(Camera cam)
    {
        followCamera.gameObject.SetActive(false);
        overviewCamera.gameObject.SetActive(false);

        cam.gameObject.SetActive(true);
        activeCamera = cam;
    }
}