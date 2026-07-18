using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class ZoneAudioManager : MonoBehaviour
{
    public static ZoneAudioManager Instance { get; private set; }

    [Header("Scene Settings")]
    [Tooltip("The EXACT name of your Main Menu / Title Screen scene.")]
    [SerializeField] private string mainMenuSceneName = "TitleScreen";

    [Header("Audio Components")]
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource fadingSource;

    private ZoneAudioTrigger currentTrigger;
    private ZoneAudioTrigger queuedTrigger;

    private Coroutine fadeCoroutine;
    private Coroutine monitorCoroutine;
    private bool isFadingOut = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sourceA == null || sourceA.GetComponentInParent<ZoneAudioManager>() != this)
            sourceA = gameObject.AddComponent<AudioSource>();

        if (sourceB == null || sourceB.GetComponentInParent<ZoneAudioManager>() != this)
            sourceB = gameObject.AddComponent<AudioSource>();

        activeSource = sourceA;
        fadingSource = sourceB;
    }

    private void OnEnable()
    {
        // Tell Unity to let us know whenever a new scene finishes loading
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 🤖 AUTOMATIC CLEANUP ON SCENE LOAD
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we just loaded into the Main Menu, aggressively kill all playing music instantly
        if (scene.name == mainMenuSceneName)
        {
            StopAllCoroutines();
            fadeCoroutine = null;
            monitorCoroutine = null;
            isFadingOut = false;

            currentTrigger = null;
            queuedTrigger = null;

            if (sourceA != null) { sourceA.Stop(); sourceA.volume = 0f; sourceA.clip = null; }
            if (sourceB != null) { sourceB.Stop(); sourceB.volume = 0f; sourceB.clip = null; }

            // Explicitly reset timescale just in case the pause menu forgot to reset it on exit
            Time.timeScale = 1f;
        }
    }

    public void FadeOutBeforeSceneLoad(float fadeDuration)
    {
        StopMonitoring();
        currentTrigger = null;
        queuedTrigger = null;

        if (activeSource != null && activeSource.isPlaying)
        {
            StopActiveFade();
            fadeCoroutine = StartCoroutine(SceneTransitionFadeRoutine(fadeDuration));
        }
    }

    private IEnumerator SceneTransitionFadeRoutine(float fadeDuration)
    {
        isFadingOut = true;

        if (activeSource == null)
        {
            isFadingOut = false;
            fadeCoroutine = null;
            yield break;
        }

        float startVolume = activeSource.volume;

        // ⏱️ FIXED: Uses unscaledDeltaTime to bypass pause menu freezes
        while (activeSource != null && activeSource.volume > 0)
        {
            activeSource.volume -= startVolume * Time.unscaledDeltaTime / fadeDuration;
            yield return null;
        }

        if (activeSource != null)
        {
            activeSource.Stop();
            activeSource.volume = 0f;
        }

        isFadingOut = false;
        fadeCoroutine = null;
    }

    public void OnZoneEnter(ZoneAudioTrigger trigger)
    {
        if (activeSource == null || fadingSource == null) return;
        if (trigger == currentTrigger && !isFadingOut) return;
        if (trigger == queuedTrigger) return;

        if (trigger == currentTrigger && isFadingOut)
        {
            StopActiveFade();
            isFadingOut = false;

            AudioSource temp = activeSource;
            activeSource = fadingSource;
            fadingSource = temp;

            activeSource.loop = (trigger.AutoFadeDuration <= 0f) ? trigger.Loop : false;
            fadeCoroutine = StartCoroutine(FadeIn(activeSource, trigger.FadeInDuration, trigger.TargetVolume));

            StartMonitoring(trigger);
            return;
        }

        if (isFadingOut || fadeCoroutine != null)
        {
            queuedTrigger = trigger;
            return;
        }

        currentTrigger = trigger;
        PlayTriggerImmediate(trigger);
    }

    public void OnZoneExit(ZoneAudioTrigger trigger)
    {
        if (activeSource == null) return;
        if (trigger == currentTrigger)
        {
            StopMonitoring();

            if (!isFadingOut && activeSource.isPlaying)
            {
                StopActiveFade();
                fadeCoroutine = StartCoroutine(DefaultFadeOutRoutine(trigger.FadeOutDuration));
            }
            else if (!isFadingOut && !activeSource.isPlaying)
            {
                currentTrigger = null;
            }
        }
        else if (trigger == queuedTrigger)
        {
            queuedTrigger = null;
        }
    }

    private void PlayTriggerImmediate(ZoneAudioTrigger trigger)
    {
        if (activeSource == null) return;

        activeSource.clip = trigger.Clip;
        activeSource.loop = (trigger.AutoFadeDuration <= 0f) ? trigger.Loop : false;
        activeSource.volume = 0f;
        activeSource.Play();

        fadeCoroutine = StartCoroutine(FadeIn(activeSource, trigger.FadeInDuration, trigger.TargetVolume));
        StartMonitoring(trigger);
    }

    private void StartMonitoring(ZoneAudioTrigger trigger)
    {
        if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
        monitorCoroutine = StartCoroutine(MonitorAudioEnd(trigger));
    }

    private void StopMonitoring()
    {
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
            monitorCoroutine = null;
        }
    }

    private IEnumerator MonitorAudioEnd(ZoneAudioTrigger trigger)
    {
        if (trigger.AutoFadeDuration <= 0f || trigger.Clip == null) yield break;

        float waitTime = trigger.Clip.length - trigger.AutoFadeDuration;
        if (waitTime > 0f)
        {
            // Note: WaitForSeconds is affected by timeScale. 
            // If you pause during an active countdown track, you might want to switch this to WaitForSecondsRealtime.
            yield return new WaitForSeconds(waitTime);
        }

        if (!isFadingOut)
        {
            StopActiveFade();
            fadeCoroutine = StartCoroutine(DefaultFadeOutRoutine(trigger.AutoFadeDuration));
        }
    }

    private IEnumerator FadeIn(AudioSource source, float duration, float targetVolume)
    {
        if (source == null) { fadeCoroutine = null; yield break; }
        float startVolume = source.volume;
        float elapsed = 0f;

        // ⏱️ FIXED: Uses unscaledDeltaTime
        while (source != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        if (source != null) source.volume = targetVolume;
        fadeCoroutine = null;
    }

    private IEnumerator DefaultFadeOutRoutine(float duration)
    {
        isFadingOut = true;
        StopMonitoring();

        AudioSource temp = activeSource;
        activeSource = fadingSource;
        fadingSource = temp;

        if (fadingSource == null)
        {
            isFadingOut = false;
            fadeCoroutine = null;
            EvaluateNextTriggerState();
            yield break;
        }

        float startVolume = fadingSource.volume;
        float elapsed = 0f;

        // ⏱️ FIXED: Uses unscaledDeltaTime
        while (fadingSource != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadingSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        if (fadingSource != null)
        {
            fadingSource.volume = 0f;
            fadingSource.Stop();
        }

        isFadingOut = false;
        fadeCoroutine = null;

        EvaluateNextTriggerState();
    }

    private void EvaluateNextTriggerState()
    {
        if (queuedTrigger != null)
        {
            currentTrigger = queuedTrigger;
            queuedTrigger = null;
            PlayTriggerImmediate(currentTrigger);
        }
        else if (currentTrigger != null && currentTrigger.Loop && currentTrigger.IsPlayerInside)
        {
            PlayTriggerImmediate(currentTrigger);
        }
        else
        {
            currentTrigger = null;
        }
    }

    private void StopActiveFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }
}