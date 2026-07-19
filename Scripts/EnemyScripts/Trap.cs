using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Trap : MonoBehaviour //IInteractable
{
    [Header("Trap Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private GameObject visual;
    [SerializeField] private float triggerDelay = 0.1f;

    [Header("Trap Audio Settings")]
    [SerializeField] private AudioClip triggerSFX;
    [SerializeField][Range(0f, 1f)] private float volume = 0.8f;

    private bool isDisarmed = false;
    public bool isTriggered = false;

    private AudioSource audioSource;

    private void Awake()
    {
        // Automatically bind the required component
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SetVisible(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isDisarmed || isTriggered)
            return;

        if (other.TryGetComponent(out IDamageable damageable))
        {
            StartCoroutine(TriggerTrap(damageable));
        }
    }

    private IEnumerator TriggerTrap(IDamageable target)
    {
        isTriggered = true;

        // 1. Give immediate audio feedback the millisecond they step on it
        if (audioSource != null && triggerSFX != null)
        {
            audioSource.PlayOneShot(triggerSFX, volume);
        }

        // 2. Wait out the tension delay
        yield return new WaitForSeconds(triggerDelay);

        // 3. Punish the mistake
        target.Die();
    }

    // ========================
    // INTERACT
    // ========================
    // public void Interact(PlayerMovement player)
    // {
    //     if (isDisarmed)
    //         return;
    // 
    //     DisarmTrap();
    // }
    // 
    // public string GetPrompt()
    // {
    //     return isDisarmed ? null : "Press E to disarm trap";
    // }
    // 
    // private void DisarmTrap()
    // {
    //     isDisarmed = true;
    //     isActive = false;
    // 
    //     if (visual != null)
    //         visual.SetActive(false);
    // }

    // ========================
    // VISIBILITY
    // ========================
    public void SetVisible(bool value)
    {
        if (visual != null)
            visual.SetActive(value && !isDisarmed);
    }

    // ========================
    // RESET (for future use)
    // ========================
    public void ResetTrap()
    {
        isActive = true;
        isDisarmed = false;
        isTriggered = false;

        SetVisible(true);
    }
}
