using System.Collections;
using UnityEngine;

public class SceneFadeIn : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Drag the CanvasGroup of your full-screen black image here.")]
    public CanvasGroup fadeCanvasGroup;
    
    [Tooltip("How long the fade-in takes in seconds")]
    public float fadeDuration = 0.4f;

    private void Start()
    {
        if (fadeCanvasGroup != null)
        {
            // Force the screen to start completely black immediately
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = true;

            // Kick off the fade-in routine
            StartCoroutine(FadeInRoutine());
        }
        else
        {
            Debug.LogError($"[SceneFadeIn] Fade Canvas Group is missing on {gameObject.name}!");
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            
            // Calculate percentage and fade from 1 (black) down to 0 (transparent)
            float percent = timer / fadeDuration;
            fadeCanvasGroup.alpha = 1f - percent;

            yield return null; // Wait for the next frame
        }

        // Lock in the final values
        fadeCanvasGroup.alpha = 0f;

        // CRITICAL: Disable raycasts so the invisible overlay doesn't block player clicks!
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        // Completely disable the object to save rendering performance
        fadeCanvasGroup.gameObject.SetActive(false);
    }
}