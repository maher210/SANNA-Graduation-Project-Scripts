using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class LanternSystem : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private PlayerMovement playerMovement;
    private float savedOilForRoom;

    [Header("Lantern Lights")]
    public Light lanternLight;
    public Light softLight;

    [Header("Oil Settings")]
    public float maxOil = 100f;
    [Tooltip("Set this to 0 (or whatever you want) in the Unity Inspector!")]
    public float currentOil = 0f;
    public float oilDrainRate = 5f;
    public bool isOn = false;

    [Header("Fade Settings")]
    public float fadeSpeed = 4f;
    public float lightIntensityON = 15f;
    public float lightIntensityOFF = 0f;
    [SerializeField] private float softLightIntensityON = 2f;

    [Header("Visibility Settings")]
    public PlayerVisibility playerVisibility;
    public float lightVisibility = 10f;
    public float darkVisibility = 0f;

    [Header("UI Settings")]
    public Image fullOilImage;
    public Image halfOilImage;
    public Image lowOilImage;

    public float halfThreshold = 0.6f;
    public float lowThreshold = 0.3f;

    [Header("Lantern Audio Clips")]
    [SerializeField] private AudioClip clickOnSFX;
    [SerializeField] private AudioClip lanternLoopSFX;
    [SerializeField] private AudioClip clickOffSFX;

    [Header("Audio Volume Settings")]
    [SerializeField][Range(0f, 1f)] private float clickOnVolume = 1.0f;
    [SerializeField][Range(0f, 1f)] private float lanternLoopVolume = 0.6f;
    [SerializeField][Range(0f, 1f)] private float clickOffVolume = 1.0f;

    [Header("Timing Settings")]
    [SerializeField]
    [Tooltip("Time in seconds to wait after the click sound starts before the light begins fading ON.")]
    private float lightOnDelay = 0.15f;

    private AudioSource audioSource;
    private Coroutine audioSequenceCoroutine;
    private Coroutine lightFadeCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
            inputActions.Player.Lantern.performed += OnLanternPress;
        }

        // 🔗 Hook into the player's death event
        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            // ⚡ CHANGED: Now calls the INSTANT turn-off routine on death
            playerMovement.OnPlayerDeath += TurnOffLanternInstant;
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Lantern.performed -= OnLanternPress;
            inputActions.Disable();
        }

        if (playerMovement != null)
        {
            // ⚡ CHANGED: Mirror cleanup for the instant turn-off routine
            playerMovement.OnPlayerDeath -= TurnOffLanternInstant;
        }
    }

    void Start()
    {
        if (lanternLight != null)
        {
            lanternLight.enabled = true;
            lanternLight.intensity = isOn ? lightIntensityON : lightIntensityOFF;
        }
        if (softLight != null)
        {
            softLight.enabled = true;
            softLight.intensity = isOn ? softLightIntensityON : 0f;
        }

        if (playerVisibility == null)
        {
            playerVisibility = FindAnyObjectByType<PlayerVisibility>();
        }

        if (playerVisibility != null)
        {
            playerVisibility.SetVisibility(isOn ? lightVisibility : darkVisibility);
        }

        UpdateUI();
    }

    void Update()
    {
        if (isOn && currentOil > 0)
        {
            currentOil -= oilDrainRate * Time.deltaTime;

            if (currentOil <= 0)
            {
                currentOil = 0;
                TurnOffLantern();
            }

            UpdateUI();
        }
    }

    public void SaveRoomOil()
    {
        savedOilForRoom = currentOil;
    }

    public void ResetLantern()
    {
        currentOil = savedOilForRoom;
        UpdateUI();
    }

    void OnLanternPress(InputAction.CallbackContext context)
    {
        ToggleLantern();
    }

    void ToggleLantern()
    {
        if (isOn)
            TurnOffLantern();
        else if (currentOil > 0)
            TurnOnLantern();
    }

    void TurnOnLantern()
    {
        isOn = true;
        StopAudioSequence();
        audioSequenceCoroutine = StartCoroutine(PlayLanternOnSequence());
    }

    // Normal smooth turn off (for gameplay toggles)
    void TurnOffLantern()
    {
        isOn = false;
        StopAudioSequence();

        if (audioSource != null)
        {
            audioSource.Stop();
            if (clickOffSFX != null)
            {
                audioSource.PlayOneShot(clickOffSFX, clickOffVolume);
            }
        }

        TriggerLightFade(lightIntensityOFF, 0f);
        if (playerVisibility != null) playerVisibility.SetVisibility(darkVisibility);
    }

    // 💀 NEW: Absolute instant kill switch for player death
    private void TurnOffLanternInstant()
    {
        isOn = false;

        // Forcefully terminate all running timing and fade coroutines right now
        StopAudioSequence();
        if (lightFadeCoroutine != null)
        {
            StopCoroutine(lightFadeCoroutine);
            lightFadeCoroutine = null;
        }

        // Hard-stop the audio track with no click-off delay sound
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        // Drop the physical light intensities to zero immediately
        if (lanternLight != null) lanternLight.intensity = lightIntensityOFF;
        if (softLight != null) softLight.intensity = 0f;

        // Drop stealth/visibility data immediately
        if (playerVisibility != null) playerVisibility.SetVisibility(darkVisibility);

        UpdateUI();
    }

    private IEnumerator PlayLanternOnSequence()
    {
        if (audioSource == null) yield break;

        if (clickOnSFX != null)
        {
            audioSource.volume = clickOnVolume;
            audioSource.clip = clickOnSFX;
            audioSource.loop = false;
            audioSource.Play();

            yield return new WaitForSeconds(lightOnDelay);
        }

        if (isOn && currentOil > 0)
        {
            TriggerLightFade(lightIntensityON, softLightIntensityON);
            if (playerVisibility != null) playerVisibility.SetVisibility(lightVisibility);
        }

        if (clickOnSFX != null)
        {
            float remainingClickTime = clickOnSFX.length - lightOnDelay;
            if (remainingClickTime > 0f)
            {
                yield return new WaitForSeconds(remainingClickTime);
            }
        }

        if (isOn && currentOil > 0 && lanternLoopSFX != null && audioSource != null)
        {
            audioSource.volume = lanternLoopVolume;
            audioSource.clip = lanternLoopSFX;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void TriggerLightFade(float targetMain, float targetSoft)
    {
        if (lightFadeCoroutine != null) StopCoroutine(lightFadeCoroutine);
        lightFadeCoroutine = StartCoroutine(FadeLightsRoutine(targetMain, targetSoft));
    }

    private IEnumerator FadeLightsRoutine(float targetMain, float targetSoft)
    {
        float timeElapsed = 0f;
        float startMain = lanternLight != null ? lanternLight.intensity : 0f;
        float startSoft = softLight != null ? softLight.intensity : 0f;

        while (timeElapsed < 1f)
        {
            timeElapsed += Time.deltaTime * fadeSpeed;

            if (lanternLight != null) lanternLight.intensity = Mathf.SmoothStep(startMain, targetMain, timeElapsed);
            if (softLight != null) softLight.intensity = Mathf.SmoothStep(startSoft, targetSoft, timeElapsed);

            yield return null;
        }

        if (lanternLight != null) lanternLight.intensity = targetMain;
        if (softLight != null) softLight.intensity = targetSoft;

        lightFadeCoroutine = null;
    }

    private void StopAudioSequence()
    {
        if (audioSequenceCoroutine != null)
        {
            StopCoroutine(audioSequenceCoroutine);
            audioSequenceCoroutine = null;
        }
    }

    public void RefillOil(float amount)
    {
        currentOil = Mathf.Clamp(currentOil + amount, 0, maxOil);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (fullOilImage == null || halfOilImage == null || lowOilImage == null) return;

        float fillPercentage = maxOil > 0 ? currentOil / maxOil : 0f;

        fullOilImage.gameObject.SetActive(false);
        halfOilImage.gameObject.SetActive(false);
        lowOilImage.gameObject.SetActive(false);

        if (fillPercentage > halfThreshold)
        {
            fullOilImage.gameObject.SetActive(true);
            fullOilImage.fillAmount = fillPercentage;
        }
        else if (fillPercentage > lowThreshold)
        {
            halfOilImage.gameObject.SetActive(true);
            halfOilImage.fillAmount = fillPercentage;
        }
        else
        {
            lowOilImage.gameObject.SetActive(true);
            lowOilImage.fillAmount = fillPercentage;
        }
    }
}