using UnityEngine;
using UnityEngine.UI;

public class EnemySuspicionUI : MonoBehaviour
{
    [Header("UI Container Setup")]
    [Tooltip("Drag the child World Space Canvas (or the UI parent GameObject) here.")]
    public Transform uiContainer;

    [Header("UI Elements")]
    public Image questionMarkFill;
    public GameObject exclamationMark;

    [Header("Position & Rotation Control")]
    [Tooltip("Offset relative to the enemy's head.")]
    public Vector3 positionOffset = new Vector3(0, 2.5f, 0);

    [Tooltip("If true, the UI will always rotate to face the main camera.")]
    public bool faceCamera = true;

    [Tooltip("Adjust the rotation. If faceCamera is true, this acts as a tilt/tweak. If false, this is the exact fixed rotation.")]
    public Vector3 customRotation = Vector3.zero;

    private EnemyChasing enemyLogic;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        enemyLogic = GetComponent<EnemyChasing>();

        if (enemyLogic == null)
        {
            Debug.LogError($"EnemySuspicionUI on {gameObject.name} couldn't find an EnemyChasing script on the same GameObject!", this);
        }

        // Initialize UI states
        if (questionMarkFill != null) questionMarkFill.gameObject.SetActive(false);
        if (exclamationMark != null) exclamationMark.SetActive(false);
    }

    void LateUpdate()
    {
        // Safety checks to prevent errors if references are missing
        if (enemyLogic == null || uiContainer == null) return;

        UpdateUIState();
        UpdateTransform();
    }

    private void UpdateUIState()
    {
        float current = enemyLogic.suspicion;
        float max = 98;

        // STATE 1: Fully Detected
        if (current >= max)
        {
            if (questionMarkFill != null) questionMarkFill.gameObject.SetActive(false);
            if (exclamationMark != null) exclamationMark.SetActive(true);
        }
        // STATE 2: Suspicious (Filling up)
        else if (current > 0)
        {
            if (exclamationMark != null) exclamationMark.SetActive(false);

            if (questionMarkFill != null)
            {
                questionMarkFill.gameObject.SetActive(true);
                questionMarkFill.fillAmount = current / max;
            }
        }
        // STATE 3: Idle
        else
        {
            if (questionMarkFill != null) questionMarkFill.gameObject.SetActive(false);
            if (exclamationMark != null) exclamationMark.SetActive(false);
        }
    }

    private void UpdateTransform()
    {
        uiContainer.position = transform.position + positionOffset;

        // 2. Control UI Container Rotation
        if (faceCamera && mainCam != null)
        {
            uiContainer.rotation = mainCam.transform.rotation * Quaternion.Euler(customRotation);
        }
        else
        {
            uiContainer.rotation = Quaternion.Euler(customRotation);
        }
    }
}