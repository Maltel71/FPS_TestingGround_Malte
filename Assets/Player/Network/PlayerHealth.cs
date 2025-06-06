using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    // Use NetworkVariable for networking
    private NetworkVariable<float> networkCurrentHealth = new NetworkVariable<float>(100f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Inspector-visible field (will update automatically)
    [Header("Debug - Current Health")]
    [SerializeField] private float inspectorCurrentHealth;

    // Keep this for compatibility with existing scripts
    public float currentHealth => networkCurrentHealth.Value;

    [Header("UI (Optional)")]
    public Slider healthBar;
    public Text healthText;

    [Header("Effects")]
    public AudioClip hurtSound;
    public AudioClip deathSound;

    private AudioSource audioSource;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        // Initialize health
        if (IsServer)
        {
            networkCurrentHealth.Value = maxHealth;
        }

        // Initialize inspector field
        inspectorCurrentHealth = networkCurrentHealth.Value;

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Subscribe to health changes
        networkCurrentHealth.OnValueChanged += OnHealthChanged;

        UpdateHealthUI();
    }

    public override void OnNetworkDespawn()
    {
        networkCurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        // Update inspector field
        inspectorCurrentHealth = newValue;

        UpdateHealthUI();

        // Play hurt sound when taking damage (only for owner)
        if (IsOwner && newValue < previousValue && newValue > 0)
        {
            if (hurtSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hurtSound);
            }
        }

        // Handle death
        if (newValue <= 0 && !isDead)
        {
            Die();
        }
    }

    // Server RPC version for networking
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damageAmount)
    {
        TakeDamage(damageAmount);
    }

    // Keep original method for compatibility
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        if (IsServer)
        {
            networkCurrentHealth.Value -= damageAmount;
            networkCurrentHealth.Value = Mathf.Clamp(networkCurrentHealth.Value, 0f, maxHealth);
        }

        Debug.Log("Player took " + damageAmount + " damage. Health: " + currentHealth);
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        if (IsServer)
        {
            networkCurrentHealth.Value += healAmount;
            networkCurrentHealth.Value = Mathf.Clamp(networkCurrentHealth.Value, 0f, maxHealth);
        }

        Debug.Log("Player healed for " + healAmount + ". Health: " + currentHealth);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        // Play death sound (only for owner)
        if (IsOwner && deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        Debug.Log("Player died!");

        // Add death logic here (respawn, game over screen, etc.)
        // For example:
        // GameManager.Instance.PlayerDied();
        // or
        // SceneManager.LoadScene("GameOverScene");
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = currentHealth.ToString("F0") + " / " + maxHealth.ToString("F0");
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    // Additional getters for compatibility
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}