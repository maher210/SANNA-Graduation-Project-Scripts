using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            SetAlpha(0f);
            fadeImage.raycastTarget = false;
            fadeImage.enabled = false;
        }
        else
        {
            Debug.LogError("ScreenFader: Fade Image reference is missing in the Inspector!");
        }
    }

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (fadeImage == null) yield break;

        if (duration <= 0f)
            duration = fadeDuration;

        fadeImage.enabled = true;
        fadeImage.raycastTarget = true;

        float elapsed = 0f;
        SetAlpha(0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetAlpha(1f);
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        if (fadeDuration <= 0f) fadeDuration = 1f;

        fadeImage.enabled = true;
        float elapsed = 0f;
        SetAlpha(1f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(1f - (elapsed / fadeDuration)));
            yield return null;
        }

        SetAlpha(0f);
        fadeImage.raycastTarget = false;
        fadeImage.enabled = false;
    }

    private void SetAlpha(float value)
    {
        Color c = fadeImage.color;
        c.a = value;
        fadeImage.color = c;
    }
}