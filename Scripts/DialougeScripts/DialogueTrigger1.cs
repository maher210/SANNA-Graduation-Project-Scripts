using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueData dialogue;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (!other.TryGetComponent(out PlayerMovement player))
            return;

        triggered = true;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogue, player);
        }
    }
}