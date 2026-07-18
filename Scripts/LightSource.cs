using UnityEngine;

public class LightSource : MonoBehaviour, IInteractable
{
    [SerializeField]
    private float detectionRadius = 8f;

    public float DetectionRadius => detectionRadius;

    public bool IsOn => isOn;
    
        [Header("Light Settings")]
    [SerializeField] private GameObject lightObject;
    [SerializeField] private bool isOn = true;

    [Header("Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip breakSound;

    private bool isDestroyed = false;

    private void Start()
    {
        ApplyState();
    }

    // ========================
    // INTERACT
    // ========================
    public void Interact(PlayerMovement player)
    {
        if (isDestroyed)
            return;

        PlayerAnimationController anim = player.GetComponent<PlayerAnimationController>();
        if (anim != null)
        {
            anim.PlayBreak();
        }

        DestroyLight();
    }

    public string GetPrompt()
    {
        return isDestroyed ? null : "Press E to break light";
    }

    // ========================
    // LOGIC
    // ========================
    private void DestroyLight()
    {
        isDestroyed = true;
        isOn = false;

        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        ApplyState();

        Debug.Log("Light destroyed 💡❌");
    }

    private void ApplyState()
    {
        if (lightObject != null)
            lightObject.SetActive(isOn);
    }

    // ========================
    // RESET (للمستقبل)
    // ========================
    public void ResetLight()
    {
        isDestroyed = false;
        isOn = true;

        ApplyState();
    }



    // اضافة من ماهر
    void OnTriggerStay(Collider other)
    {
        if (!isOn) return; // إذا الضوء مكسور ما يأثر

        if (other.CompareTag("Player"))
        {
            PlayerVisibility vis = other.GetComponent<PlayerVisibility>();
            if (vis != null)
            {
                vis.inLight = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerVisibility vis = other.GetComponent<PlayerVisibility>();
            if (vis != null)
            {
                vis.inLight = false;
            }
        }
    }
}