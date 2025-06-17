using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingBlockButton : MonoBehaviour
{
    [Header("UI Components")]
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

    private BuildingBlockData blockData;
    private int blockIndex;
    private BuildingBlocksMenu parentMenu;
    private bool isSelected = false;

    void Awake()
    {
        // Get components if not assigned
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
    }

    public void Setup(BuildingBlockData data, int index, BuildingBlocksMenu menu)
    {
        blockData = data;
        blockIndex = index;
        parentMenu = menu;

        // Setup UI
        if (blockIcon != null)
            blockIcon.sprite = data.blockIcon;

        if (blockNameText != null)
            blockNameText.text = data.blockName;

        UpdateCostDisplay();

        // Setup button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => parentMenu.OnBlockButtonClicked(blockIndex));
        }

        // Update visual state
        UpdateVisualState();
    }

    void Update()
    {
        // Regularly update affordability and visual state
        UpdateVisualState();
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
        UpdateVisualState();
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