using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    [SerializeField] private Transform playerSpawnPoint;
    private PlayerMovement player;

    private Room currentRoom;

    private Room[] allRooms;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCurrentRoom(Room room)
    {
        currentRoom = room;
        currentRoom.SaveRoomState();
        Debug.Log("Current Room set to: " + room.name);
    }

    [SerializeField] private Room startingRoom;

    private void Start()
    {
        allRooms = FindObjectsByType<Room>();
        player = Object.FindAnyObjectByType<PlayerMovement>();

        if (currentRoom == null)
            currentRoom = startingRoom;
            currentRoom.SaveRoomState();
    }

    // ========================
    // REGISTER OBJECTS
    // ========================

    // Your GameManager stays simple and optimized!
    public void ResetArea()
    {
        Debug.Log("Reset Room");

        if (currentRoom != null)
        {
            foreach (Room room in allRooms)
            {
                room.ResetRoom();
            }
            ResetPlayer(currentRoom.GetSpawnPoint());
        }
        else
        {
            Debug.LogWarning("No room set! Using default spawn");
            ResetPlayer(playerSpawnPoint.position);
        }
    }

    private void ResetPlayer(Vector3 spawnPos)
    {
        if (player == null)
            return;

        player.gameObject.SetActive(true);
        player.transform.position = spawnPos;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        
    }

 
}