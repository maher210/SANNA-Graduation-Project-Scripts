using UnityEngine;

public class HideableObject : MonoBehaviour
{
    [Header("Cover Settings")]
    public Transform coverForward;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player))
        {
            player.SetHideableObject(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player))
        {
            player.SetHideableObject(null);
        }
    }

    public bool IsPlayerHiddenFrom(Transform player, Transform enemy)
    {
        Vector3 origin = coverForward.position;

        Vector3 dirToEnemy = (enemy.position - origin).normalized;
        Vector3 dirToPlayer = (player.position - origin).normalized;

        float enemyDot = Vector3.Dot(coverForward.forward, dirToEnemy);
        float playerDot = Vector3.Dot(coverForward.forward, dirToPlayer);

        return playerDot < 0f;
    }

    void Awake()
    {
        if (coverForward == null)
            coverForward = transform;
    }

}