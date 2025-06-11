using UnityEngine;

public class TreeLog : MonoBehaviour
{
    [Header("Log Settings")]
    public int maxHealth = 3;
    [SerializeField] private int currentHealth; // Shows in inspector
    public GameObject stickPrefab;

    [Header("Stick Spawn Points")]
    public Transform[] stickSpawnPoints = new Transform[3];

    [Header("Effects")]
    public ParticleSystem breakEffect;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip breakSound;

    private Rigidbody rb;

    void Start()
    {
        currentHealth = maxHealth;

        // If no AudioSource assigned, try to get one from this GameObject
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();

        // Make sure log is on the tree layer so it can be hit by axe
        gameObject.layer = 8;

        // Add some random force when spawned for realistic falling
        if (rb != null)
        {
            Vector3 randomForce = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(1f, 3f),
                Random.Range(-2f, 2f)
            );
            rb.AddForce(randomForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Play effects
        if (breakEffect != null)
            breakEffect.Play();

        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        Debug.Log($"Log health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            BreakLog();
        }
    }

    void BreakLog()
    {
        if (stickPrefab != null && stickSpawnPoints.Length > 0)
        {
            // Spawn sticks at specific positions
            for (int i = 0; i < stickSpawnPoints.Length; i++)
            {
                if (stickSpawnPoints[i] != null)
                {
                    GameObject stick = Instantiate(stickPrefab, stickSpawnPoints[i].position, stickSpawnPoints[i].rotation);

                    // Add slight force to make sticks move naturally
                    Rigidbody stickRb = stick.GetComponent<Rigidbody>();
                    if (stickRb != null)
                    {
                        Vector3 spreadForce = Random.insideUnitSphere * 1f;
                        spreadForce.y = Mathf.Abs(spreadForce.y); // Force upward
                        stickRb.AddForce(spreadForce, ForceMode.Impulse);
                    }

                    Debug.Log($"Spawned stick {i + 1} at {stickSpawnPoints[i].name}");
                }
                else
                {
                    Debug.LogWarning($"Stick spawn point {i} is not assigned!");
                }
            }
        }
        else
        {
            Debug.LogWarning("Stick prefab or spawn points not assigned!");
        }

        Debug.Log("Log broke into sticks!");
        Destroy(gameObject);
    }

    // Show health info in inspector during play mode
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
    }
}