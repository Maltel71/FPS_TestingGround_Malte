using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("Resources")]
    public int wood = 0;
    public int stone = 0;
    public int clay = 0;

    [Header("UI - TextMeshPro")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI clayText;

    [Header("UI - Legacy Text (Alternative)")]
    public UnityEngine.UI.Text legacyWoodText;
    public UnityEngine.UI.Text legacyStoneText;
    public UnityEngine.UI.Text legacyClayText;

    [Header("Auto-Find UI")]
    public bool autoFindUI = true;
    public string woodTextName = "WoodText";
    public string stoneTextName = "StoneText";
    public string clayTextName = "ClayText";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip resourceGainSound;
    public AudioClip resourceSpendSound;
    public AudioClip insufficientResourcesSound;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Setup audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Auto-find UI elements if enabled
        if (autoFindUI)
        {
            AutoFindUIElements();
        }

        // Use existing wood count from WoodCounter if it exists
        if (WoodCounter.Instance != null)
        {
            wood = WoodCounter.Instance.GetWoodCount();
        }

        UpdateAllUI();
    }

    void AutoFindUIElements()
    {
        // Find wood text
        if (woodText == null)
        {
            GameObject woodObj = GameObject.Find(woodTextName);
            if (woodObj != null)
            {
                woodText = woodObj.GetComponent<TextMeshProUGUI>();
                if (woodText == null)
                    legacyWoodText = woodObj.GetComponent<UnityEngine.UI.Text>();
            }
        }

        // Find stone text
        if (stoneText == null)
        {
            GameObject stoneObj = GameObject.Find(stoneTextName);
            if (stoneObj != null)
            {
                stoneText = stoneObj.GetComponent<TextMeshProUGUI>();
                if (stoneText == null)
                    legacyStoneText = stoneObj.GetComponent<UnityEngine.UI.Text>();
            }
        }

        // Find clay text
        if (clayText == null)
        {
            GameObject clayObj = GameObject.Find(clayTextName);
            if (clayObj != null)
            {
                clayText = clayObj.GetComponent<TextMeshProUGUI>();
                if (clayText == null)
                    legacyClayText = clayObj.GetComponent<UnityEngine.UI.Text>();
            }
        }
    }

    // Add resources
    public void AddWood(int amount)
    {
        wood += amount;
        UpdateWoodUI();
        PlayResourceGainSound();

        // Sync with WoodCounter if it exists
        if (WoodCounter.Instance != null)
        {
            WoodCounter.Instance.AddWood(amount);
        }
    }

    public void AddStone(int amount)
    {
        stone += amount;
        UpdateStoneUI();
        PlayResourceGainSound();
    }

    public void AddClay(int amount)
    {
        clay += amount;
        UpdateClayUI();
        PlayResourceGainSound();
    }

    // Spend resources
    public bool SpendResources(int woodCost, int stoneCost, int clayCost)
    {
        if (CanAfford(woodCost, stoneCost, clayCost))
        {
            wood -= woodCost;
            stone -= stoneCost;
            clay -= clayCost;

            UpdateAllUI();
            PlayResourceSpendSound();

            // Sync with WoodCounter if it exists
            if (WoodCounter.Instance != null && woodCost > 0)
            {
                WoodCounter.Instance.RemoveWood(woodCost);
            }

            return true;
        }
        else
        {
            PlayInsufficientResourcesSound();
            return false;
        }
    }

    public bool CanAfford(int woodCost, int stoneCost, int clayCost)
    {
        return wood >= woodCost && stone >= stoneCost && clay >= clayCost;
    }

    // Getters
    public int GetWood() => wood;
    public int GetStone() => stone;
    public int GetClay() => clay;

    // UI Updates
    void UpdateAllUI()
    {
        UpdateWoodUI();
        UpdateStoneUI();
        UpdateClayUI();
    }

    void UpdateWoodUI()
    {
        string displayText = wood.ToString();

        if (woodText != null)
            woodText.text = displayText;
        else if (legacyWoodText != null)
            legacyWoodText.text = displayText;
    }

    void UpdateStoneUI()
    {
        string displayText = stone.ToString();

        if (stoneText != null)
            stoneText.text = displayText;
        else if (legacyStoneText != null)
            legacyStoneText.text = displayText;
    }

    void UpdateClayUI()
    {
        string displayText = clay.ToString();

        if (clayText != null)
            clayText.text = displayText;
        else if (legacyClayText != null)
            legacyClayText.text = displayText;
    }

    // Audio
    void PlayResourceGainSound()
    {
        if (audioSource != null && resourceGainSound != null)
            audioSource.PlayOneShot(resourceGainSound);
    }

    void PlayResourceSpendSound()
    {
        if (audioSource != null && resourceSpendSound != null)
            audioSource.PlayOneShot(resourceSpendSound);
    }

    void PlayInsufficientResourcesSound()
    {
        if (audioSource != null && insufficientResourcesSound != null)
            audioSource.PlayOneShot(insufficientResourcesSound);
    }

    // Save/Load
    public void SaveResources()
    {
        PlayerPrefs.SetInt("Wood", wood);
        PlayerPrefs.SetInt("Stone", stone);
        PlayerPrefs.SetInt("Clay", clay);
        PlayerPrefs.Save();
    }

    public void LoadResources()
    {
        wood = PlayerPrefs.GetInt("Wood", 0);
        stone = PlayerPrefs.GetInt("Stone", 0);
        clay = PlayerPrefs.GetInt("Clay", 0);
        UpdateAllUI();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveResources();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveResources();
    }

    void OnDestroy()
    {
        SaveResources();
    }
}