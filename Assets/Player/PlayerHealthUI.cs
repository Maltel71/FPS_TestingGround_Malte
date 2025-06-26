using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Health UI References")]
    [SerializeField] private Image healthAmountPanel; // The health bar image (like bulletAmountPanel for ammo)

    [Header("Death Screen References")]
    [SerializeField] private Canvas deathScreenCanvas; // Canvas_DeathScreen
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Player References")]
    [SerializeField] private FirstPersonController playerController; // To disable movement
    [SerializeField] private WeaponController weaponController; // To disable weapons
    [SerializeField] private WeaponShooting weaponShooting; // To disable shooting

    private PlayerHealth playerHealth; // Reference to the PlayerHealth script
    private bool isPlayerDead = false;
    private bool justRestarted = false; // Prevent immediate death detection after restart
    private float restartCooldown = 1f; // 1 second cooldown after restart
    private float restartTime = 0f;
    private CursorLockMode originalCursorLockMode;
    private bool originalCursorVisible;

    private void Start()
    {
        // Auto-find the health bar image if not assigned
        if (healthAmountPanel == null)
            healthAmountPanel = GetComponentInChildren<Image>();

        // Auto-find death screen canvas if not assigned
        if (deathScreenCanvas == null)
            deathScreenCanvas = GameObject.Find("Canvas_DeathScreen")?.GetComponent<Canvas>();

        // Auto-find player components
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();

            if (playerController == null)
                playerController = player.GetComponent<FirstPersonController>();

            if (weaponController == null)
                weaponController = FindObjectOfType<WeaponController>();
        }

        // Auto-find buttons in death screen
        if (deathScreenCanvas != null)
        {
            if (restartButton == null)
                restartButton = deathScreenCanvas.GetComponentInChildren<Button>();

            Button[] buttons = deathScreenCanvas.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                if (button.name.ToLower().Contains("restart"))
                    restartButton = button;
                else if (button.name.ToLower().Contains("quit"))
                    quitButton = button;
            }

            // Setup button events
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
                Debug.Log($"Restart button '{restartButton.name}' click event added");
            }
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
                Debug.Log($"Quit button '{quitButton.name}' click event added");
            }

            // Add hover effects to buttons
            SetupButtonHoverEffects();

            // Initially hide death screen
            deathScreenCanvas.gameObject.SetActive(false);
        }

        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealthUI: Could not find PlayerHealth component!");
        }

        Debug.Log($"PlayerHealthUI: Connected to PlayerHealth = {(playerHealth != null ? playerHealth.name : "null")}");
        Debug.Log($"PlayerHealthUI: Death screen found = {(deathScreenCanvas != null)}");
        Debug.Log($"PlayerHealthUI: Player health on start = {(playerHealth != null ? playerHealth.currentHealth + "/" + playerHealth.maxHealth : "N/A")}");

        // Store original cursor state
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;

        // Initialize UI and ensure player health is reset
        UpdateHealthUI();

        // Safety check: If player health is 0 on start, reset it
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            Debug.LogWarning("Player health was 0 on start - resetting to full health");
            playerHealth.currentHealth = playerHealth.maxHealth;
        }
    }

    private void Update()
    {
        UpdateHealthUI();
        CheckForPlayerDeath();

        // Debug: Press R key to restart (for testing)
        if (isPlayerDead && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Manual restart triggered with R key");
            RestartGame();
        }
    }

    private void UpdateHealthUI()
    {
        if (playerHealth == null || healthAmountPanel == null)
        {
            if (healthAmountPanel != null)
                healthAmountPanel.fillAmount = 0f; // Show empty if no health system
            return;
        }

        // Calculate fill amount (0 to 1)
        float fillAmount = playerHealth.GetHealthPercentage();
        healthAmountPanel.fillAmount = fillAmount;

        // Optional: Change color based on health level
        if (fillAmount > 0.6f)
            healthAmountPanel.color = Color.green; // Healthy
        else if (fillAmount > 0.3f)
            healthAmountPanel.color = Color.yellow; // Warning
        else
            healthAmountPanel.color = Color.red; // Critical
    }

    private void CheckForPlayerDeath()
    {
        // Don't check for death immediately after restart
        if (justRestarted && Time.time < restartTime + restartCooldown)
        {
            return;
        }
        else if (justRestarted)
        {
            justRestarted = false; // Cooldown expired
            Debug.Log("Restart cooldown expired - death detection re-enabled");
        }

        if (playerHealth != null && playerHealth.IsDead() && !isPlayerDead)
        {
            Debug.Log($"Death detected - Player health: {playerHealth.currentHealth}, isDead: {playerHealth.IsDead()}");
            ShowDeathScreen();
        }
    }

    private void ShowDeathScreen()
    {
        isPlayerDead = true;
        Debug.Log("Player died - showing death screen");

        // Show death screen
        if (deathScreenCanvas != null)
            deathScreenCanvas.gameObject.SetActive(true);

        // Disable player controls (same as pause menu)
        DisablePlayerControls();

        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DisablePlayerControls()
    {
        // Disable player movement
        if (playerController != null)
            playerController.enabled = false;

        // Disable weapons
        if (weaponController != null)
            weaponController.enabled = false;

        // Disable shooting for all active weapons
        WeaponShooting[] allWeaponShooting = FindObjectsOfType<WeaponShooting>();
        foreach (WeaponShooting weapon in allWeaponShooting)
        {
            weapon.enabled = false;
        }

        Debug.Log("Player controls disabled due to death");
    }

    private void EnablePlayerControls()
    {
        // Enable player movement
        if (playerController != null)
            playerController.enabled = true;

        // Enable weapons
        if (weaponController != null)
            weaponController.enabled = true;

        // Enable shooting for all weapons
        WeaponShooting[] allWeaponShooting = FindObjectsOfType<WeaponShooting>();
        foreach (WeaponShooting weapon in allWeaponShooting)
        {
            weapon.enabled = true;
        }

        // Restore cursor state
        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;

        Debug.Log("Player controls enabled");
    }

    public void RestartGame()
    {
        Debug.Log("=== RESTART BUTTON PRESSED ===");

        // Make sure time scale is normal
        Time.timeScale = 1f;

        // FORCE disable death screen immediately
        if (deathScreenCanvas != null)
        {
            Debug.Log($"Death screen active before disable: {deathScreenCanvas.gameObject.activeSelf}");
            deathScreenCanvas.gameObject.SetActive(false);
            Debug.Log($"Death screen active after disable: {deathScreenCanvas.gameObject.activeSelf}");
        }

        // Re-enable player controls immediately
        EnablePlayerControls();

        // Reset player state
        isPlayerDead = false;
        justRestarted = true; // Set restart flag
        restartTime = Time.time; // Record restart time

        Debug.Log("Restart cooldown started - death detection disabled for 1 second");

        // Restore cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Wait a frame then reload to ensure UI updates
        StartCoroutine(DelayedSceneReload());
    }

    private System.Collections.IEnumerator DelayedSceneReload()
    {
        Debug.Log("Waiting one frame before scene reload...");
        yield return null; // Wait one frame

        // Force garbage collection
        System.GC.Collect();

        // Get current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Reloading scene: {currentSceneName}");

        // Reload scene
        SceneManager.LoadScene(currentSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private void SetupButtonHoverEffects()
    {
        // Find PauseMenuManager for hover sounds (optional)
        PauseMenuManager pauseManager = FindObjectOfType<PauseMenuManager>();

        // Add hover effects to all buttons in death screen
        Button[] buttons = deathScreenCanvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            // Check if button already has hover effect
            ButtonHoverEffect existingEffect = button.GetComponent<ButtonHoverEffect>();
            if (existingEffect == null)
            {
                // Add hover effect component
                ButtonHoverEffect hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();

                // Initialize with settings (same as pause menu)
                hoverEffect.Initialize(
                    button.transform.localScale,  // Original scale
                    1.1f,                         // Scale amount (10% bigger)
                    10f,                          // Animation speed
                    pauseManager                  // For hover sounds
                );

                Debug.Log($"Added hover effect to button: {button.name}");
            }
        }
    }

    // Public method to manually show death screen (for testing)
    [ContextMenu("Test Death Screen")]
    public void TestDeathScreen()
    {
        ShowDeathScreen();
    }

    // Public method to manually hide death screen (for debugging)
    [ContextMenu("Force Hide Death Screen")]
    public void ForceHideDeathScreen()
    {
        if (deathScreenCanvas != null)
        {
            deathScreenCanvas.gameObject.SetActive(false);
            Debug.Log("Death screen force hidden");
        }
        EnablePlayerControls();
        isPlayerDead = false;
    }

    // Public method to set the PlayerHealth reference (alternative to auto-finding)
    public void SetPlayerHealth(PlayerHealth health)
    {
        playerHealth = health;
        Debug.Log($"PlayerHealthUI: Set PlayerHealth to {(health != null ? health.name : "null")}");
        UpdateHealthUI();
    }
}