using UnityEngine;

[CreateAssetMenu(fileName = "BuildingBlockDatabase", menuName = "Building/Block Database")]
public class BuildingBlockDatabase : ScriptableObject
{
    [Header("Building Blocks")]
    public BuildingBlockData[] blocks;

    public BuildingBlockData GetBlockData(int index)
    {
        if (index >= 0 && index < blocks.Length)
            return blocks[index];
        return null;
    }

    public int GetBlockCount()
    {
        return blocks != null ? blocks.Length : 0;
    }
}

[System.Serializable]
public class BuildingBlockData
{
    [Header("Basic Info")]
    public string blockName = "Block";
    public GameObject blockPrefab;
    public Sprite blockIcon; // For UI display

    [Header("Resource Costs")]
    public int woodCost = 0;
    public int stoneCost = 0;
    public int clayCost = 0;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "A basic building block";

    [Header("Properties")]
    public bool isUnlocked = true; // For progression system
    public int durability = 100;

    public bool CanAfford(int wood, int stone, int clay)
    {
        return wood >= woodCost && stone >= stoneCost && clay >= clayCost;
    }

    public string GetCostString()
    {
        string cost = "";
        if (woodCost > 0) cost += $"Wood: {woodCost} ";
        if (stoneCost > 0) cost += $"Stone: {stoneCost} ";
        if (clayCost > 0) cost += $"Clay: {clayCost}";
        return string.IsNullOrEmpty(cost) ? "Free" : cost.Trim();
    }
}