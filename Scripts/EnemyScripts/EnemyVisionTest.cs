using UnityEngine;

public class EnemyVisionTest : MonoBehaviour
{
    public Transform player;
    public float baseViewDistance = 2f;

    void Update()
    {
        
        float distance = Vector3.Distance(transform.position, player.position);

        PlayerVisibility vis = player.GetComponent<PlayerVisibility>();

        // كل ما اللاعب أوضح → العدو يشوفه من مسافة أكبر
        float effectiveViewDistance = baseViewDistance + vis.visibility;

        if (distance < effectiveViewDistance)
        {
            if (CanSeePlayer())
            {
                transform.LookAt(player);
                Debug.Log("👀 Enemy sees you!");
            }
            else
            {
                Debug.Log("🧱 Player behind wall");
            }
        }
    }
     bool CanSeePlayer()
    {

        
        Vector3 origin = transform.position + Vector3.up * 1.5f; // ارتفاع العين
        Vector3 direction = (player.position - origin).normalized;

        Debug.DrawRay(origin, direction * 10f, Color.green); // عشان اشوف انه العدو شايفني

        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit))
        {
            if (hit.transform == player)
            {
                return true; // شاف اللاعب
            }
        }

        return false; // في جدار
    }

    //عشان اعرف لوين بقدر يشوف العدو
    void OnDrawGizmos()
    {
        if (player == null) return;

        PlayerVisibility vis = player.GetComponent<PlayerVisibility>();

        float effectiveViewDistance = baseViewDistance + vis.visibility;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, effectiveViewDistance);

        
    }

    
}
