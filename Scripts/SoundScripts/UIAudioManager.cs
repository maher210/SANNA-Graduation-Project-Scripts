using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    public AudioSource audioSource;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    public void PlayHover()
    {
        audioSource.PlayOneShot(hoverSound);
    }

    public void PlayClick()
    {
        audioSource.PlayOneShot(clickSound);
    }
}