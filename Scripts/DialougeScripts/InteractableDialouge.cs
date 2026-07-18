using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInteractable : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Floating Prompt Settings (World Space)")]
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [Tooltip("Manual position offset for the interaction prompt.")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);
    [Tooltip("Manual fixed rotation for the interaction prompt.")]
    [SerializeField] private Vector3 promptRotation = Vector3.zero;
    [SerializeField] private float promptFadeDuration = 0.15f;

    [Header("Global Animation Settings")]
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float floatAmount = 0.15f;

    private PlayerMovement player;
    private InputSystem_Actions inputActions;

    private bool playerInside = false;
    private Coroutine promptFadeCoroutine;
    private Vector3 initialPromptScale;

    private void Awake()
    {
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

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.transform.localScale = Vector3.zero;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Disable();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }

    private void Update()
    {
        HandleFloatingPromptAnimation();
        HandlePromptRotation();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!playerInside || DialogueManager.Instance.IsActive)
            return;

        TriggerDialogue();
    }

    private void TriggerDialogue()
    {
        if (dialogueData == null)
        {
            Debug.LogWarning($"{gameObject.name} has no DialogueData assigned!");
            return;
        }

        // Hide floating prompt while conversation is running
        TogglePrompt(false);

        DialogueManager.Instance.StartDialogue(dialogueData, player);
    }

    private void HandleDialogueEnded()
    {
        // If player is still standing inside the zone when dialogue finishes, bring the prompt back up
        if (playerInside)
        {
            TogglePrompt(true);
        }
    }

    private void HandleFloatingPromptAnimation()
    {
        if (promptCanvasGroup == null || promptCanvasGroup.alpha <= 0.01f || !playerInside) return;

        // Bobs the prompt up and down based on the offset
        Vector3 targetBasePos = transform.position + promptOffset;
        float bobbingOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        promptCanvasGroup.transform.position = targetBasePos + Vector3.up * bobbingOffset;
    }

    private void HandlePromptRotation()
    {
        if (promptCanvasGroup == null || promptCanvasGroup.alpha <= 0.01f) return;

        // Applies your precise manual rotation values from the inspector
        promptCanvasGroup.transform.rotation = Quaternion.Euler(promptRotation);
    }

    private void TogglePrompt(bool show)
    {
        if (promptCanvasGroup == null) return;

        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);

        float targetAlpha = show ? 1f : 0f;
        Vector3 targetScale = show ? initialPromptScale : Vector3.zero;

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
        if (!other.CompareTag("Player") || DialogueManager.Instance.IsActive) return;

        player = other.GetComponent<PlayerMovement>();
        playerInside = true;

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
        TogglePrompt(false);
    }
}