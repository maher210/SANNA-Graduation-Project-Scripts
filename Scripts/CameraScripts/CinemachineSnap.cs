using UnityEngine;
using Unity.Cinemachine;

public class CameraDebug : MonoBehaviour
{
    void Start()
    {
        foreach (var cam in Object.FindObjectsByType<CinemachineCamera>())
        {
            Debug.Log(
                $"{cam.name} | Priority={cam.Priority} | Enabled={cam.isActiveAndEnabled}");
        }
    }
}