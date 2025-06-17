using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Resources;

public class BuildingBlocksMenu : MonoBehaviour
{
    [Header("Database")]
    public BuildingBlockDatabase blockDatabase;

    [Header("UI References")]
    public GameObject menuPanel;
    public Transform buttonContainer;
    public GameObject buttonPrefab;
    public Button closeButton;

    [Header("Block Info Panel")]
    public GameObject blockInfoPanel;
    public Image blockIconImage;
    public TextMeshProUGUI blockNameText;
    public TextMeshProUGUI blockDescriptionText;
    public TextMeshProUGUI blockCostText;
    public Button selectBlockButton;

    [Header("Resource Display")]
    public TextMeshProUGUI woodDisplayText;
    public TextMeshProUGUI stoneDisplayText;
    public TextMeshProUGUI clayDisplayText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public AudioClip blockSelectSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;

    [Header("References")]
    public SnapBuildingSystem buildingSystem;

    private List<BuildingBlockButton> blockButtons = new List<BuildingBlockButton>();
    private int selectedBlockIndex = 0;
    private bool isMenuOpen = false;

    void Start()
    {
        // Find building system if not assigned
        if (buildingSystem == null)
            buildingSystem = FindObjectOfType<SnapBuildingSystem>();

        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);

        // Setup select block button
        if (selectBlockButton != null)
            selectBlockButton.onClick.AddListener(SelectCurrentBlock);

        // Create block buttons
        CreateBlockButtons();

        // Initially hide menu
        menuPanel.SetActive(false);

        // Update resource display
        UpdateResourceDisplay();
    }

    void Update()
    {
        // Update resource display regularly
        UpdateResourceDisplay();

        // Handle input for opening menu (only in building mode)
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

    void CreateBlockButtons()
    {
        if (blockDatabase == null || buttonContainer == null || buttonPrefab == null)
        {
            Debug.LogWarning("BuildingBlocksMenu: Missing required references!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
        blockButtons.Clear();

        // Create button for each block
        for (int i = 0; i < blockDatabase.GetBlockCount(); i++)
        {
            BuildingBlockData blockData = blockDatabase.GetBlockData(i);
            if (blockData == null) continue;

            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            BuildingBlockButton blockButton = buttonObj.GetComponent<BuildingBlockButton>();

            if (blockButton == null)
                blockButton = buttonObj.AddComponent<BuildingBlockButton>();

            blockButton.Setup(blockData, i, this);
            blockButtons.Add(blockButton);
        }

        // Select first block by default
        if (blockButtons.Count > 0)
        {
            SelectBlock(0);
        }
    }

    public void OpenMenu()
    {
        if (isMenuOpen) return;

        isMenuOpen = true;
        menuPanel.SetActive(true);

        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play open sound
        PlaySound(menuOpenSound);

        // Update resource display
        UpdateResourceDisplay();

        Debug.Log("Building Blocks Menu opened");
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        isMenuOpen = false;
        menuPanel.SetActive(false);

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Play close sound
        PlaySound(menuCloseSound);

        Debug.Log("Building Blocks Menu closed");
    }

    public void OnBlockButtonClicked(int blockIndex)
    {
        SelectBlock(blockIndex);
        PlaySound(buttonClickSound);
    }

    void SelectBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= blockDatabase.GetBlockCount())
            return;

        selectedBlockIndex = blockIndex;
        BuildingBlockData blockData = blockDatabase.GetBlockData(blockIndex);

        // Update block info panel
        UpdateBlockInfoPanel(blockData);

        // Update button highlights
        UpdateButtonHighlights();
    }

    void UpdateBlockInfoPanel(BuildingBlockData blockData)
    {
        if (blockInfoPanel == null) return;

        if (blockIconImage != null)
            blockIconImage.sprite = blockData.blockIcon;

        if (blockNameText != null)
            blockNameText.text = blockData.blockName;

        if (blockDescriptionText != null)
            blockDescriptionText.text = blockData.description;

        if (blockCostText != null)
        {
            blockCostText.text = blockData.GetCostString();

            // Color code based on affordability
            if (ResourceManager.Instance != null)
            {
                bool canAfford = blockData.CanAfford(
                    ResourceManager.Instance.GetWood(),
                    ResourceManager.Instance.GetStone(),
                    ResourceManager.Instance.GetClay()
                );

                blockCostText.color = canAfford ? Color.green : Color.red;
            }
        }

        // Update select button
        if (selectBlockButton != null)
        {
            bool canAfford = true;
            if (ResourceManager.Instance != null)
            {
                canAfford = blockData.CanAfford(
                    ResourceManager.Instance.GetWood(),
                    ResourceManager.Instance.GetStone(),
                    ResourceManager.Instance.GetClay()
                );
            }

            selectBlockButton.interactable = canAfford && blockData.isUnlocked;
        }
    }

    void UpdateButtonHighlights()
    {
        for (int i = 0; i < blockButtons.Count; i++)
        {
            blockButtons[i].SetSelected(i == selectedBlockIndex);
        }
    }

    void SelectCurrentBlock()
    {
        BuildingBlockData selectedBlock = blockDatabase.GetBlockData(selectedBlockIndex);
        if (selectedBlock == null) return;

        // Check if we can afford it
        if (ResourceManager.Instance != null)
        {
            bool canAfford = selectedBlock.CanAfford(
                ResourceManager.Instance.GetWood(),
                ResourceManager.Instance.GetStone(),
                ResourceManager.Instance.GetClay()
            );

            if (!canAfford)
            {
                Debug.Log("Not enough resources for " + selectedBlock.blockName);
                return;
            }
        }

        // Set the building system to use this block
        if (buildingSystem != null)
        {
            buildingSystem.SetSelectedBlock(selectedBlockIndex);
        }

        PlaySound(blockSelectSound);
        CloseMenu();

        Debug.Log($"Selected block: {selectedBlock.blockName}");
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

    public bool IsMenuOpen() => isMenuOpen;
    public int GetSelectedBlockIndex() => selectedBlockIndex;
}