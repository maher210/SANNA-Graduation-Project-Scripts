using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the black UI Image used for fading here.")]
    public Image fadeOverlay;

    [Header("Settings")]
    [Tooltip("How long the fade takes in seconds.")]
    public float fadeDuration = 1.5f;

    private void Start()
    {
        // Automatically start fading in when the scene loads
        if (fadeOverlay != null)
        {
            StartCoroutine(FadeIn());
        }
        else
        {
            Debug.LogWarning("Fade Overlay Image is not assigned in the SceneTransitionManager!");
        }
    }

    /// <summary>
    /// Call this method from buttons, triggers, or other scripts to leave the scene.
    /// </summary>
    /// <param name="sceneName">The exact string name of the next scene.</param>
    public void LoadNextScene(string sceneName)
    {
        StartCoroutine(FadeOut(sceneName));
    }

    private IEnumerator FadeIn()
    {
        fadeOverlay.gameObject.SetActive(true);
        Color overlayColor = fadeOverlay.color;
        overlayColor.a = 1f; // Start completely opaque (pitch black)
        fadeOverlay.color = overlayColor;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // Calculate the alpha from 1 to 0
            float alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            fadeOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            
            yield return null; // Wait until the next frame
        }

        // Disable the image so it doesn't block UI raycasts (like clicking buttons)
        fadeOverlay.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut(string sceneName)
    {
        fadeOverlay.gameObject.SetActive(true);
        Color overlayColor = fadeOverlay.color;
        overlayColor.a = 0f; // Start completely transparent
        fadeOverlay.color = overlayColor;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // Calculate the alpha from 0 to 1
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
            
            yield return null;
        }

        // Once the screen is fully black, load the next scene
        SceneManager.LoadScene(sceneName);
    }
}