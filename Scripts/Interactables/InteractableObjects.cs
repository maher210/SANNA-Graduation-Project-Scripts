using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Should this object only be interactable once?")]
    [SerializeField] private bool isSingleUse = true;
    [Tooltip("What happens when the player interacts with this object?")]
    public UnityEvent onInteract;

    [Header("Floating Prompt Settings (World Space)")]
    [Tooltip("Assign a CanvasGroup that is a CHILD of this object. Do not share one Canvas between multiple objects!")]
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [Tooltip("Manual position offset for the interaction prompt.")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);
    [Tooltip("Manual fixed rotation for the interaction prompt.")]
    [SerializeField] private Vector3 promptRotation = Vector3.zero;
    [SerializeField] private float promptFadeDuration = 0.15f;
    [Tooltip("Target scale for the prompt when visible. (Default is 1,1,1)")]
    [SerializeField] private Vector3 visibleScale = Vector3.one;

    [Header("Global Animation Settings")]
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float floatAmount = 0.15f;

    [Header("Interaction Audio Clips")]
    [SerializeField] private AudioClip interactSFX;

    [Header("Audio Volume Settings")]
    [SerializeField][Range(0f, 1f)] private float interactVolume = 0.8f;

    private InputSystem_Actions inputActions;
    private bool playerInside = false;
    private bool isExhausted = false;
    private Coroutine promptFadeCoroutine;
    private AudioSource audioSource;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Interact.performed += OnInteractPerformed;

        audioSource = GetComponent<AudioSource>();

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.transform.localScale = Vector3.zero;
        }
    }

    private void OnEnable()
    {
        if (playerInside) inputActions.Enable();

        // 🔗 Listen to the global static player death event
        PlayerMovement.OnAnyPlayerDeath += ResetInteractableState;
    }

    private void OnDisable()
    {
        inputActions.Disable();

        // 🛑 Unsubscribe from the global static event to avoid memory leaks
        PlayerMovement.OnAnyPlayerDeath -= ResetInteractableState;
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed -= OnInteractPerformed;
        }
    }

    // 💀 NEW: Automatically called across EVERY interactable in your level when the player dies
    private void ResetInteractableState()
    {
        isExhausted = false;
        playerInside = false;

        // Instantly hide the UI prompt without waiting for an active fade coroutine
        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.transform.localScale = Vector3.zero;
        }

        // Shut down input processing completely until the fresh player walks back into the trigger
        inputActions.Disable();
    }

    private void Update()
    {
        if (isExhausted || !playerInside) return;

        HandleFloatingPromptAnimation();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!playerInside || isExhausted) return;

        if (interactSFX != null)
        {
            AudioSource.PlayClipAtPoint(interactSFX, transform.position, interactVolume);
        }

        onInteract?.Invoke();

        if (isSingleUse)
        {
            isExhausted = true;
            TogglePrompt(false);
            inputActions.Disable();
        }
    }

    private void HandleFloatingPromptAnimation()
    {
        if (promptCanvasGroup == null || promptCanvasGroup.alpha <= 0.01f) return;

        // Uses time-independent tracking to avoid pause menu physics breaking the bob animation
        Vector3 targetBasePos = transform.position + promptOffset;
        float bobbingOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;

        promptCanvasGroup.transform.position = targetBasePos + Vector3.up * bobbingOffset;
        promptCanvasGroup.transform.rotation = Quaternion.Euler(promptRotation);
    }

    private void TogglePrompt(bool show)
    {
        if (promptCanvasGroup == null) return;

        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);

        float targetAlpha = show ? 1f : 0f;
        Vector3 targetScale = show ? visibleScale : Vector3.zero;

        promptFadeCoroutine = StartCoroutine(PromptFadeRoutine(targetAlpha, targetScale));
    }

    private IEnumerator PromptFadeRoutine(float targetAlpha, Vector3 targetScale)
    {
        float elapsedTime = 0f;
        float startAlpha = promptCanvasGroup.alpha;
        Vector3 startScale = promptCanvasGroup.transform.localScale;

        while (elapsedTime < promptFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float smoothPercentage = Mathf.SmoothStep(0f, 1f, elapsedTime / promptFadeDuration);

            promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothPercentage);
            promptCanvasGroup.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothPercentage);

            yield return null;
        }

        promptCanvasGroup.alpha = targetAlpha;
        promptCanvasGroup.transform.localScale = targetScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isExhausted || !other.CompareTag("Player")) return;

        playerInside = true;
        inputActions.Enable();

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.transform.position = transform.position + promptOffset;
            promptCanvasGroup.transform.rotation = Quaternion.Euler(promptRotation);
            TogglePrompt(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        inputActions.Disable();

        if (!isExhausted)
        {
            TogglePrompt(false);
        }
    }
}