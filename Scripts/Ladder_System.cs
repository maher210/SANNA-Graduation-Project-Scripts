using UnityEngine;

public class Ladder : MonoBehaviour
{
    [Header("Ladder Settings")]
    public Transform snapPoint;
    public float exitOffset = 0.3f;

    private PlayerMovement playerInRange;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerMovement player))
            return;

        playerInRange = player;
        player.SetNearbyLadder(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PlayerMovement player))
            return;

        if (playerInRange != player)
            return;

        player.SetNearbyLadder(null);

        player.IsClimbing = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;

        ExitLadder(player.transform);

        playerInRange = null;
    }

    public void TryStartClimbing()
    {
        if (playerInRange == null)
            return;

        playerInRange.IsClimbing = true;

        Rigidbody rb = playerInRange.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }

        SnapToLadder(playerInRange.transform);
    }

    private void SnapToLadder(Transform player)
    {
        player.position = snapPoint.position;
    }

    private void ExitLadder(Transform player)
    {
        Vector3 dir = (player.position - snapPoint.position).normalized;
        player.position += dir * exitOffset;
    }
}