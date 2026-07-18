using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System; 

[RequireComponent(typeof(AudioSource))] 
public class InteractableObjectsUI : MonoBehaviour
{
    public event Action OnInteractionTriggered;

    private PlayerMovement player;

    [Header("UI Settings (Inspection Menu)")]
    [SerializeField] private Image uiImageContainer;
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Exclamation Mark Settings (World Space)")]
    [Tooltip("The World Space Canvas/GameObject for the exclamation mark.")]
    [SerializeField] private GameObject exclamationMark;
    [Tooltip("Manual position offset for the exclamation mark.")]
    [SerializeField] private Vector3 exclamationOffset = new Vector3(0f, 2.5f, 0f);
    [Tooltip("Should the exclamation mark bob up and down?")]
    [SerializeField] private bool animateExclamation = true;
    [Tooltip("Should the exclamation mark rotate to face the camera?")]
    [SerializeField] private bool rotateExclamationToCamera = true;

    [Header("Floating Prompt Settings (World Space)")]
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [Tooltip("Manual position offset for the interaction prompt.")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.5f, 0f);
    [Tooltip("Manual rotation offset for the interaction prompt.")]
    [SerializeField] private Vector3 promptRotationOffset;
    [SerializeField] private float promptFadeDuration = 0.15f;

    [Header("Global Animation Settings")]
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float floatAmount = 0.15f;

    [Header("Interaction")]
    [SerializeField] private Sprite interactionSprite;

    [Header("Interaction Audio Clips")]
    [SerializeField] private AudioClip promptAppearSFX; 
    [SerializeField] private AudioClip openUiSFX;       
    [SerializeField] private AudioClip closeUiSFX;      

    [Header("Audio Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float promptVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float openUiVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float closeUiVolume = 0.7f;

    // 🛠️ NEW: Control exactly how many seconds the audio plays before the UI begins fading onto the screen
    [Header("Timing Settings")]
    [SerializeField] [Tooltip("Time in seconds to wait after the opening sound starts playing before the UI window visual fade begins.")]
    private float uiOpenDelay = 0.12f;

    private InputSystem_Actions inputActions;

    private bool playerInside = false;
    private bool isUiVisible = false;
    private bool isExhausted = false;

    private Coroutine fadeCoroutine;
    private Coroutine promptFadeCoroutine;
    private Coroutine uiSequenceCoroutine; // 🛠️ NEW: Tracks our timed entry routine

    private Vector3 initialPromptScale;
    private Camera mainCamera;
    private AudioSource audioSource; 

    private void Awake()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>(); 

        if (promptCanvasGroup != null)
        {
            initialPromptScale = Vector3.one;
        }
    }

    private void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Interact.performed += OnInteractPerformed;

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0f;
            uiImageContainer.transform.localScale = Vector3.zero;
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.transform.localScale = Vector3.zero;
        }

        if (exclamationMark != null && !isExhausted)
        {
            exclamationMark.SetActive(true);
        }
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Disable();
    }

    private void Update()
    {
        if (isExhausted) return;

        HandleExclamationMark();
        HandleFloatingPromptAnimation();
        HandlePromptRotation();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!playerInside || isExhausted)
            return;

        ToggleUI();
    }

    private void ToggleUI()
    {
        // Safely cancel any active transitions running in the background
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (uiSequenceCoroutine != null) StopCoroutine(uiSequenceCoroutine);

        if (isUiVisible)
        {
            // Closing the UI (Instant cleanup loop execution)
            fadeCoroutine = StartCoroutine(FadeRoutine(0f, Vector3.zero));
            isUiVisible = false;
            isExhausted = true;

            if (closeUiSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(closeUiSFX, closeUiVolume);
            }

            TogglePrompt(false);
            PauseGame(false);
        }
        else
        {
            // Opening the UI (Passed off entirely to our delayed sequence routine)
            if (interactionSprite != null)
            {
                uiSequenceCoroutine = StartCoroutine(OpenUiSequenceRoutine());
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} has no interaction sprite assigned!");
            }
        }
    }

    // 🛠️ NEW SEQUENCING ROUTINE: Safely splits input registration, audio, and visual playback
    private IEnumerator OpenUiSequenceRoutine()
    {
        // 1. Instantly lock controls so player cannot move during the sound delay window
        PauseGame(true);
        TogglePrompt(false);

        if (exclamationMark != null)
        {
            exclamationMark.SetActive(false);
        }

        // 2. Fire off the interaction menu audio track immediately
        if (openUiSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(openUiSFX, openUiVolume);
        }

        // 3. Pause operations right here for your custom micro-second frame delay window
        yield return new WaitForSeconds(uiOpenDelay);

        // 4. Time up! Initialize the sprite container and step through the smooth UI fade in loop
        uiImageContainer.sprite = interactionSprite;
        fadeCoroutine = StartCoroutine(FadeRoutine(1f, Vector3.one));
        isUiVisible = true;

        OnInteractionTriggered?.Invoke();
        uiSequenceCoroutine = null;
    }

    private void HandleExclamationMark()
    {
        if (exclamationMark == null || !exclamationMark.activeSelf)
            return;

        Vector3 targetBasePos = transform.position + exclamationOffset;

        if (animateExclamation)
        {
            float bobbingOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            exclamationMark.transform.position = targetBasePos + Vector3.up * bobbingOffset;
        }
        else
        {
            transform.position = targetBasePos;
        }

        if (rotateExclamationToCamera && mainCamera != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - exclamationMark.transform.position;
            directionToCamera.y = 0f;

            if (directionToCamera != Vector3.zero)
            {
                exclamationMark.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }

    private void HandleFloatingPromptAnimation()
    {
        if (promptCanvasGroup == null || promptCanvasGroup.alpha <= 0.01f || !playerInside)
            return;

        Vector3 targetBasePos = transform.position + promptOffset;
        float bobbingOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        promptCanvasGroup.transform.position = targetBasePos + Vector3.up * bobbingOffset;
    }

    private void HandlePromptRotation()
    {
        if (promptCanvasGroup == null || promptCanvasGroup.alpha <= 0.01f)
            return;

        if (mainCamera == null)
            return;

        Vector3 directionToCamera = mainCamera.transform.position - promptCanvasGroup.transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
            targetRotation *= Quaternion.Euler(promptRotationOffset);
            promptCanvasGroup.transform.rotation = targetRotation;
        }
    }

    private void TogglePrompt(bool show)
    {
        if (promptCanvasGroup == null)
            return;

        if (promptFadeCoroutine != null)
            StopCoroutine(promptFadeCoroutine);

        float targetAlpha = show ? 1f : 0f;
        Vector3 targetScale = show ? initialPromptScale : Vector3.zero;

        promptFadeCoroutine = StartCoroutine(PromptFadeRoutine(targetAlpha, targetScale));
    }

    private IEnumerator FadeRoutine(float targetAlpha, Vector3 targetScale)
    {
        float elapsedTime = 0f;
        float startAlpha = uiCanvasGroup.alpha;
        Vector3 startScale = uiImageContainer.transform.localScale;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float smoothPercentage = Mathf.SmoothStep(0f, 1f, elapsedTime / fadeDuration);

            uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothPercentage);
            uiImageContainer.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothPercentage);

            yield return null;
        }

        uiCanvasGroup.alpha = targetAlpha;
        uiImageContainer.transform.localScale = targetScale;
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
        if (isExhausted || !other.CompareTag("Player"))
            return;

        player = other.GetComponent<PlayerMovement>();
        playerInside = true;

        if (promptCanvasGroup != null && !isUiVisible)
        {
            promptCanvasGroup.transform.position = transform.position + promptOffset;
            TogglePrompt(true);

            if (promptAppearSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(promptAppearSFX, promptVolume);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (!isExhausted)
        {
            TogglePrompt(false);
        }

        if (isUiVisible || uiSequenceCoroutine != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            if (uiSequenceCoroutine != null) StopCoroutine(uiSequenceCoroutine);

            fadeCoroutine = StartCoroutine(FadeRoutine(0f, Vector3.zero));
            isUiVisible = false;

            isExhausted = true;
            PauseGame(false);
        }
    }

    private void PauseGame(bool state)
    {
        if (player != null)
        {
            player.SetControlsLocked(state);
        }
        else
        {
            Debug.LogWarning("Player reference is missing! Could not lock/unlock controls.");
        }
    }
}