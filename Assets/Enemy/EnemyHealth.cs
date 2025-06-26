using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Effects")]
    public GameObject deathEffectPrefab;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public float deathDelay = 2f;

    [Header("Drops")]
    public GameObject[] itemDrops; // Items to drop on death
    public float dropForce = 5f;
    public int minDrops = 0;
    public int maxDrops = 2;

    private AudioSource audioSource;
    private bool isDead = false;
    private EnemyAI enemyAI;

    void Start()
    {
        currentHealth = maxHealth;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        enemyAI = GetComponent<EnemyAI>();
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Play hurt sound
            if (hurtSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hurtSound);
            }

            // Force enemy to detect player when damaged
            if (enemyAI != null)
            {
                enemyAI.ForceDetectPlayer();
            }
        }

        Debug.Log($"Enemy took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        // Disable AI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }

        // Drop items
        DropItems();

        // Disable movement and collisions
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Hide mesh renderers
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disable colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Debug.Log("Enemy died!");

        // Destroy after delay
        Destroy(gameObject, deathDelay);
    }

    void DropItems()
    {
        if (itemDrops.Length == 0) return;

        int dropCount = Random.Range(minDrops, maxDrops + 1);

        for (int i = 0; i < dropCount; i++)
        {
            GameObject itemToDrop = itemDrops[Random.Range(0, itemDrops.Length)];
            Vector3 dropPosition = transform.position + Vector3.up * 1f + Random.insideUnitSphere * 0.5f;

            GameObject droppedItem = Instantiate(itemToDrop, dropPosition, Quaternion.identity);

            // Add some force to make it scatter
            Rigidbody dropRb = droppedItem.GetComponent<Rigidbody>();
            if (dropRb != null)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = Mathf.Abs(randomDirection.y); // Force upward
                dropRb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
            }
        }
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public bool IsDead() { return isDead; }
    public float GetHealthPercentage() { return currentHealth / maxHealth; }
}