using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI text;

    [Header("Settings")]
    public float typingSpeed = 0.03f;
    public float popDuration = 0.2f;

    [Header("Dialogue Audio Settings")]
    [SerializeField] private AudioClip typewriterSFX;
    [SerializeField][Range(0f, 1f)] private float volume = 0.6f;

    [Tooltip("Slightly alters the frequency of each click so it sounds natural and non-repetitive.")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField][Range(0f, 0.3f)] private float pitchVariationRange = 0.12f;

    private Coroutine typingRoutine;
    private Coroutine popRoutine;
    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        audioSource = GetComponent<AudioSource>();
    }

    public void Show(string sentence)
    {
        panel.SetActive(true);

        // Stop previous routines specifically so they don't interfere
        if (popRoutine != null) StopCoroutine(popRoutine);
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        // 🛠️ NEW: Force the hardware to stop instantly. 
        // This kills any trailing audio tail lingering from a skipped sentence.
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        popRoutine = StartCoroutine(PopAnimation());
        typingRoutine = StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator PopAnimation()
    {
        float elapsed = 0;
        Vector3 targetScale = Vector3.one;
        panel.transform.localScale = Vector3.zero;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float percent = elapsed / popDuration;

            panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, percent);
            yield return null;
        }

        panel.transform.localScale = targetScale;
    }

    IEnumerator TypeSentence(string sentence)
    {
        text.text = "";

        foreach (char c in sentence)
        {
            text.text += c;

            if (audioSource != null && typewriterSFX != null)
            {
                if (!char.IsWhiteSpace(c))
                {
                    if (randomizePitch)
                    {
                        audioSource.pitch = Random.Range(1f - pitchVariationRange, 1f + pitchVariationRange);
                    }
                    else
                    {
                        audioSource.pitch = 1f;
                    }

                    audioSource.PlayOneShot(typewriterSFX, volume);
                }
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }
    }

    // 🛠️ CHANGED: Cleans up running routines and mutes audio hardware if closed early
    public void Hide()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        if (popRoutine != null) StopCoroutine(popRoutine);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.pitch = 1f; // Keep house tidy for the next launch
        }

        panel.SetActive(false);
    }
}