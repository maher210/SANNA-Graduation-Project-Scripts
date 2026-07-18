using System.Collections;
using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    private AudioSource audioSource;

    public float fadeDuration = 2f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void FadeOutMusic()
    {
        StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -=
                startVolume *
                Time.deltaTime /
                fadeDuration;

            yield return null;
        }

        audioSource.Stop();

        audioSource.volume = startVolume;
    }
}