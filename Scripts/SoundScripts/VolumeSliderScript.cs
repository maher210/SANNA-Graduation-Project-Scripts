using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

[RequireComponent(typeof(Slider))]
public class VolumeSliderSync : MonoBehaviour
{
    [Header("Audio Setup")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string mixerParameter = "MasterVolume";

    private Slider volumeSlider;
    private const string PrefKey = "SavedMasterVolume";
    private const float DefaultVolume = 1f;

    private void Awake()
    {
        volumeSlider = GetComponent<Slider>();
    }

    private void Start()
    {
        // 1. Load the saved volume, or fall back to default
        float savedVolume = PlayerPrefs.GetFloat(PrefKey, DefaultVolume);

        // 2. Set the slider UI value without triggering onValueChanged prematurely
        volumeSlider.value = savedVolume;

        // 3. Apply it to the mixer immediately on scene start
        ApplyVolumeToMixer(savedVolume);

        // 4. Listen for future player adjustments
        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        // Save the raw 0-1 value
        PlayerPrefs.SetFloat(PrefKey, value);
        PlayerPrefs.Save();

        // Apply to mixer
        ApplyVolumeToMixer(value);
    }

    private void ApplyVolumeToMixer(float value)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning($"AudioMixer unassigned on {gameObject.name}");
            return;
        }

        // Convert linear 0.0001-1 slider scale to logarithmic decibel scale (-80dB to 0dB)
        float decibel = Mathf.Log10(value) * 20;
        audioMixer.SetFloat(mixerParameter, decibel);
    }

    private void OnDestroy()
    {
        // Clean up listeners when the scene changes or object is destroyed
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}