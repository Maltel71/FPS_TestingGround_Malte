using UnityEngine;

public class WoodCounter : MonoBehaviour
{
    public static WoodCounter Instance;

    [Header("UI")]
    public UnityEngine.UI.Text woodCountText;

    private int woodCount = 0;

    void Awake()
    {
        // Singleton pattern to access from anywhere
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

    public void AddWood(int amount)
    {
        woodCount += amount;
        UpdateUI();
        Debug.Log($"Added {amount} wood. Total: {woodCount}");
    }

    public int GetWoodCount()
    {
        return woodCount;
    }

    void UpdateUI()
    {
        if (woodCountText != null)
        {
            woodCountText.text = "Wood: " + woodCount.ToString();
        }
    }
}