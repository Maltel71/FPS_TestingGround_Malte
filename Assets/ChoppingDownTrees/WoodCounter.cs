using UnityEngine;
using TMPro;

public class WoodCounter : MonoBehaviour
{
    public static WoodCounter Instance;

    [Header("UI")]
    public TextMeshProUGUI woodCountText;

    private int woodCount = 0;

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
        UpdateUI();
    }

    public bool AddWood(int amount)
    {
        woodCount += amount;
        UpdateUI();

        // Sync with ResourceManager
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddWood(amount);
        }

        return true;
    }

    public bool RemoveWood(int amount)
    {
        if (woodCount >= amount)
        {
            woodCount -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public int GetWoodCount() => woodCount;

    void UpdateUI()
    {
        if (woodCountText != null)
            woodCountText.text = "Wood: " + woodCount.ToString();
    }
}