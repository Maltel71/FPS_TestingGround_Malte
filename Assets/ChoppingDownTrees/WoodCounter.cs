using UnityEngine;
using TMPro; // Add this for TextMeshPro support

public class WoodCounter : MonoBehaviour
{
    public static WoodCounter Instance;

    [Header("UI - TextMeshPro")]
    public TextMeshProUGUI woodCountText; // Changed to TextMeshPro
    public Canvas uiCanvas; // Optional canvas reference

    [Header("UI - Legacy Text (Alternative)")]
    public UnityEngine.UI.Text legacyWoodCountText; // Fallback for legacy UI Text

    [Header("Auto-Find UI")]
    public bool autoFindUI = true; // Automatically find UI elements
    public string textObjectName = "WoodCountText"; // Name to search for

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
        // Auto-find UI elements if enabled and not assigned
        if (autoFindUI)
        {
            if (woodCountText == null)
            {
                // Try to find TextMeshPro text
                GameObject textObj = GameObject.Find(textObjectName);
                if (textObj != null)
                {
                    woodCountText = textObj.GetComponent<TextMeshProUGUI>();
                    if (woodCountText == null)
                    {
                        // If no TextMeshPro found, try legacy Text
                        legacyWoodCountText = textObj.GetComponent<UnityEngine.UI.Text>();
                    }
                }
            }

            // Auto-find canvas if not assigned
            if (uiCanvas == null)
            {
                uiCanvas = FindObjectOfType<Canvas>();
            }
        }

        UpdateUI();
    }

    public void AddWood(int amount)
    {
        woodCount += amount;
        UpdateUI();
        Debug.Log($"Added {amount} wood. Total: {woodCount}");
    }

    public bool RemoveWood(int amount)
    {
        if (woodCount >= amount)
        {
            woodCount -= amount;
            UpdateUI();
            Debug.Log($"Removed {amount} wood. Total: {woodCount}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough wood! Have {woodCount}, need {amount}");
            return false;
        }
    }

    public int GetWoodCount()
    {
        return woodCount;
    }

    public bool HasEnoughWood(int amount)
    {
        return woodCount >= amount;
    }

    void UpdateUI()
    {
        string displayText = "Wood: " + woodCount.ToString();

        // Update TextMeshPro text if available
        if (woodCountText != null)
        {
            woodCountText.text = displayText;
        }
        // Fallback to legacy text if TextMeshPro not available
        else if (legacyWoodCountText != null)
        {
            legacyWoodCountText.text = displayText;
        }
        // If no UI assigned, try to find it again
        else if (autoFindUI)
        {
            TryFindUIAgain();
        }
    }

    void TryFindUIAgain()
    {
        GameObject textObj = GameObject.Find(textObjectName);
        if (textObj != null)
        {
            woodCountText = textObj.GetComponent<TextMeshProUGUI>();
            if (woodCountText == null)
            {
                legacyWoodCountText = textObj.GetComponent<UnityEngine.UI.Text>();
            }

            if (woodCountText != null || legacyWoodCountText != null)
            {
                UpdateUI(); // Try updating UI again now that we found it
            }
        }
    }

    // Optional: Reset wood count
    public void ResetWoodCount()
    {
        woodCount = 0;
        UpdateUI();
        Debug.Log("Wood count reset to 0");
    }

    // Optional: Save/Load functionality
    public void SaveWoodCount()
    {
        PlayerPrefs.SetInt("WoodCount", woodCount);
        PlayerPrefs.Save();
    }

    public void LoadWoodCount()
    {
        woodCount = PlayerPrefs.GetInt("WoodCount", 0);
        UpdateUI();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveWoodCount();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveWoodCount();
    }

    void OnDestroy()
    {
        SaveWoodCount();
    }
}