using UnityEngine;
using Unity.Cinemachine;

public class CameraZone : MonoBehaviour
{
    public CinemachineCamera targetCamera;
    public Transform followTarget;

    // REMOVED: The targetCamera.Priority = 10 in Start(). 
    // Let your CameraManager handle the starting priorities instead!

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivateZoneCamera(other.transform);
        }
    }

    // FIX: If the player starts the game ALREADY inside this zone, 
    // this ensures the camera still triggers.
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && CameraManager.Instance != null)
        {
            // Only trigger if this isn't already the active camera
            CameraManager.Instance.ActivateCamera(targetCamera, followTarget != null ? followTarget : other.transform);
        }
    }

    private void ActivateZoneCamera(Transform playerTransform)
    {
        if (CameraManager.Instance != null)
        {
            // If you didn't assign a specific followTarget in the inspector, 
            // default to following the player who walked in.
            Transform target = (followTarget != null) ? followTarget : playerTransform;

            CameraManager.Instance.ActivateCamera(targetCamera, target);
        }
    }
}