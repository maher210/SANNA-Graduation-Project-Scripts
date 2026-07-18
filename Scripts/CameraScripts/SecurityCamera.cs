using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SecurityCamera : MonoBehaviour
{
    private PlayerMovement playerMovement; // Reference to the player's movement script
    private Light cameraSpotlight; // Reference to the child spotlight
    private Color originalLightColor;
    private Collider[] hitColliders = new Collider[10]; // Adjust size based on how many things might overlap
    private WaitForSeconds edgeWait;
    private Quaternion initialRotation; // Stores the starting rotation

    public enum CameraRotationType
    {
        Horizontal,
        Vertical
    }

    [Header("Camera Type")]
    public CameraRotationType rotationType;
    public Transform player;

    [Header("Vision")]
    public float viewDistance = 10f;
    public float viewAngle = 45f;
    public float detectionRadius = 0.5f;

    [Header("Rotation")]
    public float rotationSpeed = 2f;
    public float maxAngle = 45f;

    [Header("Wait")]
    public float waitTimeAtEdges = 1f;

    private bool isWaiting = false;

    [Header("System")]
    public bool isActive = true;
    private float currentAngle = 0f;
    private int direction = 1;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip spottedSFX;
    [SerializeField][Range(0f, 1f)] private float maxAudioVolume = 0.8f;
    [Tooltip("Time to wait AFTER spotting the player BEFORE the warning sound plays.")]
    [SerializeField] private float audioPreDelay = 0f;
    [Tooltip("Time to wait AFTER the sound plays BEFORE executing the game over/kill logic.")]
    [SerializeField] private float audioPostDelay = 0f;
    [Tooltip("Duration in seconds for the audio to smoothly fade out when the camera is reset.")]
    [SerializeField] private float audioFadeDuration = 0.4f;

    private AudioSource audioSource;
    private bool isDetecting = false;
    private Coroutine detectionSequenceCoroutine;
    private Coroutine fadeAudioCoroutine;

    void Awake()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        cameraSpotlight = GetComponentInChildren<Light>();

        if (cameraSpotlight != null)
        {
            originalLightColor = cameraSpotlight.color;
        }

        edgeWait = new WaitForSeconds(waitTimeAtEdges);
        audioSource = GetComponent<AudioSource>();

        // SAVE THE INITIAL ROTATION HERE
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        if (!isActive) return;

        if (!isWaiting)
        {
            RotateCamera();
        }
        CheckVision();
    }

    // =========================
    // 🔄 Rotation
    // =========================
    void RotateCamera()
    {
        // 1. Advance the mathematical tracker first
        currentAngle += rotationSpeed * direction * Time.deltaTime;

        // 2. Clamp the tracker and trigger the turn-around if out of bounds
        if (Mathf.Abs(currentAngle) >= maxAngle)
        {
            currentAngle = Mathf.Clamp(currentAngle, -maxAngle, maxAngle);
            StartCoroutine(WaitAndTurn());
        }

        // 3. Apply the exact absolute rotation based on the safely clamped angle
        if (rotationType == CameraRotationType.Horizontal)
        {
            // Vector3.forward is the Z-axis
            transform.localRotation = initialRotation * Quaternion.Euler(0, 0, currentAngle);
        }
        else if (rotationType == CameraRotationType.Vertical)
        {
            // Vector3.right is the X-axis
            transform.localRotation = initialRotation * Quaternion.Euler(currentAngle, 0, 0);
        }
    }

    IEnumerator WaitAndTurn()
    {
        isWaiting = true;

        yield return edgeWait;

        direction *= -1;
        isWaiting = false;
    }

    // =========================
    // 👁️ Vision
    // =========================
    void CheckVision()
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        Vector3 visionDirection = transform.up.normalized;
        float actualCastDistance = Mathf.Max(0, viewDistance - detectionRadius);
        Vector3 finalDetectionPoint = origin + visionDirection * actualCastDistance;

        // Use NonAlloc - it returns the NUMBER of things hit, and fills the hitColliders array
        int hitCount = Physics.OverlapSphereNonAlloc(finalDetectionPoint, detectionRadius, hitColliders);

        bool spotted = false;

        // Only loop through the number of things we actually hit
        for (int i = 0; i < hitCount; i++)
        {
            if (hitColliders[i].transform == player)
            {
                spotted = true;
                break;
            }
        }

        // Only kick off sequence if we aren't already locked into a tracking loop
        if (spotted && !isDetecting)
        {
            Debug.Log($"Spotted by {gameObject.name}");
            detectionSequenceCoroutine = StartCoroutine(DetectionSequence());
        }
    }

    private IEnumerator DetectionSequence()
    {
        isDetecting = true;

        // Visual feedback happens instantly so the player knows they messed up
        if (cameraSpotlight != null)
        {
            cameraSpotlight.color = Color.red;
        }

        // 1. Handle Pre-Delay
        if (audioPreDelay > 0f)
        {
            yield return new WaitForSeconds(audioPreDelay);
        }

        // 2. Play the sound direct via the track channel
        if (audioSource != null && spottedSFX != null)
        {
            if (fadeAudioCoroutine != null) StopCoroutine(fadeAudioCoroutine);

            audioSource.clip = spottedSFX;
            audioSource.volume = maxAudioVolume;
            audioSource.Play();
        }

        // 3. Handle Post-Delay
        if (audioPostDelay > 0f)
        {
            yield return new WaitForSeconds(audioPostDelay);
        }

        KillPlayer();
    }

    private void StartAudioFadeOut()
    {
        if (fadeAudioCoroutine != null) StopCoroutine(fadeAudioCoroutine);
        fadeAudioCoroutine = StartCoroutine(AudioFadeOutRoutine());
    }

    private IEnumerator AudioFadeOutRoutine()
    {
        if (audioSource == null || !audioSource.isPlaying) yield break;

        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < audioFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / audioFadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = maxAudioVolume; // Reset internal track volume level baseline
    }

    // =========================
    // 🎨 Gizmos 
    // =========================
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        Vector3 visionDirection = transform.up.normalized;
        float actualCastDistance = Mathf.Max(0, viewDistance - detectionRadius);
        Vector3 endPoint = origin + visionDirection * actualCastDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, endPoint);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(endPoint, detectionRadius);
    }

    // =========================
    // 💀 Kill
    // =========================
    void KillPlayer()
    {
        if (cameraSpotlight != null)
        {
            cameraSpotlight.color = Color.red;
        }
        playerMovement.Die();
    }

    public void ResetCamera()
    {
        isDetecting = false;

        // Halt any running kill sequences immediately
        if (detectionSequenceCoroutine != null)
        {
            StopCoroutine(detectionSequenceCoroutine);
            detectionSequenceCoroutine = null;
        }

        // Safely ease down running alert signals over time
        StartAudioFadeOut();

        if (cameraSpotlight != null)
        {
            cameraSpotlight.color = originalLightColor;
        }
    }
}