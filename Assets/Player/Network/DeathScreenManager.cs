using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class DeathScreenManager : MonoBehaviour
{
    [Header("Death Screen References")]
    [SerializeField] private GameObject canvasDeathScreen;
    [SerializeField] private Text deathText;
    [SerializeField] private Button respawnButton; // Optional: if you want a respawn button

    [Header("Settings")]
    [SerializeField] private float respawnDelay = 5f; // Auto-respawn after 5 seconds
    [SerializeField] private string deathMessage = "You Died";

    private PlayerHealth playerHealth;
    private bool isDeathScreenActive = false;

    private void Start()
    {
        // Hide death screen initially
        if (canvasDeathScreen != null)
            canvasDeathScreen.SetActive(false);

        // Don't try to find player immediately - wait for network spawn
        // FindLocalPlayerHealth() will be called in Update()

        // Setup respawn button if it exists
        if (respawnButton != null)
        {
            respawnButton.onClick.AddListener(RequestRespawn);
        }

        Debug.Log("DeathScreenManager started - waiting for local player to spawn");
    }

    private void FindLocalPlayerHealth()
    {
        // Find all PlayerHealth components in the scene
        PlayerHealth[] allPlayerHealths = FindObjectsOfType<PlayerHealth>();

        foreach (PlayerHealth health in allPlayerHealths)
        {
            // Check if this is the local player (the one we control)
            // Make sure the NetworkBehaviour is spawned and we own it
            if (health.IsSpawned && health.IsOwner)
            {
                playerHealth = health;
                Debug.Log($"Found local player health component on {health.gameObject.name}");
                break;
            }
        }

        if (playerHealth == null)
        {
            Debug.Log("No local player health found yet - players may still be spawning");
        }
    }

    private void Update()
    {
        // If we don't have a player health reference, try to find it
        if (playerHealth == null)
        {
            FindLocalPlayerHealth();
            return;
        }

        // Verify the player health is still valid and owned by us
        if (!playerHealth.IsSpawned || !playerHealth.IsOwner)
        {
            playerHealth = null;
            return;
        }

        // Check if player died and death screen isn't already shown
        if (playerHealth.IsDead() && !isDeathScreenActive)
        {
            ShowDeathScreen();
        }
        // Check if player is alive and death screen is shown (for respawn)
        else if (!playerHealth.IsDead() && isDeathScreenActive)
        {
            HideDeathScreen();
        }
    }

    private void ShowDeathScreen()
    {
        if (canvasDeathScreen == null) return;

        isDeathScreenActive = true;
        canvasDeathScreen.SetActive(true);

        // Set death message
        if (deathText != null)
        {
            deathText.text = deathMessage;
        }

        // Unlock cursor so player can interact with UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Optional: Start auto-respawn timer
        if (respawnDelay > 0)
        {
            Invoke(nameof(RequestRespawn), respawnDelay);
        }

        Debug.Log("Death screen shown for local player");
    }

    private void HideDeathScreen()
    {
        if (canvasDeathScreen == null) return;

        isDeathScreenActive = false;
        canvasDeathScreen.SetActive(false);

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Cancel any pending auto-respawn
        CancelInvoke(nameof(RequestRespawn));

        Debug.Log("Death screen hidden for local player");
    }

    private void RequestRespawn()
    {
        // Cancel the auto-respawn invoke if it was called manually
        CancelInvoke(nameof(RequestRespawn));

        if (playerHealth != null && playerHealth.IsOwner)
        {
            // Request respawn from server
            // You'll need to implement this in your PlayerHealth or a GameManager
            // playerHealth.RespawnServerRpc();

            // For now, just heal the player to full health as a simple respawn
            if (NetworkManager.Singleton.IsServer)
            {
                playerHealth.Heal(playerHealth.GetMaxHealth());
            }
            else
            {
                // If not server, request respawn via ServerRpc
                // This assumes you add a RespawnServerRpc method to PlayerHealth
                Debug.Log("Requesting respawn from server...");
            }
        }
    }

    // Optional: Method to customize death message
    public void SetDeathMessage(string message)
    {
        deathMessage = message;
        if (deathText != null && isDeathScreenActive)
        {
            deathText.text = message;
        }
    }

    // Optional: Method to check if death screen is currently active
    public bool IsDeathScreenActive()
    {
        return isDeathScreenActive;
    }
}