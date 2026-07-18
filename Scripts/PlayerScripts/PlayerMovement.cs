using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 10f;

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
        inputActions = new InputSystem_Actions();
        animationController = GetComponent<PlayerAnimationController>();
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

            animationController.animator.SetFloat("Speed", 0f);
        }
    }

    void Update()
    {
        if (controlsLocked)
            return;
        HandleCameraRelativeMovement();
        CheckInteractable();

        Debug.DrawRay(cameraTransform.position,
            cameraTransform.forward * interactDistance,
            Color.red);
    }

    void FixedUpdate()
    {
        if (controlsLocked)
            return;
        HandleClimbing();
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

        currentInteractable.Interact(this);

        animationController?.PlayInteract();
    }

    // ========================
    // CAMERA RELATIVE MOVEMENT
    // ========================

    private void HandleCameraRelativeMovement()
    {
        if (controlsLocked)
            return;
        if (cameraTransform == null)
        {
            movement = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
        else
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            movement =
                (camForward * moveInput.y +
                 camRight * moveInput.x).normalized;
        }

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(movement);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ========================
    // INTERACTION
    // ========================

    private void CheckInteractable()
    {
        currentInteractable = null;

        Ray headRay = new Ray(
            transform.position + Vector3.up,
            transform.forward);

        Debug.DrawRay(
            headRay.origin,
            headRay.direction * interactDistance,
            Color.red);

        if (Physics.Raycast(headRay, out RaycastHit hit, interactDistance))
        {
            var interactable =
                hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
            }
        }

        Ray feetRay = new Ray(
            transform.position,
            transform.forward);

        Debug.DrawRay(
            feetRay.origin,
            feetRay.direction * interactDistance,
            Color.blue);

        if (Physics.Raycast(feetRay, out hit, interactDistance))
        {
            var interactable =
                hit.collider.GetComponentInParent<IInteractable>();

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
        return currentInteractable != null
            ? currentInteractable.GetPrompt()
            : null;
    }

    // ========================
    // MOVEMENT
    // ========================

    private void HandleMovement()
    {
        if (IsClimbing)
            return;

        rb.useGravity = true;

        float speed =
            moveSpeed * (IsCrouching ? 0.5f : 1f);

        rb.MovePosition(
            rb.position +
            movement * speed * Time.fixedDeltaTime);

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

        rb.linearVelocity =
            new Vector3(0, moveInput.y * climbSpeed, 0);

        isMovingClimb =
            Mathf.Abs(moveInput.y) > 0.1f;
    }

    // ========================
    // STEALTH
    // ========================

    public bool IsHiddenFrom(Transform enemy)
    {
        if (currentHideable == null)
            return false;

        return currentHideable.IsPlayerHiddenFrom(
            transform,
            enemy);
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