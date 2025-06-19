using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingBlocksMenu : MonoBehaviour
{
    [Header("Database")]
    public BuildingBlockDatabase blockDatabase;

    [Header("UI References")]
    public GameObject menuPanel;
    public Button closeButton;

    [Header("Manual Block Buttons - Design These Yourself!")]
    public BuildingBlockButton[] blockButtons; // Drag your designed buttons here

    [Header("Resource Display")]
    public TextMeshProUGUI woodDisplayText;
    public TextMeshProUGUI stoneDisplayText;
    public TextMeshProUGUI clayDisplayText;

    [Header("Auto-Close Settings")]
    public bool autoCloseOnSelection = true; // Toggle for auto-close behavior

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public AudioClip blockSelectSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;
    public AudioClip insufficientResourcesSound; // Add this for better feedback

    [Header("References")]
    public SnapBuildingSystem buildingSystem;

    private int selectedBlockIndex = 0;
    private bool isMenuOpen = false;

    // Store original cursor state
    private CursorLockMode originalCursorLockMode;
    private bool originalCursorVisible;

    void Start()
    {
        // Find building system if not assigned
        if (buildingSystem == null)
            buildingSystem = FindObjectOfType<SnapBuildingSystem>();

        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);

        // Setup manual buttons
        SetupManualButtons();

        // Initially hide menu
        menuPanel.SetActive(false);

        // Update resource display
        UpdateResourceDisplay();

        // Store initial cursor state
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;
    }

    void SetupManualButtons()
    {
        // Setup each manually designed button
        for (int i = 0; i < blockButtons.Length; i++)
        {
            if (blockButtons[i] != null && blockDatabase != null && i < blockDatabase.GetBlockCount())
            {
                BuildingBlockData blockData = blockDatabase.GetBlockData(i);
                int buttonIndex = i; // Capture for closure

                // Setup the button with data
                blockButtons[i].SetupManual(blockData, buttonIndex, this);
            }
        }

        // Select first block by default
        if (blockButtons.Length > 0)
        {
            SelectBlock(0);
        }
    }

    void Update()
    {
        // Update resource display regularly
        UpdateResourceDisplay();

        // Handle input for opening/closing menu in building mode
        if (buildingSystem != null && buildingSystem.IsBuildingMode())
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                if (isMenuOpen)
                    CloseMenu();
                else
                    OpenMenu();
            }
        }
    }

    public void OpenMenu()
    {
        if (isMenuOpen) return;

        isMenuOpen = true;
        menuPanel.SetActive(true);

        // Store current cursor state before changing it
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;

        // Unlock cursor for menu interaction (same as pause menu)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play open sound
        PlaySound(menuOpenSound);

        // Update resource display
        UpdateResourceDisplay();

        Debug.Log("Building Blocks Menu opened - Player look controls disabled");
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        isMenuOpen = false;
        menuPanel.SetActive(false);

        // Restore original cursor state (same as pause menu)
        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;

        // Play close sound
        PlaySound(menuCloseSound);

        Debug.Log("Building Blocks Menu closed - Player look controls restored");
    }

    public void OnBlockButtonClicked(int blockIndex)
    {
        Debug.Log($"=== BLOCK BUTTON CLICKED === Index: {blockIndex}");

        // Always play the button click sound first
        PlaySound(buttonClickSound);

        // Select the block for visual feedback
        SelectBlock(blockIndex);

        if (blockDatabase == null)
        {
            Debug.LogError("Block database is null!");
            if (autoCloseOnSelection) CloseMenu();
            return;
        }

        BuildingBlockData selectedBlock = blockDatabase.GetBlockData(blockIndex);
        if (selectedBlock == null)
        {
            Debug.LogError($"No block data found for index {blockIndex}");
            if (autoCloseOnSelection) CloseMenu();
            return;
        }

        Debug.Log($"Block data found: {selectedBlock.blockName}");

        // Check if we can afford it
        bool canAfford = true;
        if (ResourceManager.Instance != null)
        {
            int currentWood = ResourceManager.Instance.GetWood();
            int currentStone = ResourceManager.Instance.GetStone();
            int currentClay = ResourceManager.Instance.GetClay();

            Debug.Log($"Current resources - Wood: {currentWood}, Stone: {currentStone}, Clay: {currentClay}");
            Debug.Log($"Block cost - Wood: {selectedBlock.woodCost}, Stone: {selectedBlock.stoneCost}, Clay: {selectedBlock.clayCost}");

            canAfford = selectedBlock.CanAfford(currentWood, currentStone, currentClay);

            Debug.Log($"Can afford: {canAfford}");

            if (!canAfford)
            {
                Debug.Log($"Not enough resources for {selectedBlock.blockName}. Cost: {selectedBlock.GetCostString()}");

                // Play insufficient resources sound
                PlaySound(insufficientResourcesSound);

                // Don't close menu if can't afford - let player see what they need
                return;
            }
        }

        // Set the building system to use this block
        if (buildingSystem != null)
        {
            Debug.Log("Setting selected block in building system...");
            buildingSystem.SetSelectedBlock(blockIndex);
        }
        else
        {
            Debug.LogError("Building system is null!");
        }

        // Play selection sound
        PlaySound(blockSelectSound);

        Debug.Log($"Selected block: {selectedBlock.blockName}");

        // Close menu if auto-close is enabled
        Debug.Log($"Auto close enabled: {autoCloseOnSelection}");
        if (autoCloseOnSelection)
        {
            Debug.Log("Attempting to close menu...");
            CloseMenu();
        }
    }

    void SelectBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockDatabase == null || blockIndex >= blockDatabase.GetBlockCount())
            return;

        selectedBlockIndex = blockIndex;

        // Update button highlights
        UpdateButtonHighlights();
    }

    void UpdateButtonHighlights()
    {
        for (int i = 0; i < blockButtons.Length; i++)
        {
            if (blockButtons[i] != null)
                blockButtons[i].SetSelected(i == selectedBlockIndex);
        }
    }

    void UpdateResourceDisplay()
    {
        if (ResourceManager.Instance == null) return;

        if (woodDisplayText != null)
            woodDisplayText.text = ResourceManager.Instance.GetWood().ToString();

        if (stoneDisplayText != null)
            stoneDisplayText.text = ResourceManager.Instance.GetStone().ToString();

        if (clayDisplayText != null)
            clayDisplayText.text = ResourceManager.Instance.GetClay().ToString();
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // Public methods for external access
    public bool IsMenuOpen() => isMenuOpen;
    public int GetSelectedBlockIndex() => selectedBlockIndex;

    // Method to force close menu (useful for external scripts)
    public void ForceCloseMenu()
    {
        CloseMenu();
    }

    // Method to toggle auto-close behavior at runtime
    public void SetAutoCloseOnSelection(bool autoClose)
    {
        autoCloseOnSelection = autoClose;
    }
}