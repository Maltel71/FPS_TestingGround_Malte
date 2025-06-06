using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text healthText; // Optional: display current/max health
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        // Auto-find components if not assigned
        if (healthBarFill == null)
            healthBarFill = GetComponentInChildren<Image>();

        // Find the LOCAL player's health (the one we own)
        if (playerHealth == null)
        {
            FindLocalPlayerHealth();
        }

        // Initialize UI
        UpdateHealthUI();
    }

    private void FindLocalPlayerHealth()
    {
        // Find all PlayerHealth components in the scene
        PlayerHealth[] allPlayerHealths = FindObjectsOfType<PlayerHealth>();

        foreach (PlayerHealth health in allPlayerHealths)
        {
            // Check if this is the local player (the one we control)
            if (health.IsOwner)
            {
                playerHealth = health;
                break;
            }
        }
    }

    private void Update()
    {
        // If we still don't have a reference, try to find it again
        if (playerHealth == null)
        {
            FindLocalPlayerHealth();
        }

        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (playerHealth != null && healthBarFill != null)
        {
            // Calculate fill amount (0 to 1)
            float fillAmount = playerHealth.GetHealthPercentage();
            healthBarFill.fillAmount = fillAmount;

            // Optional: Update text display
            if (healthText != null)
            {
                healthText.text = $"{playerHealth.currentHealth:F0} / {playerHealth.maxHealth:F0}";
            }
        }
    }
}