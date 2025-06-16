using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapons")]
    public GameObject rifle;
    public GameObject axe;
    public GameObject hammer; // New hammer weapon

    [Header("Axe Settings")]
    public float axeRange = 3f;
    public LayerMask treeLayer = 1 << 8; // Set trees to layer 8

    [Header("References")]
    public BuildingSystem buildingSystem; // Reference to building system
    public FirstPersonController firstPersonController; // Reference to check if carrying

    public enum WeaponType
    {
        Rifle,
        Axe,
        Hammer
    }

    private WeaponType currentWeapon = WeaponType.Rifle;
    private Camera playerCamera;
    private bool weaponsEnabled = true;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        // Find building system if not assigned
        if (buildingSystem == null)
            buildingSystem = FindObjectOfType<BuildingSystem>();

        // Find first person controller if not assigned
        if (firstPersonController == null)
            firstPersonController = FindObjectOfType<FirstPersonController>();

        // Start with rifle active
        SetWeapon(WeaponType.Rifle);
    }

    void Update()
    {
        // Only allow weapon switching when not in building mode AND weapons are enabled
        if (weaponsEnabled && buildingSystem != null && !buildingSystem.IsBuildingMode())
        {
            HandleWeaponSwitching();
        }

        // Handle weapon-specific actions only if weapons are enabled
        if (weaponsEnabled)
        {
            HandleWeaponActions();
        }
    }

    void HandleWeaponSwitching()
    {
        // Q key cycles between rifle and axe (original functionality)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentWeapon == WeaponType.Rifle)
                SetWeapon(WeaponType.Axe);
            else if (currentWeapon == WeaponType.Axe)
                SetWeapon(WeaponType.Rifle);
            // Don't cycle to hammer via Q - that's handled by building mode
        }

        // Number keys for direct weapon selection (optional)
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetWeapon(WeaponType.Rifle);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetWeapon(WeaponType.Axe);
        // Alpha3 could be hammer if you want direct access outside building mode
    }

    void HandleWeaponActions()
    {
        // Only handle weapon actions if left mouse is pressed
        if (Input.GetMouseButtonDown(0))
        {
            switch (currentWeapon)
            {
                case WeaponType.Axe:
                    ChopTree();
                    break;
                case WeaponType.Hammer:
                    // Hammer actions are handled by BuildingSystem
                    // Could add hammer-specific effects here if needed
                    break;
                    // Rifle shooting is handled by WeaponShooting component
            }
        }
    }

    public void SetWeapon(WeaponType weaponType)
    {
        // Don't allow weapon switching if weapons are disabled
        if (!weaponsEnabled)
            return;

        currentWeapon = weaponType;

        // Deactivate all weapons first
        rifle.SetActive(false);
        axe.SetActive(false);
        hammer.SetActive(false);

        // Activate the selected weapon
        switch (weaponType)
        {
            case WeaponType.Rifle:
                rifle.SetActive(true);
                break;
            case WeaponType.Axe:
                axe.SetActive(true);
                break;
            case WeaponType.Hammer:
                hammer.SetActive(true);
                break;
        }

        Debug.Log($"Switched to {weaponType}");
    }

    // New method to enable/disable all weapons
    public void SetWeaponsEnabled(bool enabled)
    {
        weaponsEnabled = enabled;

        if (!enabled)
        {
            // Disable all weapons when carrying
            rifle.SetActive(false);
            axe.SetActive(false);
            hammer.SetActive(false);
            Debug.Log("Weapons disabled - carrying object");
        }
        else
        {
            // Re-enable the current weapon when not carrying
            SetWeapon(currentWeapon);
            Debug.Log("Weapons enabled");
        }
    }

    // Called by BuildingSystem when entering/exiting build mode
    public void OnBuildingModeChanged(bool isBuildingMode)
    {
        if (isBuildingMode)
        {
            // Switch to hammer when entering build mode
            SetWeapon(WeaponType.Hammer);
        }
        else
        {
            // Return to rifle when exiting build mode
            SetWeapon(WeaponType.Rifle);
        }
    }

    void ChopTree()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, axeRange, treeLayer))
        {
            // First check the hit object itself
            Tree tree = hit.collider.GetComponent<Tree>();
            TreeLog treeLog = hit.collider.GetComponent<TreeLog>();

            // If not found, check the parent object
            if (tree == null && treeLog == null && hit.collider.transform.parent != null)
            {
                tree = hit.collider.transform.parent.GetComponent<Tree>();
                treeLog = hit.collider.transform.parent.GetComponent<TreeLog>();
            }

            // If still not found, check if the hit object is a parent with Tree/TreeLog in children
            if (tree == null && treeLog == null)
            {
                tree = hit.collider.GetComponentInParent<Tree>();
                treeLog = hit.collider.GetComponentInParent<TreeLog>();
            }

            if (tree != null)
            {
                tree.TakeDamage(1);
                Debug.Log("Hit tree!");
            }
            else if (treeLog != null)
            {
                treeLog.TakeDamage(1);
                Debug.Log("Hit log!");
            }
        }
    }

    // Getters for other systems
    public WeaponType GetCurrentWeapon() => currentWeapon;
    public bool IsAxeActive() => currentWeapon == WeaponType.Axe;
    public bool IsHammerActive() => currentWeapon == WeaponType.Hammer;
    public bool IsRifleActive() => currentWeapon == WeaponType.Rifle;

    void OnDrawGizmosSelected()
    {
        if (currentWeapon == WeaponType.Axe && playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 rayStart = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;
            Gizmos.DrawRay(rayStart, rayDirection * axeRange);
        }
    }
}