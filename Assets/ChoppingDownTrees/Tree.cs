using UnityEngine;

public class Tree : MonoBehaviour
{
    [Header("Tree Settings")]
    public int maxHealth = 5;
    [SerializeField] private int currentHealth; // Shows in inspector
    public GameObject treeLogPrefab;

    [Header("Log Spawn Points")]
    public Transform[] logSpawnPoints = new Transform[3];

    [Header("Effects")]
    public ParticleSystem chopEffect;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip chopSound;
    public AudioClip treeFallingSound;

    private bool isDestroyed = false; // Prevent multiple calls

    void Start()
    {
        currentHealth = maxHealth;

        // If no AudioSource assigned, try to get one from this GameObject
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Make sure tree is on the correct layer
        gameObject.layer = 8;
    }

    public void TakeDamage(int damage)
    {
        // Prevent damage if already destroyed
        if (isDestroyed) return;

        currentHealth -= damage;

        // Play effects
        if (chopEffect != null)
            chopEffect.Play();

        if (audioSource != null && chopSound != null)
            audioSource.PlayOneShot(chopSound);

        Debug.Log($"Tree health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            FellTree();
        }
    }

    void FellTree()
    {
        // Prevent multiple calls
        if (isDestroyed) return;
        isDestroyed = true;

        // Spawn logs immediately
        SpawnLogs();

        // Hide the tree and disable collision
        HideTreeComponents();

        // Play falling sound and destroy after sound finishes
        if (audioSource != null && treeFallingSound != null)
        {
            audioSource.PlayOneShot(treeFallingSound);
            StartCoroutine(DestroyAfterSound(treeFallingSound.length));
        }
        else
        {
            // No sound, destroy after short delay
            StartCoroutine(DestroyAfterSound(0.1f));
        }
    }

    void HideTreeComponents()
    {
        // Hide all mesh renderers
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disable all colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Disable particle effect if it exists
        if (chopEffect != null)
            chopEffect.gameObject.SetActive(false);
    }

    System.Collections.IEnumerator DestroyAfterSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void SpawnLogs()
    {
        if (treeLogPrefab != null && logSpawnPoints.Length > 0)
        {
            // Spawn logs at specific positions
            for (int i = 0; i < logSpawnPoints.Length; i++)
            {
                if (logSpawnPoints[i] != null)
                {
                    GameObject log = Instantiate(treeLogPrefab, logSpawnPoints[i].position, logSpawnPoints[i].rotation);
                    Debug.Log($"Spawned log {i + 1} at {logSpawnPoints[i].name}");
                }
                else
                {
                    Debug.LogWarning($"Log spawn point {i} is not assigned!");
                }
            }

            Debug.Log("Tree fell down!");
        }
        else
        {
            Debug.LogWarning("TreeLog prefab or spawn points not assigned!");
        }
    }

    // Show health info in inspector during play mode
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            // Keep currentHealth in sync during play
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
    }
}