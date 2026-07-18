using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [Header("Intensity Settings")]
    [SerializeField] private float minIntensity = 0.1f;
    [SerializeField] private float maxIntensity = 2.0f;

    [Header("Speed Settings")]
    [Tooltip("Minimum time between light changes.")]
    [SerializeField] private float minDelay = 0.02f;
    [Tooltip("Maximum time between light changes.")]
    [SerializeField] private float maxDelay = 0.2f;

    private Light spotlight;

    void Start()
    {

        spotlight = GetComponent<Light>();

        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {

            spotlight.intensity = Random.Range(minIntensity, maxIntensity);


            float randomDelay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(randomDelay);
        }
    }
}