using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [Header("Room Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayerMovement player;
    private LanternSystem lantern;
    private List<Trap> traps = new List<Trap>();
    private List<LightSource> lights = new List<LightSource>();
    private List<EnemyChasing> enemies = new List<EnemyChasing>();
    private List<SecurityCamera> cameras = new List<SecurityCamera>();
    private List<CameraSwitch> camSwitches = new List<CameraSwitch>();
    private List<GateSwitch> gateSwitches = new List<GateSwitch>(); // NEW: Gate switch storage

    private void Awake()
    {
        RegisterObjects();
        lantern = FindAnyObjectByType<LanternSystem>();
    }

    private void RegisterObjects()
    {
        traps.AddRange(GetComponentsInChildren<Trap>(true));
        lights.AddRange(GetComponentsInChildren<LightSource>(true));
        camSwitches.AddRange(GetComponentsInChildren<CameraSwitch>(true));
        enemies.AddRange(GetComponentsInChildren<EnemyChasing>(true));
        cameras.AddRange(GetComponentsInChildren<SecurityCamera>(true));

        // --- NEW: Automatically find all gate switches in this room ---
        gateSwitches.AddRange(GetComponentsInChildren<GateSwitch>(true));
    }

    public void ResetRoom()
    {
        foreach (var trap in traps)
            if (trap != null) trap.ResetTrap();

        foreach (var light in lights)
            if (light != null) light.ResetLight();

        foreach (var enemy in enemies)
            if (enemy != null) enemy.ResetEnemy();

        foreach (var cam in cameras)
            if (cam != null) cam.ResetCamera();

        foreach (var camSwitch in camSwitches)
            if (camSwitch != null) camSwitch.ResetCameraSwitch();

        // --- NEW: Reset all gates and their switches ---
        foreach (var gateSwitch in gateSwitches)
            if (gateSwitch != null) gateSwitch.ResetGateSwitch();

        if (lantern != null)
         lantern.ResetLantern();
    }

    public Vector3 GetSpawnPoint()
    {
        if (spawnPoint == null)
            return transform.position;

        return spawnPoint.position;
    }

    public void SaveRoomState()
        {
            if (lantern != null)
            {
                lantern.SaveRoomOil();
            }
        }
}