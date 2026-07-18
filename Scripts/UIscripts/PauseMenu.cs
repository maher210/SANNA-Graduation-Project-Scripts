using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The GameObject containing your Settings/Pause Menu Canvas")]
    public GameObject settingsPanel;
    [Tooltip("The CanvasGroup component attached to the Settings Panel")]
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float animationDuration = 0.25f;
    
    [Header("Scene Settings")]
    [Tooltip("The exact name of your title screen scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private Coroutine currentAnimation;

    void Start()
    {
        // Ensure the menu is hidden and game is unpaused when the scene starts
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Listen for the Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // Called by Escape key
    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze the game
        
        settingsPanel.SetActive(true);
        
        // Stop any currently running fade animation
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateMenu(1f, 1f)); // Fade in, scale to 1
        
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    // Can be called by Escape key OR the Resume Button
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Unfreeze the game
        
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateMenu(0f, 0.9f)); // Fade out, scale down to 0.9
    }

    // Can be called by the Main Menu Button
    public void GoToMainMenu()
    {
        // Make sure time is unscaled before changing scenes, 
        // otherwise physics/animations might break in the next scene
        Time.timeScale = 1f; 
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // The Coroutine that handles the smooth fade and scale animation
    private IEnumerator AnimateMenu(float targetAlpha, float targetScale)
    {
        float time = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = settingsPanel.transform.localScale;
        Vector3 endScale = new Vector3(targetScale, targetScale, 1f);

        while (time < animationDuration)
        {
            // Use unscaledDeltaTime so the animation runs even when Time.timeScale == 0
            time += Time.unscaledDeltaTime;
            
            // SmoothStep for a more polished easing effect
            float t = Mathf.Clamp01(time / animationDuration);
            t = t * t * (3f - 2f * t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            settingsPanel.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        settingsPanel.transform.localScale = endScale;

        // If fading out is complete, completely disable the panel to save performance
        if (targetAlpha <= 0f)
        {
            settingsPanel.SetActive(false);
        }
    }
}