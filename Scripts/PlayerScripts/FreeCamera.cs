using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Rigidbody))]
public class FreeLookMovement : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 10f;

    // ADD THESE TWO LINES RIGHT HERE:
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 1000f;

    private PlayerAnimationController animationController;

    private Rigidbody rb;
    private Ladder nearbyLadder;

    private bool isMovingGround;
    private bool isMovingClimb;
    private bool isDead = false;
    public bool controlsLocked = false;

    private HideableObject currentHideable;
    private IInteractable currentInteractable;

    public Vector2 moveInput;
    private Vector3 movement;

    public bool IsClimbing { get; set; }
    public bool IsCrouching { get; private set; }

    public bool IsMoving
    {
        get
        {
            return IsClimbing ? isMovingClimb : isMovingGround;
        }
    }

    public float CurrentSpeedPercent { get; private set; }

    private InputSystem_Actions inputActions;
    public event Action OnPlayerDeath;
    public static event Action OnAnyPlayerDeath;

    // ========================
    // UNITY
    // ========================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // CRITICAL FOR FREELOOK: Stop physics from randomly tipping the player over
        rb.freezeRotation = true;

        inputActions = new InputSystem_Actions();
        animationController = GetComponent<PlayerAnimationController>();

        // FORCE lock onto the true Main Camera, bypassing Inspector assignment mistakes
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("No Main Camera found in the scene! Ensure your camera is tagged 'MainCamera'.");
        }
    }

    void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMoveCanceled;

        inputActions.Player.Crouch.performed += OnCrouch;
        inputActions.Player.Crouch.canceled += OnCrouch;

        inputActions.Player.Interact.performed += OnInteract;
    }

    void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMoveCanceled;

        inputActions.Player.Crouch.performed -= OnCrouch;
        inputActions.Player.Crouch.canceled -= OnCrouch;

        inputActions.Player.Interact.performed -= OnInteract;

        inputActions.Player.Disable();
    }

    public void SetControlsLocked(bool state)
    {
        controlsLocked = state;

        if (state)
        {
            moveInput = Vector2.zero;
            movement = Vector3.zero;

            isMovingGround = false;
            isMovingClimb = false;

            CurrentSpeedPercent = 0f;

            IsCrouching = false;

            rb.linearVelocity = Vector3.zero;

            if (animationController != null && animationController.animator != null)
            {
                animationController.animator.SetFloat("Speed", 0f);
            }
        }
    }

    void Update()
    {
        if (controlsLocked)
            return;

        CalculateCameraRelativeDirection();
        CheckInteractable();

        if (animationController != null && animationController.animator != null)
        {
            animationController.animator.SetFloat("Speed", CurrentSpeedPercent);
            animationController.animator.SetBool("IsCrouching", IsCrouching);
        }

        if (cameraTransform != null)
        {
            Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactDistance, Color.red);
        }
    }

    void FixedUpdate()
    {
        if (controlsLocked)
            return;

        HandleClimbing();
        HandleRotation(); // Rotates cleanly in the physics loop
        HandleMovement();
    }

    // ========================
    // INPUT
    // ========================

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnCrouch(InputAction.CallbackContext context)
    {
        if (controlsLocked)
            return;
        if (!context.performed)
            return;

        IsCrouching = !IsCrouching;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (controlsLocked)
            return;
        if (nearbyLadder != null)
        {
            nearbyLadder.TryStartClimbing();
            return;
        }

        if (currentInteractable == null)
            return;

        animationController?.PlayInteract();
    }

    // ========================
    // CAMERA RELATIVE DIRECTION CALCULATION
    // ========================

    private void CalculateCameraRelativeDirection()
    {
        // 1. Force find the main camera if the reference is lost
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 2. Diagnostics if completely missing
        if (cameraTransform == null)
        {
            Debug.LogError("!! CAMERA MISSING !! Your script cannot find any Main Camera. Defaulting to broken World Space.");
            movement = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            return;
        }

        // 3. Print exactly what object it is reading and its Y rotation angle
        // Look at your console while orbiting the camera to see if this number changes!
        Debug.Log($"Tracking Camera: Object Name = '{cameraTransform.gameObject.name}' | Current Y Rotation = {cameraTransform.eulerAngles.y}");

        // Get camera directions flattened onto the XZ ground plane
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        // Calculate final direction relative to camera perspective
        movement = (camForward * moveInput.y + camRight * moveInput.x).normalized;
    }

    private void HandleRotation()
    {
        // Only rotate if the player is actively providing input and not climbing
        if (movement != Vector3.zero && !IsClimbing)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            // Using rb.MoveRotation instead of transform.rotation stops FreeLook jitter
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    // ========================
    // INTERACTION
    // ========================

    private void CheckInteractable()
    {
        currentInteractable = null;

        Ray headRay = new Ray(transform.position + Vector3.up, transform.forward);

        Debug.DrawRay(headRay.origin, headRay.direction * interactDistance, Color.red);

        if (Physics.Raycast(headRay, out RaycastHit hit, interactDistance))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
            }
        }

        Ray feetRay = new Ray(transform.position, transform.forward);

        Debug.DrawRay(feetRay.origin, feetRay.direction * interactDistance, Color.blue);

        if (Physics.Raycast(feetRay, out hit, interactDistance))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
            }
        }
    }

    public bool HasInteractable()
    {
        return currentInteractable != null;
    }

    public string GetInteractPrompt()
    {
        return currentInteractable != null ? currentInteractable.GetPrompt() : null;
    }

    // ========================
    // MOVEMENT
    // ========================

    private void HandleMovement()
    {
        if (IsClimbing)
            return;

        rb.useGravity = true;

        float speed = moveSpeed * (IsCrouching ? 0.5f : 1f);

        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);

        isMovingGround = movement.sqrMagnitude > 0.01f;
        CurrentSpeedPercent = movement.magnitude;
    }

    // ========================
    // CLIMBING
    // ========================

    private void HandleClimbing()
    {
        if (!IsClimbing)
            return;

        float climbSpeed = moveSpeed * 0.5f;
        IsCrouching = false;
        rb.useGravity = false;

        rb.linearVelocity = new Vector3(0, moveInput.y * climbSpeed, 0);
        isMovingClimb = Mathf.Abs(moveInput.y) > 0.1f;
    }

    // ========================
    // STEALTH
    // ========================

    public bool IsHiddenFrom(Transform enemy)
    {
        if (currentHideable == null)
            return false;

        return currentHideable.IsPlayerHiddenFrom(transform, enemy);
    }

    public float GetStealthMultiplier()
    {
        float multiplier = 1f;

        if (IsCrouching)
            multiplier *= 0.6f;

        if (IsCrouching && IsMoving)
            multiplier *= 0.8f;

        return multiplier;
    }

    public float GetVisibilityLevel(Transform enemy)
    {
        if (IsHiddenFrom(enemy))
            return 0f;

        float visibility = 1f;

        if (IsCrouching)
            visibility *= 0.5f;

        if (IsCrouching && IsMoving)
            visibility *= 0.75f;

        return visibility;
    }

    // ========================
    // LADDER / HIDING
    // ========================

    public void SetNearbyLadder(Ladder ladder)
    {
        nearbyLadder = ladder;
    }

    public void SetHideableObject(HideableObject obj)
    {
        currentHideable = obj;
    }

    // ========================
    // FREEZE
    // ========================

    public void SetFrozen(bool state)
    {
        enabled = !state;

        if (state)
        {
            moveInput = Vector2.zero;
            movement = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }
    }

    // ========================
    // DEATH
    // ========================

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        StartCoroutine(DeathRoutine());
        OnPlayerDeath?.Invoke();
        OnAnyPlayerDeath?.Invoke();
    }

    private System.Collections.IEnumerator DeathRoutine()
    {
        SetControlsLocked(true);
        Time.timeScale = 0.3f;

        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }
        else
        {
            Debug.LogWarning("ScreenFader missing from scene!");
            yield return new WaitForSecondsRealtime(1f);
        }

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetArea();
        }

        yield return null;

        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeIn());
        }

        isDead = false;
        SetControlsLocked(false);
    }
}