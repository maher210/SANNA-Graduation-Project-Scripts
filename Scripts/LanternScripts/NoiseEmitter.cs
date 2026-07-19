using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))] // 🛠️ ADDITION: Guarantees an AudioSource exists on this GameObject
public class NoiseEmitter : MonoBehaviour
{
    public bool isShaking = false;
    public float noiseRadius = 10f;

    // مدة رج المفاتيح
    public float shakeDuration = 0.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip shakeSFX; // 🛠️ ADDITION: Drop your key-jingle / noise clip here in the Inspector

    private InputSystem_Actions inputActions;
    private AudioSource audioSource; // 🛠️ ADDITION: Reference to the speaker component

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        inputActions.Player.Shake.performed += MakeNoise;

        // 🛠️ ADDITION: Grab the AudioSource component on startup
        audioSource = GetComponent<AudioSource>();
    }

    void OnDisable()
    {
        inputActions.Player.Shake.performed -= MakeNoise;
        inputActions.Player.Disable();
    }

    // =========================
    // 🔔 إصدار الصوت
    // =========================
    public void MakeNoise(InputAction.CallbackContext context)
    {
        // إذا كان بالفعل يرج المفاتيح لا تعيد التشغيل
        if (isShaking)
            return;

        StartCoroutine(ShakeRoutine());
    }

    // =========================
    // 🔑 روتين رج المفاتيح
    // =========================
    IEnumerator ShakeRoutine()
    {
        isShaking = true;
        Debug.Log("Noise Emitted");

        // 🛠️ ADDITION: Play the audio clip immediately when the shake action triggers
        if (audioSource != null && shakeSFX != null)
        {
            audioSource.clip = shakeSFX;
            audioSource.Play();
        }

        // نبح عن كل الأشياء داخل دائرة
        Collider[] hits = Physics.OverlapSphere(transform.position, noiseRadius);

        Debug.Log("Enemies Found: " + hits.Length);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Debug.Log("Enemy Heard Sound: " + hit.name);

                EnemyTest enemy = hit.GetComponent<EnemyTest>();

                if (enemy != null)
                {
                    enemy.HearNoise(transform.position);
                }
            }
        }

        // مدة الأنيميشن
        yield return new WaitForSeconds(shakeDuration);

        // 🛠️ ADDITION: Force-stop the audio if the sound clip lasts longer than the shake duration
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isShaking = false;
        Debug.Log("Noise Stopped");
    }

    // =========================
    // 🟡 رؤية مدى الصوت
    // =========================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
