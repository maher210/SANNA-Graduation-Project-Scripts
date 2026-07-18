using UnityEditor.UI;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch Settings")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float smoothSpeed = 10f;

    private CapsuleCollider col;
    private PlayerMovement movement;

    private float currentHeight;

    void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        movement = GetComponent<PlayerMovement>();

        currentHeight = standHeight;
    }

    void Update()
    {
        HandleCrouch();
    }

    private void HandleCrouch()
    {
        float targetHeight = movement.IsCrouching ? crouchHeight : standHeight;

        currentHeight = Mathf.Lerp(
            currentHeight,
            targetHeight,
            Time.deltaTime * smoothSpeed
        );

        UpdateCollider(currentHeight);
    }

    private void UpdateCollider(float height)
    {
        col.height = height;
        col.center = new Vector3(0f, height / 2f, 0f);
    }
}