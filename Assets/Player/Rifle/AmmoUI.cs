using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image bulletAmountPanel;
    [SerializeField] private WeaponShooting weaponShooting;

    private void Start()
    {
        // Auto-find components if not assigned
        if (bulletAmountPanel == null)
            bulletAmountPanel = GetComponentInChildren<Image>();

        if (weaponShooting == null)
            weaponShooting = FindObjectOfType<WeaponShooting>();

        // Initialize UI
        UpdateAmmoUI();
    }

    private void Update()
    {
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (weaponShooting != null && bulletAmountPanel != null)
        {
            // Calculate fill amount (0 to 1)
            float fillAmount = (float)weaponShooting.GetCurrentAmmo() / weaponShooting.GetMaxAmmo();
            bulletAmountPanel.fillAmount = fillAmount;
        }
    }
}