using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class OilPickup : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    [Header("Refill Settings")]
    public float oilAmount = 50f;

    [Tooltip("Check this for standard separate items that disappear. Uncheck this if you want an infinite refill station (like an oil barrel) that stays in the world forever!")]
    [SerializeField] private bool destroyOnUse = true;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip pickupSFX;
    [SerializeField][Range(0f, 1f)] private float volume = 0.8f;

    [Header("Timing Settings")]
    [SerializeField]
    [Tooltip("How long (in seconds) to wait after the sound starts before the oil meter actually refills.")]
    private float refillDelay = 0.6f;

    private Collider playerInZone = null;
    private bool isProcessing = false; // Prevents button spamming during the active audio delay

    void Awake()
    {
        // Setup input locally but DO NOT enable it globally yet
        inputActions = new InputSystem_Actions();
    }

    void OnDisable()
    {
        // Safe unbinding cleanup if the object is disabled or destroyed
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed -= OnInteractPerformed;
            inputActions.Disable();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
       
        if (playerInZone != null && !isProcessing)
        {
            LanternSystem lantern = playerInZone.GetComponent<LanternSystem>();

            if (lantern == null)
            {
                lantern = playerInZone.GetComponentInChildren<LanternSystem>();
            }

            if (lantern != null)
            {
                // Stop the interaction if the lantern's tank is already full
                if (lantern.currentOil >= lantern.maxOil)
                {
                    Debug.Log("خزان الزيت ممتلئ بالفعل! 🛢️");
                    return;
                }

                StartCoroutine(RefillSequence(lantern));
            }
        }
    }

    private IEnumerator RefillSequence(LanternSystem lantern)
    {
        isProcessing = true;

        // 1. Play the audio clip instantly at this position
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, volume);
        }

        // 2. Visual handling based on single-use items
        if (destroyOnUse)
        {
            Collider myCollider = GetComponent<Collider>();
            if (myCollider != null) myCollider.enabled = false;

            MeshRenderer myRenderer = GetComponent<MeshRenderer>();
            if (myRenderer != null) myRenderer.enabled = false;

            foreach (MeshRenderer childRenderer in GetComponentsInChildren<MeshRenderer>())
            {
                childRenderer.enabled = false;
            }
        }

        // 3. Wait for the audio sweet-spot timing delay
        yield return new WaitForSeconds(refillDelay);

        // 4. Grant the oil fuel points to the player
        lantern.RefillOil(oilAmount);
        Debug.Log("تم تعبئة الزيت ⛽");

        // 5. Final lifecycle handling
        if (destroyOnUse)
        {
            Destroy(gameObject);
        }
        else
        {
            // If it's an infinite station, clear the lock so it can be used again next time!
            isProcessing = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = other;

            // 🛠️ FIX: Only enable the input listeners when the player is actively standing inside this specific item zone!
            inputActions.Enable();
            inputActions.Player.Interact.performed += OnInteractPerformed;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = null;

            // 🛠️ FIX: Safely completely isolate and disconnect inputs when the player walks away
            inputActions.Player.Interact.performed -= OnInteractPerformed;
            inputActions.Disable();
        }
    }
}