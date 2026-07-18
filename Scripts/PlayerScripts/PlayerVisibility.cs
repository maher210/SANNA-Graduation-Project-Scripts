using UnityEngine;

public class PlayerVisibility : MonoBehaviour
{
    private LightSource[] lightSources;
    public float visibility = 0f; // النتيجة النهائية

    float manualVisibility = -1f; // لو في سكربت ثاني بدو يفرض قيمة

    PlayerMovement movement;
    LanternSystem lantern;

    public bool inLight;
    public bool isHidden;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        lantern = GetComponent<LanternSystem>();
        lightSources = FindObjectsByType<LightSource>();

Debug.Log("Light Sources Found: " + lightSources.Length);
    } 

    void Update()
    {
//     Debug.Log(
//     "Visibility: " + visibility +
//     " | InLight: " + inLight
// );
        CalculateVisibility();
        Debug.Log("In Light: " + inLight);
    }

    public void SetVisibility(float value)
    {
        manualVisibility = value;
    }
 bool IsInsideAnyLight()
{
    LightSource[] lightSources =
        FindObjectsByType<LightSource>(FindObjectsInactive.Exclude);

    foreach (LightSource source in lightSources)
    {
        Light lightComp =
            source.GetComponent<Light>();

        if (lightComp == null)
            continue;

        if (!lightComp.enabled)
            continue;

        Vector3 directionToPlayer =
            transform.position - lightComp.transform.position;

        float distance =
            directionToPlayer.magnitude;

        // خارج مدى الضوء
        if (distance > lightComp.range)
            continue;

        float angle =
            Vector3.Angle(
                lightComp.transform.forward,
                directionToPlayer
            );

        // خارج مخروط الضوء
        if (angle > lightComp.spotAngle * 0.4f)
            continue;

        return true;
    }

    return false;
}
    void CalculateVisibility()
    {
        inLight = IsInsideAnyLight();

        visibility = 0f;

        if (inLight || lantern.isOn)
            visibility += 10f;
        else
            visibility += 5f;

        if (movement.IsCrouching)
            visibility *= 0.5f;

        if (movement.moveInput.magnitude > 0.1f)
            visibility += 2f;

        if (isHidden)
            visibility = 0f;
    }
}