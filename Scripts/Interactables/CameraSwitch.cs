using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    public GameObject targetCameraGroup;
    public Animator switchAnimator;
    public string flipTriggerName = "FlipSwitch";
    // NEW: A flag to check if the player is close enough to interact
    private bool isPlayerNear = false;

    public void TurnOffCameras()
    {
        if (targetCameraGroup == null)
        {
            Debug.LogWarning("No target camera group assigned to the switch!");
            return;
        }

        // Play the switch animation
        if (switchAnimator != null)
        {
            switchAnimator.SetTrigger(flipTriggerName);
        }
        else
        {
            Debug.LogWarning("No Animator assigned to the CameraSwitch!");
        }

        SecurityCamera[] securityCams = targetCameraGroup.GetComponentsInChildren<SecurityCamera>();
        foreach (SecurityCamera cam in securityCams)
        {
            cam.enabled = false;
        }

        Transform[] allChildren = targetCameraGroup.GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            // 2. Look for the child named "lense" (or "lens")
            if (child.name.ToLower() == "lense" || child.name.ToLower() == "lens")
            {
                Renderer rend = child.GetComponent<Renderer>();
                if (rend != null)
                {
                    foreach (Material mat in rend.materials)
                    {
                        if (mat.name.Contains("RedLense"))
                        {
                            mat.DisableKeyword("_EMISSION");
                            if (mat.HasProperty("_EmissionColor"))
                            {
                                mat.SetColor("_EmissionColor", Color.black);
                            }
                        }
                    }
                }
            }

            Light childLight = child.GetComponent<Light>();
            if (childLight != null && childLight.type == LightType.Spot)
            {
                childLight.enabled = false;
            }
        }

        Debug.Log("Security systems disabled.");
    }

    private void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            TurnOffCameras();


        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
    public void ResetCameraSwitch()
    {

        if (switchAnimator != null)
        {
            switchAnimator.Play("IdleSwitch");
        }

        if (targetCameraGroup == null) return;

        SecurityCamera[] securityCams = targetCameraGroup.GetComponentsInChildren<SecurityCamera>(true);
        foreach (SecurityCamera cam in securityCams)
        {
            cam.enabled = true;
        }

        Transform[] allChildren = targetCameraGroup.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            // Restore the red lens material
            if (child.name.ToLower() == "lense" || child.name.ToLower() == "lens")
            {
                Renderer rend = child.GetComponent<Renderer>();
                if (rend != null)
                {
                    foreach (Material mat in rend.materials)
                    {
                        if (mat.name.Contains("RedLense"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            if (mat.HasProperty("_EmissionColor"))
                            {
                                // Sets the emission back to red. Adjust Color.red if you used a custom hex color!
                                mat.SetColor("_EmissionColor", Color.red);
                            }
                        }
                    }
                }
            }

            // Restore Spotlights
            Light childLight = child.GetComponent<Light>();
            if (childLight != null && childLight.type == LightType.Spot)
            {
                childLight.enabled = true;
            }
        }
    }
}