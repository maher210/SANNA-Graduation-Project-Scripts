using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    private CinemachineCamera currentCamera;
    [SerializeField] private CinemachineCamera defaultCamera;

    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        defaultCamera.Priority = activePriority;
    }

    public void ActivateCamera(CinemachineCamera newCamera, Transform followTarget)
    {
        if (currentCamera == newCamera) return;

        if (currentCamera != null)
        {
            currentCamera.Priority = inactivePriority;
        }

        currentCamera = newCamera;

        if (followTarget != null)
        {
            currentCamera.Follow = followTarget;
        }

        currentCamera.ForceCameraPosition(
            currentCamera.transform.position,
            currentCamera.transform.rotation
        );

        currentCamera.Priority = activePriority;
    }
}