using UnityEngine;

public class ZoneAudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip clip;
    [SerializeField] private bool loop = true;
    [Range(0f, 1f)][SerializeField] private float targetVolume = 1f;

    [Header("Fade Timings (Seconds)")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 2.0f;

    [Tooltip("How many seconds before the track ends should it automatically fade out?")]
    [SerializeField] private float autoFadeDuration = 2.0f;

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    // Public getters
    public AudioClip Clip => clip;
    public bool Loop => loop;
    public float TargetVolume => targetVolume;
    public float FadeInDuration => fadeInDuration;
    public float FadeOutDuration => fadeOutDuration;
    public float AutoFadeDuration => autoFadeDuration;

    // Tracks if the player is actually standing here
    public bool IsPlayerInside { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            IsPlayerInside = true;
            ZoneAudioManager.Instance.OnZoneEnter(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            IsPlayerInside = false;
            ZoneAudioManager.Instance.OnZoneExit(this);
        }
    }
}