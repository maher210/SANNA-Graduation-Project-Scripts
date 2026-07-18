using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels (Canvas Groups)")]
    [Tooltip("Drag the MainMenuPanel here")]
    public CanvasGroup mainMenuGroup;
    [Tooltip("Drag the SettingsPanel here")]
    public CanvasGroup settingsGroup;

    [Header("Scene Transition (Optional)")]
    [Tooltip("Highly Recommended: Drag a full-screen black Image UI panel with a CanvasGroup here. If left empty, the menu will just fade out.")]
    public CanvasGroup sceneFaderGroup;
    [Tooltip("How fast the screen fades out when pressing Play")]
    public float gameStartFadeDuration = 0.2f;

    [Header("Default Selected Buttons")]
    public GameObject mainFirstButton;
    public GameObject settingsFirstButton;

    [Header("Settings")]
    public string gameSceneName = "MainScene";
    public float fadeDuration = 0.3f; // How long menu cross-fades take

    [Header("Audio")]
    public AudioSource menuMusic;
    public float musicFadeDuration = 1.5f;

    private void Start()
    {
        // Ensure Main Menu is visible and Settings is hidden on start
        mainMenuGroup.alpha = 1;
        mainMenuGroup.interactable = true;
        mainMenuGroup.blocksRaycasts = true;

        settingsGroup.alpha = 0;
        settingsGroup.interactable = false;
        settingsGroup.blocksRaycasts = false;

        // Setup the scene fader if you provided one
        if (sceneFaderGroup != null)
        {
            sceneFaderGroup.alpha = 0f;
            sceneFaderGroup.interactable = false;
            sceneFaderGroup.blocksRaycasts = false;
            sceneFaderGroup.gameObject.SetActive(true);
        }
    }

    // --- MENU NAVIGATION ---

    public void OpenSettings()
    {
        StopAllCoroutines();
        StartCoroutine(CrossFadePanels(mainMenuGroup, settingsGroup, settingsFirstButton));
    }

    public void CloseSettings()
    {
        StopAllCoroutines();
        StartCoroutine(CrossFadePanels(settingsGroup, mainMenuGroup, mainFirstButton));
    }

    // --- THE ANIMATION LOGIC ---

    private IEnumerator CrossFadePanels(CanvasGroup panelToHide, CanvasGroup panelToShow, GameObject buttonToSelect)
    {
        panelToHide.interactable = false;
        panelToHide.blocksRaycasts = false;
        panelToShow.interactable = false;
        panelToShow.blocksRaycasts = false;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float percent = timer / fadeDuration;

            panelToHide.alpha = 1f - percent;
            panelToShow.alpha = percent;

            yield return null;
        }

        panelToHide.alpha = 0f;
        panelToShow.alpha = 1f;

        panelToShow.interactable = true;
        panelToShow.blocksRaycasts = true;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonToSelect);
    }

    // --- GAME LOGIC ---

    public void StartGame()
    {
        // Stop any menu navigation transitions and start the scene exit transition
        StopAllCoroutines();
        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        // 1. Instantly lock down the menu UI so the player can't spam click buttons during the fade
        mainMenuGroup.interactable = false;
        mainMenuGroup.blocksRaycasts = false;

        float timer = 0f;

        // Determine if we are fading a black screen overlay IN or fading the main menu OUT
        bool usingSceneFader = sceneFaderGroup != null;
        CanvasGroup targetGroup = usingSceneFader ? sceneFaderGroup : mainMenuGroup;

        if (usingSceneFader)
        {
            targetGroup.blocksRaycasts = true; // Block inputs with the black fader
        }

        // 2. Fast fade loop
        while (timer < gameStartFadeDuration)
        {
            timer += Time.deltaTime;
            float percent = timer / gameStartFadeDuration;

            if (usingSceneFader)
            {
                targetGroup.alpha = percent; // Fades black overlay from 0 to 1
            }
            else
            {
                targetGroup.alpha = 1f - percent; // Fades main menu text from 1 to 0
            }

            yield return null;
        }

        // 3. Ensure final values are strictly set
        targetGroup.alpha = usingSceneFader ? 1f : 0f;

        if (menuMusic != null)
        {
            float startVolume = menuMusic.volume;

            float musicTimer = 0f;

            while (musicTimer < musicFadeDuration)
            {
                musicTimer += Time.deltaTime;

                menuMusic.volume =
                    Mathf.Lerp(
                        startVolume,
                        0f,
                        musicTimer / musicFadeDuration
                    );

                yield return null;
            }
        }

        // 4. Load the actual game scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}