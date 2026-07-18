using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EnemyChasing : MonoBehaviour
{
    // =========================
    // الحالات الممكنة للعدو
    // =========================
    public enum EnemyState
    {
        Idle,
        Investigating,
        Returning
    }
    private PlayerVisibility playerVisibility;

    [Header("Suspicion")]
    public float suspicion = 0f;
    public float maxSuspicion = 100f;
    // سرعة الزيادة الأساسية
    public float suspicionIncreaseRate = 20f;
    // سرعة النقصان
    public float suspicionDecreaseRate = 15f;

    private EnemyPatrol patrol;
    public float killDistance = 1.5f;
    public Transform player;

    private PlayerMovement playerMovement;
    private EnemyShooter shooter;
    private bool watchingPlayer = false;

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0, 180)] public float viewAngle = 90f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip spottedSFX;
    [SerializeField][Range(0f, 1f)] private float maxAudioVolume = 0.8f;
    [Tooltip("Time to wait AFTER hitting 100% suspicion BEFORE the sound plays.")]
    [SerializeField] private float audioPreDelay = 0f;
    [Tooltip("Time to wait AFTER the sound plays BEFORE executing the kill logic.")]
    [SerializeField] private float audioPostDelay = 0f;
    [Tooltip("Duration in seconds for the audio to smoothly fade out when the enemy is reset or interrupted.")]
    [SerializeField] private float audioFadeDuration = 0.4f;

    private Coroutine investigateCoroutine;
    private Coroutine detectionSequenceCoroutine;
    private Coroutine fadeAudioCoroutine;

    [SerializeField] private Animator animator;

    public EnemyState currentState;
    private NavMeshAgent agent;

    private Vector3 startPosition;
    private Quaternion startRotation;
    public float searchTime = 3f;

    private bool canSeePlayer;
    public bool debugThisEnemy = false;
    private float blindTimer = 0f;

    private AudioSource audioSource;
    private bool isDetecting = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        patrol = GetComponent<EnemyPatrol>();
        agent = GetComponent<NavMeshAgent>();
        playerMovement = player.GetComponent<PlayerMovement>();
        shooter = GetComponent<EnemyShooter>();

        startPosition = transform.position;
        startRotation = transform.rotation;
        currentState = EnemyState.Idle;
        playerVisibility = player.GetComponent<PlayerVisibility>();
    }

    // =========================
    // يسمع صوت
    // =========================
    public void HearNoise(Vector3 noisePosition)
    {
        Debug.Log("I HEARD SOUND");

        if (investigateCoroutine != null)
        {
            StopCoroutine(investigateCoroutine);
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.isStopped = false;

        if (patrol != null)
        {
            patrol.StopPatrol();
        }

        currentState = EnemyState.Investigating;
        agent.SetDestination(noisePosition);
        Debug.Log("Going to: " + noisePosition);

        investigateCoroutine = StartCoroutine(InvestigateRoutine());
    }

    void CheckVision()
    {
        if (blindTimer > 0)
        {
            blindTimer -= Time.deltaTime;
            return;
        }
        canSeePlayer = false;

        if (player == null || playerMovement == null)
            return;

        Vector3 eyePosition = transform.position + Vector3.up * 2.8f;
        Vector3 targetPosition;

        if (playerMovement.IsCrouching)
        {
            targetPosition = player.position + Vector3.up * 0.2f;
        }
        else
        {
            targetPosition = player.position + Vector3.up * 1f;
        }

        Vector3 dirToPlayer = targetPosition - eyePosition;
        float distance = dirToPlayer.magnitude;
        float angle = Vector3.Angle(transform.forward, dirToPlayer.normalized);

        float currentViewAngle = viewAngle;
        if (distance < 3f)
        {
            currentViewAngle = 180f;
        }
        if (angle > currentViewAngle / 2f)
        {
            return;
        }

        // =========================
        // الانحناء يقلل الرؤية
        // =========================
        float effectiveViewDistance = Mathf.Lerp(2f, viewDistance, playerVisibility.visibility / 10f);

        if (distance > effectiveViewDistance)
        {
            return;
        }

        // =========================
        // فحص الجدران والعوائق
        // =========================
        RaycastHit hit;

        if (Physics.Raycast(eyePosition, dirToPlayer.normalized, out hit, viewDistance))
        {
            if (hit.transform == player)
            {
                Debug.Log("PLAYER SPOTTED 👀");

                Vector3 lookDirection = player.position - transform.position;
                lookDirection.y = 0;
                transform.rotation = Quaternion.LookRotation(lookDirection);

                if (hit.transform == player)
                {
                    canSeePlayer = true;

                    if (!watchingPlayer)
                    {
                        watchingPlayer = true;
                        agent.isStopped = true;
                    }

                    IncreaseSuspicion(distance);
                }
            }
        }
        Debug.Log("Hit: " + hit.transform.name);
    }

    void IncreaseSuspicion(float distance)
    {
        float distanceMultiplier = Mathf.Lerp(8f, 1f, distance / viewDistance);
        float suspicionGain = suspicionIncreaseRate * distanceMultiplier;

        if (debugThisEnemy)
        {
            Debug.Log("Distance = " + distance + " | Multiplier = " + distanceMultiplier + "x" + " | Suspicion/sec = " + suspicionGain);
        }

        // Only increase suspicion if we aren't already locked into the final detection sequence
        if (!isDetecting)
        {
            suspicion += suspicionIncreaseRate * distanceMultiplier * Time.deltaTime;
            suspicion = Mathf.Clamp(suspicion, 0, maxSuspicion);
        }

        if (suspicion >= maxSuspicion && !isDetecting)
        {
            Debug.Log("PLAYER DETECTED 💀");
            detectionSequenceCoroutine = StartCoroutine(DetectionSequence());
        }
    }

    private IEnumerator DetectionSequence()
    {
        isDetecting = true;

        // 1. Handle Pre-Delay (Time waiting before playing the sound)
        if (audioPreDelay > 0f)
        {
            yield return new WaitForSeconds(audioPreDelay);
        }

        // 2. Play the sound clip directly through the track controller
        if (audioSource != null && spottedSFX != null)
        {
            if (fadeAudioCoroutine != null) StopCoroutine(fadeAudioCoroutine);

            audioSource.clip = spottedSFX;
            audioSource.volume = maxAudioVolume;
            audioSource.Play();
        }

        // 3. Handle Post-Delay (Time waiting after the sound plays before death execution)
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
        audioSource.volume = maxAudioVolume; // Reset volume baseline internally for the next lifecycle loop
    }

    // =========================
    // التحقيق
    // =========================
    IEnumerator InvestigateRoutine()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            if (!agent.hasPath)
                break;

            yield return null;
        }
        float timer = 0f;

        while (timer < searchTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.Idle;
        agent.isStopped = false;

        if (patrol != null)
        {
            patrol.ResumePatrol();
        }

        investigateCoroutine = null;
    }

    void Update()
    {
        CheckVision();

        if (!canSeePlayer && !isDetecting)
        {
            suspicion -= suspicionDecreaseRate * Time.deltaTime;
        }

        if (!canSeePlayer && watchingPlayer)
        {
            watchingPlayer = false;
            agent.isStopped = false;
        }

        suspicion = Mathf.Clamp(suspicion, 0, maxSuspicion);

        Vector3 dirToPlayer = player.position - transform.position;
        float distance = dirToPlayer.magnitude;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        float currentKillDistance = killDistance;

        if (playerMovement.IsCrouching)
        {
            currentKillDistance *= 0.5f;
        }

        if (distance <= currentKillDistance && angle <= 35f)
        {
            KillPlayer();
        }

        bool isWalking = agent.velocity.magnitude > 0.1f;
        animator.SetBool("isWalking", isWalking);
    }

    void KillPlayer()
    {
        playerMovement.Die();
    }

    void ShootPlayer()
    {
        if (shooter == null)
            return;

        shooter.TryShoot(playerMovement);
    }

    public void ResetEnemy()
    {
        blindTimer = 2f;
        suspicion = 0f;
        canSeePlayer = false;
        watchingPlayer = false;
        isDetecting = false;

        // Halt any pending death sequencing calculations
        if (detectionSequenceCoroutine != null)
        {
            StopCoroutine(detectionSequenceCoroutine);
            detectionSequenceCoroutine = null;
        }

        // Cleanly spin down active audio elements instead of letting them clip out hard
        StartAudioFadeOut();

        if (investigateCoroutine != null)
        {
            StopCoroutine(investigateCoroutine);
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        currentState = EnemyState.Idle;
        transform.position = startPosition;
        transform.rotation = startRotation;

        agent.Warp(startPosition);
        agent.nextPosition = startPosition;

        if (patrol != null)
        {
            patrol.ResumePatrol();
        }

        if (shooter != null)
        {
            shooter.ResetShooter();
        }

        agent.isStopped = false;
        Debug.Log("Enemy Reset - Suspicion cleared & Audio fading");
    }

    void OnDrawGizmosSelected()
    {
        Vector3 eyePosition = transform.position + Vector3.up * 2.8f;
        float currentViewDistance = viewDistance;

        if (playerMovement != null && playerMovement.IsCrouching)
        {
            currentViewDistance *= 0.5f;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawRay(eyePosition, transform.forward * currentViewDistance);

        Quaternion leftRayRotation = Quaternion.AngleAxis(-viewAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(viewAngle / 2, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * transform.forward;
        Vector3 rightRayDirection = rightRayRotation * transform.forward;

        Gizmos.DrawRay(eyePosition, leftRayDirection * currentViewDistance);
        Gizmos.DrawRay(eyePosition, rightRayDirection * currentViewDistance);

        float currentKillDistance = killDistance;

        if (playerMovement != null && playerMovement.IsCrouching)
        {
            currentKillDistance *= 0.5f;
        }

        Gizmos.color = Color.magenta;
        Vector3 killDirection = transform.forward * currentKillDistance;
        Gizmos.DrawRay(transform.position, killDirection);
    }
}