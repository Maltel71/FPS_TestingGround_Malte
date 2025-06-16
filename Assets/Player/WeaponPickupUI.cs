using UnityEngine;
using UnityEngine.UI;

public class WeaponPickupUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas pickupCanvas;
    public Text pickupText;
    public Image weaponIcon;

    [Header("Settings")]
    public float fadeSpeed = 5f;
    public Vector3 worldOffset = Vector3.up * 1.5f;

    private Camera playerCamera;
    private CanvasGroup canvasGroup;
    private bool shouldShow = false;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        // Get or add canvas group for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start hidden
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerCamera != null)
        {
            // Make UI face the camera
            transform.LookAt(transform.position + playerCamera.transform.rotation * Vector3.forward,
                           playerCamera.transform.rotation * Vector3.up);
        }

        // Handle fading
        float targetAlpha = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // Deactivate when fully faded out
        if (canvasGroup.alpha <= 0.01f && !shouldShow)
        {
            gameObject.SetActive(false);
        }
    }

    public void ShowPickupPrompt(string weaponName, Sprite icon = null)
    {
        shouldShow = true;
        gameObject.SetActive(true);

        if (pickupText != null)
        {
            pickupText.text = $"Press E to pickup {weaponName}";
        }

        if (weaponIcon != null && icon != null)
        {
            weaponIcon.sprite = icon;
            weaponIcon.gameObject.SetActive(true);
        }
        else if (weaponIcon != null)
        {
            weaponIcon.gameObject.SetActive(false);
        }
    }

    public void HidePickupPrompt()
    {
        shouldShow = false;
    }

    // Method to update world position
    public void UpdateWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition + worldOffset;
    }
}