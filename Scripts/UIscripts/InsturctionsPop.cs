using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class InstructionsPop : MonoBehaviour
{
    private PlayerMovement player;
    private InputSystem_Actions inputActions;

    [Header("UI Settings")]
    [Tooltip("The parent transform or image that will scale up/down.")]
    [SerializeField] private Transform uiImageContainer;
    [Tooltip("Canvas group to handle the fading effect.")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private float animationDuration = 0.3f;

    [Header("Trigger Settings")]
    [Tooltip("If true, the image will only pop up the first time the player enters the trigger.")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip popSound;
    [SerializeField][Range(0f, 1f)] private float volume = 0.7f;
    [Tooltip("Time in seconds to wait AFTER playing the sound BEFORE the visual UI animation scales up.")]
    [SerializeField] private float soundPreDelay = 0f;

    private bool isVisible = false;
    private bool isAnimating = false;
    private bool hasTriggered = false;

    private Coroutine animationCoroutine;
    private AudioSource audioSource;

    private void Awake()
    {
        if (uiImageContainer != null)
            uiImageContainer.localScale = Vector3.zero;

        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = 0f;

        // Automatically bind the required component
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
        }
        inputActions.Enable();
        inputActions.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.performed -= OnInteractPerformed;
            inputActions.Disable();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || (triggerOnce && hasTriggered))
            return;

        player = other.GetComponent<PlayerMovement>();

        if (player == null)
        {
            player = other.GetComponentInParent<PlayerMovement>();
        }

        if (player != null && !isVisible && !isAnimating)
        {
            hasTriggered = true;
            ShowImage();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isVisible)
        {
            player = null;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (isVisible)
        {
            HideImage();
        }
    }

    private void ShowImage()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ShowSequence());
    }

    private void HideImage()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        // Instantly kill any lingering sound trail if the player skips/closes early
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        animationCoroutine = StartCoroutine(AnimateUI(false));
    }

    private IEnumerator ShowSequence()
    {
        isAnimating = true;
        LockPlayer(true);

        // Play the audio cue using the exposed volume slider
        if (audioSource != null && popSound != null)
        {
            audioSource.PlayOneShot(popSound, volume);
        }

        if (soundPreDelay > 0f)
        {
            yield return new WaitForSeconds(soundPreDelay);
        }

        yield return StartCoroutine(AnimateUI(true));
    }

    private IEnumerator AnimateUI(bool show)
    {
        isAnimating = true;
        if (!show) isVisible = false;

        float elapsedTime = 0f;
        float startAlpha = uiCanvasGroup != null ? uiCanvasGroup.alpha : 0f;
        float targetAlpha = show ? 1f : 0f;

        Vector3 startScale = uiImageContainer != null ? uiImageContainer.localScale : Vector3.zero;
        Vector3 targetScale = show ? Vector3.one : Vector3.zero;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float smoothPercentage = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);

            if (uiCanvasGroup != null)
                uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothPercentage);

            if (uiImageContainer != null)
                uiImageContainer.localScale = Vector3.Lerp(startScale, targetScale, smoothPercentage);

            yield return null;
        }

        if (uiCanvasGroup != null) uiCanvasGroup.alpha = targetAlpha;
        if (uiImageContainer != null) uiImageContainer.localScale = targetScale;

        isAnimating = false;

        if (show)
        {
            isVisible = true;
        }
        else
        {
            LockPlayer(false);

            if (triggerOnce)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void LockPlayer(bool state)
    {
        if (player != null)
        {
            player.SetControlsLocked(state);
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    player.SetControlsLocked(state);
                    return;
                }
            }
            Debug.LogWarning($"Player reference missing on {gameObject.name}! Could not lock/unlock controls.");
        }
    }
}