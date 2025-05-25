using UnityEngine;

public class MissileSpawner : MonoBehaviour
{
    [Header("Missile Settings")]
    public GameObject missileProjectilePrefab;
    public Transform target;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public bool autoSpawn = true;

    private Transform spawnPoint;
    private float nextSpawnTime;

    void Start()
    {
        // Find the spawn point child object
        spawnPoint = transform.Find("MissileSpawnPoint");

        if (spawnPoint == null)
        {
            Debug.LogError("MissileSpawnPoint not found as child of " + gameObject.name);
        }

        if (missileProjectilePrefab == null)
        {
            Debug.LogError("Missile Projectile Prefab not assigned on " + gameObject.name);
        }
    }

    void Update()
    {
        if (autoSpawn && Time.time >= nextSpawnTime)
        {
            SpawnMissile();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void SpawnMissile()
    {
        if (spawnPoint == null || missileProjectilePrefab == null)
        {
            Debug.LogWarning("Cannot spawn missile - missing spawn point or prefab");
            return;
        }

        // Spawn the missile at the spawn point
        GameObject missile = Instantiate(missileProjectilePrefab, spawnPoint.position, spawnPoint.rotation);

        // Get the homing script and set the target
        HomingMissile homingScript = missile.GetComponent<HomingMissile>();
        if (homingScript != null && target != null)
        {
            homingScript.SetTarget(target);
        }
        else if (target == null)
        {
            Debug.LogWarning("No target assigned to missile spawner");
        }
    }

    // Method to manually spawn a missile (can be called from UI or other scripts)
    public void FireMissile()
    {
        SpawnMissile();
    }
}