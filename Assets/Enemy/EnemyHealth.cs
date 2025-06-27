using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Effects")]
    public GameObject deathEffectPrefab;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public float deathAnimationTime = 2f; // How long death animation takes
    public float deathDelay = 1f; // Extra delay after animation before destroying

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
                enemyAI.TakeDamage(damageAmount); // Pass damage to AI
            }
        }

        Debug.Log($"Enemy took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name} died!");

        // Start the death sequence
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 1. DON'T disable AI immediately - let it handle death animation
        // The AI script will detect IsDead() and stop movement/attacks itself

        // 2. Play death sound immediately
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 3. Trigger death animation (EnemyAI will handle this when it detects IsDead())
        // The animation system will automatically trigger when it sees isDead = true

        // 4. Drop items immediately (so they don't disappear with the enemy)
        DropItems();

        // 5. Wait for death animation to complete
        yield return new WaitForSeconds(deathAnimationTime);

        // 6. Spawn death effect after animation
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }

        // 7. Disable physics and visual components (but keep object for animation)
        DisablePhysicsAndVisuals();

        // 8. Disable AI after animation completes
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // 9. Wait additional delay if needed
        if (deathDelay > 0)
        {
            yield return new WaitForSeconds(deathDelay);
        }

        // 10. Finally destroy the entire game object
        Destroy(gameObject);
    }

    void DisablePhysicsAndVisuals()
    {
        // Disable movement and collisions
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Disable colliders (but keep the object for animation)
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Optional: You can choose to hide mesh renderers here or let the animation handle it
        // MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        // foreach (MeshRenderer renderer in renderers)
        // {
        //     renderer.enabled = false;
        // }
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

    // Method to set health directly (useful for initialization)
    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
    }

    // Public getters
    public bool IsDead() { return isDead; }
    public float GetHealthPercentage() { return currentHealth / maxHealth; }
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }

    // Method to get total death time (useful for other systems)
    public float GetTotalDeathTime() { return deathAnimationTime + deathDelay; }
}