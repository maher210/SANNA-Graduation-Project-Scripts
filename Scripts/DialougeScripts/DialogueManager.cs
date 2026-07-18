using UnityEngine;
using UnityEngine.InputSystem;
using System; // Added for Action

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    // --- NEW: Events and Properties for synchronization ---
    public event Action OnDialogueEnded;
    public bool IsActive => isActive;

    private string[] sentences;
    private int index;
    private bool isActive;

    private PlayerMovement player;
    private InputSystem_Actions inputAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputAction = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputAction.Enable();
        inputAction.UI.GoNext.performed += OnNextSentence;
    }

    void OnDisable()
    {
        inputAction.UI.GoNext.performed -= OnNextSentence;
        inputAction.Disable();
    }

    private void OnNextSentence(InputAction.CallbackContext context)
    {
        if (!isActive) return;
        NextSentence();
    }

    public void StartDialogue(DialogueData data, PlayerMovement p)
    {
        player = p;
        sentences = data.sentences;
        index = 0;
        isActive = true;

        PauseGame(true);
        DialogueUI.Instance.Show(sentences[index]);
    }

    void NextSentence()
    {
        index++;

        if (index >= sentences.Length)
        {
            EndDialogue();
            return;
        }

        DialogueUI.Instance.Show(sentences[index]);
    }

    void EndDialogue()
    {
        isActive = false;
        DialogueUI.Instance.Hide();
        PauseGame(false);

        // --- NEW: Notify the interactable script that dialogue is over ---
        OnDialogueEnded?.Invoke();
    }

    void PauseGame(bool state)
    {
        if (player != null)
        {
            player.SetControlsLocked(state);
        }
    }
}