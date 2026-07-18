using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class EndGameTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name of your title screen scene in the Build Settings.")]
    [SerializeField] private string titleSceneName = "TitleScreen";
    
    [Header("Dialogue Settings")]
    [Tooltip("The dialogue data to play before the game ends.")]
    [SerializeField] private DialogueData endingDialogue;

    [Header("Timing & Fading")]
    [Tooltip("How long the fade to black should take (in seconds).")]
    [SerializeField] private float fadeDuration = 2f;
    [Tooltip("How long to wait after the screen goes black before loading the scene.")]
    [SerializeField] private float delayAfterFade = 0.5f;

    private bool hasTriggered = false;
    private PlayerMovement triggeredPlayer;

    private void OnTriggerEnter(Collider other)
    {
        // Prevent the trigger from firing multiple times
        if (hasTriggered) return;

        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            hasTriggered = true;
            triggeredPlayer = player;
            
            // Check if we have dialogue to play
            if (DialogueManager.Instance != null && endingDialogue != null)
            {
                // 1. Subscribe to the event so we know when the dialogue finishes
                DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
                
                // 2. Start the dialogue (This will automatically lock controls via DialogueManager)
                DialogueManager.Instance.StartDialogue(endingDialogue, triggeredPlayer);
            }
            else
            {
                // Fallback: If no dialogue is assigned, just skip straight to fading
                triggeredPlayer.SetControlsLocked(true);
                StartCoroutine(FadeAndLoadScene());
            }
        }
    }

    private void HandleDialogueEnded()
    {
        // 3. Unsubscribe immediately to prevent memory leaks or double-firing
        DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        
        // 4. Re-lock the player's controls (Because DialogueManager just unlocked them)
        if (triggeredPlayer != null)
        {
            triggeredPlayer.SetControlsLocked(true);
        }

        // 5. Start the fade out sequence
        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        // 6. Trigger the fade out, passing in the custom fadeDuration
        if (ScreenFader.Instance != null)
        {
            ZoneAudioManager.Instance.FadeOutBeforeSceneLoad(2f);
            yield return StartCoroutine(ScreenFader.Instance.FadeOut(fadeDuration));
        }
        else
        {
            Debug.LogWarning("ScreenFader.Instance not found! Simulating fade delay.");
            yield return new WaitForSeconds(fadeDuration);
        }

        // Optional slight pause while the screen is pitch black
        if (delayAfterFade > 0f)
        {
            yield return new WaitForSeconds(delayAfterFade);
        }

        // 7. Load the title screen
        SceneManager.LoadScene(titleSceneName);
    }

    private void OnDestroy()
    {
        // Safety check: Ensure we unsubscribe if this object is destroyed before dialogue ends
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }
}