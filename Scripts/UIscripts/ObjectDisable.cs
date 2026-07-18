using UnityEngine;

public class DisableOnOtherInteract : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Drag the object that has the InteractableObjectsUI script here.")]
    [SerializeField] private InteractableObjectsUI targetInteractable;

    private void OnEnable()
    {
        if (targetInteractable != null)
        {
            // Subscribe to the interaction event
            targetInteractable.OnInteractionTriggered += HandleTargetInteracted;
        }
    }

    private void OnDisable()
    {
        if (targetInteractable != null)
        {
            // Always unsubscribe to prevent memory leaks
            targetInteractable.OnInteractionTriggered -= HandleTargetInteracted;
        }
    }

    private void HandleTargetInteracted()
    {
        // Turn off the GameObject this script is attached to
        gameObject.SetActive(false);
    }
}