using UnityEngine;
using System.Collections.Generic;

public class MaterialPulseCustomTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("The specific collider designated as the interaction zone. MUST have 'Is Trigger' checked.")]
    [SerializeField] private Collider specificTrigger;

    [Header("Target Settings")]
    [Tooltip("If empty, it will automatically find ALL renderers on this object and its children.")]
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private Color glowColor = Color.white;

    private List<Material> targetMaterials = new List<Material>();
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // A private internal component used to redirect physics events from the specific trigger
    private TriggerListener listener;

    private void Awake()
    {
        // 1. Verify and setup the custom trigger
        if (specificTrigger == null)
        {
            Debug.LogError($"[MaterialPulse] Please assign a Specific Trigger collider on {gameObject.name}!", this);
            enabled = false;
            return;
        }

        if (!specificTrigger.isTrigger)
        {
            Debug.LogWarning($"[MaterialPulse] The assigned collider '{specificTrigger.name}' did not have 'Is Trigger' checked. Enabling it automatically.", specificTrigger);
            specificTrigger.isTrigger = true;
        }

        // 2. Attach a helper component to the trigger object so it can report back to this script
        listener = specificTrigger.gameObject.AddComponent<TriggerListener>();
        listener.Initialize(OnPlayerEnter, OnPlayerExit);

        // 3. Find and cache all renderers/materials
        if (targetRenderers.Count == 0)
        {
            targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
        }

        foreach (Renderer rend in targetRenderers)
        {
            if (rend != null)
            {
                targetMaterials.Add(rend.material);
            }
        }

        // Start disabled so Update() doesn't consume CPU cycles until the player arrives
        enabled = false;
    }

    private void Update()
    {
        ExecutePulse();
    }

    private void ExecutePulse()
    {
        if (targetMaterials.Count == 0) return;

        float rawSin = Mathf.Sin(Time.time * pulseSpeed);
        float normalizedSin = (rawSin + 1f) / 2f;
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, normalizedSin);

        foreach (Material mat in targetMaterials)
        {
            if (mat != null)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(EmissionColorID, glowColor * currentIntensity);
            }
        }
    }

    private void OnPlayerEnter()
    {
        enabled = true; // Wake up the Update loop
    }

    private void OnPlayerExit()
    {
        enabled = false; // Sleep the Update loop
        ResetEmission();
    }

    private void ResetEmission()
    {
        foreach (Material mat in targetMaterials)
        {
            if (mat != null)
            {
                mat.SetColor(EmissionColorID, Color.clear);
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Material mat in targetMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
    }
}

// --- HELPER COMPONENT ---
// This hidden class lives on the trigger object to forward physics entries back to our main script
internal class TriggerListener : MonoBehaviour
{
    private System.Action onEnter;
    private System.Action onExit;

    public void Initialize(System.Action enterCallback, System.Action exitCallback)
    {
        onEnter = enterCallback;
        onExit = exitCallback;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onEnter?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onExit?.Invoke();
        }
    }
}