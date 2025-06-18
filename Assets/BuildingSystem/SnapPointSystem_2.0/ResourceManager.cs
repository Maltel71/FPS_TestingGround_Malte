using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("Resources")]
    public int wood = 0;
    public int stone = 0;
    public int clay = 0;

    [Header("UI")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI clayText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Sync with WoodCounter after a frame delay
        Invoke("SyncWithWoodCounter", 0.1f);
        UpdateUI();
    }

    void SyncWithWoodCounter()
    {
        if (WoodCounter.Instance != null)
        {
            wood = WoodCounter.Instance.GetWoodCount();
            UpdateUI();
        }
    }

    public bool SpendResources(int woodCost, int stoneCost, int clayCost)
    {
        if (wood >= woodCost && stone >= stoneCost && clay >= clayCost)
        {
            wood -= woodCost;
            stone -= stoneCost;
            clay -= clayCost;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void AddWood(int amount) { wood += amount; UpdateUI(); }
    public void AddStone(int amount) { stone += amount; UpdateUI(); }
    public void AddClay(int amount) { clay += amount; UpdateUI(); }

    public int GetWood() => wood;
    public int GetStone() => stone;
    public int GetClay() => clay;

    void UpdateUI()
    {
        if (woodText != null) woodText.text = wood.ToString();
        if (stoneText != null) stoneText.text = stone.ToString();
        if (clayText != null) clayText.text = clay.ToString();
    }
}