using UnityEngine;

/// <summary>
/// Manages sound detection for enemies. Notifies all enemies when player makes sounds.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private EnemyAI[] enemies;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find all enemies in scene
        RefreshEnemyList();
    }

    /// <summary>
    /// Refresh the list of enemies (call this when enemies are spawned/destroyed)
    /// </summary>
    public void RefreshEnemyList()
    {
        enemies = FindObjectsOfType<EnemyAI>();
        if (showDebugLogs)
            Debug.Log($"SoundManager: Found {enemies.Length} enemies to notify of sounds");
    }

    /// <summary>
    /// Notify all enemies of a gunshot at the given position
    /// </summary>
    public void NotifyGunshotSound(Vector3 shotPosition)
    {
        if (enemies == null || enemies.Length == 0)
        {
            RefreshEnemyList();
        }

        int notifiedCount = 0;
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.OnGunshotHeard(shotPosition);
                notifiedCount++;
            }
        }

        if (showDebugLogs)
            Debug.Log($"SoundManager: Notified {notifiedCount} enemies of gunshot at {shotPosition}");
    }

    /// <summary>
    /// Call this method when an enemy is destroyed to clean up null references
    /// </summary>
    public void OnEnemyDestroyed()
    {
        // Remove null references from the array
        System.Collections.Generic.List<EnemyAI> validEnemies = new System.Collections.Generic.List<EnemyAI>();

        if (enemies != null)
        {
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy != null)
                    validEnemies.Add(enemy);
            }
        }

        enemies = validEnemies.ToArray();
    }

    /// <summary>
    /// Add a new enemy to the notification list
    /// </summary>
    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy == null) return;

        // Check if enemy is already registered
        if (enemies != null)
        {
            foreach (EnemyAI existingEnemy in enemies)
            {
                if (existingEnemy == enemy)
                    return; // Already registered
            }
        }

        // Add to array
        System.Collections.Generic.List<EnemyAI> enemyList = new System.Collections.Generic.List<EnemyAI>();
        if (enemies != null)
            enemyList.AddRange(enemies);

        enemyList.Add(enemy);
        enemies = enemyList.ToArray();

        if (showDebugLogs)
            Debug.Log($"SoundManager: Registered new enemy {enemy.name}");
    }

    /// <summary>
    /// Get the number of active enemies
    /// </summary>
    public int GetEnemyCount()
    {
        if (enemies == null) return 0;

        int activeCount = 0;
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                activeCount++;
        }
        return activeCount;
    }
}