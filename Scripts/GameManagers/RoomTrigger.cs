using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    [SerializeField] private Room room;

    private void OnTriggerEnter(Collider other)
    {
        if (room == null)
        {
            Debug.LogWarning("Room not assigned!");
            return;
        }


        if (!other.TryGetComponent(out PlayerMovement player))
            return;

        GameManager.Instance.SetCurrentRoom(room);

        Debug.Log("Entered Room: " + room.name);
        
    }
}