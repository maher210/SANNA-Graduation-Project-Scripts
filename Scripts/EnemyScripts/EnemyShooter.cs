using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shoot Settings")]
    [SerializeField] private float shootCooldown = 1f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
    }

    public void TryShoot(PlayerMovement player)
    {
        if (player == null)
            return;

        if (timer < shootCooldown)
            return;

        timer = 0f;

        Debug.Log("Enemy SHOOTS 🔫");

        player.Die();
    }

    public void ResetShooter()
    {
        timer = 0f;
    }
}