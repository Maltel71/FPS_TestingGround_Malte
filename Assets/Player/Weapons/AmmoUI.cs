using UnityEngine;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image bulletAmountPanel;

    private WeaponShooting weaponShooting; // Remove from inspector - set programmatically

    private void Start()
    {
        // Auto-find components if not assigned
        if (bulletAmountPanel == null)
            bulletAmountPanel = GetComponentInChildren<Image>();

        // Don't auto-find WeaponShooting here - wait for SetWeaponShooting to be called

        // Initialize UI
        UpdateAmmoUI();
    }

    private void Update()
    {
        UpdateAmmoUI();
    }

    public void SetWeaponShooting(WeaponShooting shooting)
    {
        weaponShooting = shooting;
        Debug.Log($"AmmoUI: Set WeaponShooting to {(shooting != null ? shooting.name : "null")}");

        // Force immediate UI update
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (weaponShooting == null || bulletAmountPanel == null)
        {
            if (bulletAmountPanel != null)
                bulletAmountPanel.fillAmount = 0f; // Show empty if no weapon
            return;
        }

        // Calculate fill amount (0 to 1)
        float fillAmount = (float)weaponShooting.GetCurrentAmmo() / weaponShooting.GetMaxAmmo();
        bulletAmountPanel.fillAmount = fillAmount;

        // Debug info
        // Debug.Log($"AmmoUI Update: {weaponShooting.GetCurrentAmmo()}/{weaponShooting.GetMaxAmmo()} = {fillAmount}");
    }
}