using System.Collections;
using UnityEngine;

public class GateSwitch : MonoBehaviour
{
    [Header("References")]
    public GameObject targetGate;
    public Animator switchAnimator;
    public string flipTriggerName = "FlipSwitch";

    [Header("Movement Settings")]
    public float moveDistance = 3.5f;
    public float moveDuration = 2.0f;

    private bool isPlayerNear = false;
    private bool hasBeenActivated = false;

    private Vector3 initialGatePosition; // Caches the starting position
    private Coroutine movementCoroutine; // Keeps track of the running coroutine

    private void Start()
    {
        // Cache the gate's starting position on awake so we know where to reset it to
        if (targetGate != null)
        {
            initialGatePosition = targetGate.transform.position;
        }
    }

    public void OpenGate()
    {
        if (hasBeenActivated) return;

        if (targetGate == null)
        {
            Debug.LogWarning("No target gate assigned to the switch!");
            return;
        }

        hasBeenActivated = true;

        if (switchAnimator != null)
        {
            switchAnimator.SetTrigger(flipTriggerName);
        }

        // Store the coroutine so we can stop it manually if the player dies mid-movement
        movementCoroutine = StartCoroutine(AnimateGateUp());
    }

    private IEnumerator AnimateGateUp()
    {
        Vector3 startPosition = targetGate.transform.position;
        Vector3 targetPosition = initialGatePosition + (Vector3.up * moveDistance);
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            targetGate.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        targetGate.transform.position = targetPosition;
        movementCoroutine = null;
    }

    // --- NEW: Reset logic called by the Room manager ---
    public void ResetGateSwitch()
    {
        // 1. If the door is currently moving while the player dies, stop it immediately
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        // 2. Allow the switch to be used again
        hasBeenActivated = false;

        // 3. Snap the gate back to its starting position
        if (targetGate != null)
        {
            targetGate.transform.position = initialGatePosition;
        }

        // 4. Return the lever handle animation back to idle
        if (switchAnimator != null)
        {
            switchAnimator.Play("DoorSwitchIdle");
        }
    }

    private void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E) && !hasBeenActivated)
        {
            OpenGate();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = false;
    }
}