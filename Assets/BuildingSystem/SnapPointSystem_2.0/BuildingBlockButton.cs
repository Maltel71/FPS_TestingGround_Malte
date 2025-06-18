using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuildingBlockButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Manual Setup - Choose Your Block!")]
    public int blockIndex = 0; // Set this in inspector to choose which block this button represents

    [Header("UI Components (Auto-found if not assigned)")]
    public Image blockIcon;
    public TextMeshProUGUI blockNameText;
    public TextMeshProUGUI costText;
    public Button button;
    public GameObject selectedHighlight;
    public GameObject affordableIndicator;

    [Header("Colors")]
    public Color affordableColor = Color.green;
    public Color unaffordableColor = Color.red;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Hover Effects")]
    public float hoverScaleAmount = 1.1f;
    public float scaleAnimationSpeed = 8f;
    public AudioClip hoverSound;

    [Header("Debug")]
    public bool enableDebugLogs = true; // Toggle for debug logging

    private BuildingBlockData blockData;
    private BuildingBlocksMenu parentMenu;
    private bool isSelected = false;
    private bool isHovering = false;
    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        // Auto-find components if not assigned
        if (button == null)
            button = GetComponent<Button>();

        if (blockIcon == null)
            blockIcon = GetComponentInChildren<Image>();

        if (blockNameText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) blockNameText = texts[0];
            if (texts.Length > 1) costText = texts[1];
        }

        // Store original scale for hover effects
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (enableDebugLogs)
            Debug.Log($"BuildingBlockButton Awake - BlockIndex: {blockIndex}, Button found: {button != null}");
    }

    // Called by BuildingBlocksMenu to setup this button
    public void SetupManual(BuildingBlockData data, int index, BuildingBlocksMenu menu)
    {
        if (enableDebugLogs)
            Debug.Log($"=== SETTING UP BUTTON === Index: {index}, Data: {data?.blockName}, Menu: {menu != null}");

        blockData = data;
        blockIndex = index; // This can be overridden by inspector value
        parentMenu = menu;

        // Setup UI with block data
        UpdateUI();

        // Setup button click - CLEAR EXISTING LISTENERS FIRST
        if (button != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Setting up button click for {blockData?.blockName}. Button interactable: {button.interactable}");

            button.onClick.RemoveAllListeners();

            // Add the click listener
            button.onClick.AddListener(OnButtonClicked);

            // Verify the listener was added
            int listenerCount = button.onClick.GetPersistentEventCount();
            if (enableDebugLogs)
                Debug.Log($"Button listener added. Persistent event count: {listenerCount}");

            if (enableDebugLogs)
                Debug.Log($"Button click listener added for block index {blockIndex}");
        }
        else
        {
            Debug.LogError("Button component is null during setup!");
        }

        // Update visual state
        UpdateVisualState();
    }

    void Start()
    {
        // If no parent menu assigned, try to find it
        if (parentMenu == null)
            parentMenu = FindObjectOfType<BuildingBlocksMenu>();

        if (enableDebugLogs)
            Debug.Log($"BuildingBlockButton Start - ParentMenu found: {parentMenu != null}");
    }

    void Update()
    {
        // Regularly update affordability and visual state
        UpdateVisualState();

        // Animate scale for hover effect
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleAnimationSpeed * Time.deltaTime);

        // Handle click detection manually since Unity Button isn't working
        if (isHovering && Input.GetMouseButtonDown(0))
        {
            if (enableDebugLogs)
                Debug.Log($"=== RAW MOUSE CLICK DETECTED === Over button {blockIndex} while hovering");

            // Call our button click method directly
            OnButtonClicked();
        }
    }

    void UpdateUI()
    {
        if (blockData == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("Block data is null in UpdateUI");
            return;
        }

        // Update icon
        if (blockIcon != null)
            blockIcon.sprite = blockData.blockIcon;

        // Update name
        if (blockNameText != null)
            blockNameText.text = blockData.blockName;

        // Update cost
        UpdateCostDisplay();

        if (enableDebugLogs)
            Debug.Log($"UI Updated for {blockData.blockName}");
    }

    void UpdateCostDisplay()
    {
        if (costText != null && blockData != null)
        {
            costText.text = blockData.GetCostString();
        }
    }

    void UpdateVisualState()
    {
        if (blockData == null) return;

        bool canAfford = true;

        // Check affordability
        if (ResourceManager.Instance != null)
        {
            canAfford = blockData.CanAfford(
                ResourceManager.Instance.GetWood(),
                ResourceManager.Instance.GetStone(),
                ResourceManager.Instance.GetClay()
            );
        }

        // Update button interactability
        if (button != null)
        {
            button.interactable = canAfford && blockData.isUnlocked;
        }

        // Update cost text color
        if (costText != null)
        {
            costText.color = canAfford ? affordableColor : unaffordableColor;
        }

        // Update affordability indicator
        if (affordableIndicator != null)
        {
            affordableIndicator.SetActive(canAfford);
        }

        // Update selection highlight
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(isSelected);
        }

        // Update overall color based on state
        Color targetColor = normalColor;
        if (isSelected)
            targetColor = selectedColor;
        else if (!canAfford)
            targetColor = unaffordableColor;
        else
            targetColor = affordableColor;

        // Apply color to main image/background
        Image backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, Time.deltaTime * 5f);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateHoverScale(); // Update scale when selection changes
        UpdateVisualState();

        if (enableDebugLogs)
            Debug.Log($"Button {blockIndex} selected: {selected}");
    }

    // Hover effect methods
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        UpdateHoverScale();

        // Play hover sound
        if (hoverSound != null && parentMenu != null && parentMenu.audioSource != null)
        {
            parentMenu.audioSource.PlayOneShot(hoverSound);
        }

        if (enableDebugLogs)
            Debug.Log($"Hovering over button {blockIndex}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        UpdateHoverScale();
    }

    // Method called by Unity Button onClick
    public void OnButtonClicked()
    {
        if (enableDebugLogs)
            Debug.Log($"=== BUTTON CLICKED === Block: {blockData?.blockName}, Index: {blockIndex}");

        if (parentMenu != null)
        {
            parentMenu.OnBlockButtonClicked(blockIndex);
        }
        else
        {
            Debug.LogError("Parent menu is null when button clicked!");
        }
    }

    void UpdateHoverScale()
    {
        if (isHovering)
        {
            targetScale = originalScale * hoverScaleAmount;
        }
        else
        {
            targetScale = originalScale;
        }
    }

    // Manual test method for debugging
    [ContextMenu("Test Button Click")]
    public void TestButtonClick()
    {
        Debug.Log($"=== MANUAL TEST CLICK === Block: {blockData?.blockName}, Index: {blockIndex}");
        if (parentMenu != null)
            parentMenu.OnBlockButtonClicked(blockIndex);
        else
            Debug.LogError("Parent menu is null!");
    }

    public BuildingBlockData GetBlockData() => blockData;
    public int GetBlockIndex() => blockIndex;
    public bool IsAffordable()
    {
        if (ResourceManager.Instance == null || blockData == null) return false;

        return blockData.CanAfford(
            ResourceManager.Instance.GetWood(),
            ResourceManager.Instance.GetStone(),
            ResourceManager.Instance.GetClay()
        );
    }
}