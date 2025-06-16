using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [System.Serializable]
    public class WeaponData
    {
        [Header("Weapon Identity")]
        public string weaponName;
        public WeaponCategory weaponCategory;

        [Header("Prefabs")]
        public GameObject equipPrefab;    // The weapon the player holds/uses
        public GameObject pickupPrefab;   // The pickup version when dropped/unequipped

        [Header("Optional Settings")]
        public Sprite weaponIcon;         // For UI
        public AudioClip pickupSound;     // Custom pickup sound for this weapon

        public bool IsValid() => equipPrefab != null && pickupPrefab != null && !string.IsNullOrEmpty(weaponName);
    }

    [System.Serializable]
    public class WeaponSlot
    {
        public GameObject weaponPrefab;
        public string weaponName;
        public WeaponData weaponData; // Reference to the weapon data

        public bool HasWeapon() => weaponPrefab != null;

        public void SetWeapon(GameObject weapon, WeaponData data)
        {
            weaponPrefab = weapon;
            weaponName = data.weaponName;
            weaponData = data;
        }

        public void ClearWeapon()
        {
            if (weaponPrefab != null) weaponPrefab.SetActive(false);
            weaponPrefab = null;
            weaponName = "";
            weaponData = null;
        }
    }

    [Header("Weapon Database")]
    [SerializeField] private WeaponData[] weaponDatabase;

    [Header("Weapon Slots")]
    [SerializeField] private WeaponSlot primaryWeapon = new WeaponSlot();
    [SerializeField] private WeaponSlot secondaryWeapon = new WeaponSlot();
    [SerializeField] private WeaponSlot meleeWeapon = new WeaponSlot();
    [SerializeField] private WeaponSlot buildingHammer = new WeaponSlot();

    [Header("Building Hammer")]
    [SerializeField] private WeaponData defaultHammerData;

    [Header("Input Settings")]
    [SerializeField] private KeyCode unequipKey = KeyCode.H;

    [Header("Settings")]
    public float axeRange = 3f;
    public LayerMask treeLayer = 1 << 8;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask weaponPickupLayer = -1;
    [SerializeField] private float unequipThrowForce = 5f;

    [Header("References")]
    public BuildingSystem buildingSystem;
    public FirstPersonController firstPersonController;

    [Header("Audio")]
    [SerializeField] private AudioSource weaponSwitchAudioSource;
    [SerializeField] private AudioClip weaponSwitchSound;
    [SerializeField] private AudioClip weaponPickupSound;

    public enum WeaponType { Primary, Secondary, Melee, BuildingHammer }
    public enum WeaponCategory { AssaultRifle, SniperRifle, Pistol, FlareGun, Axe, Knife, Machete, Hammer }

    private WeaponType currentWeaponType = WeaponType.Primary;
    private Camera playerCamera;
    private bool weaponsEnabled = true;
    private WeaponPickup nearbyWeapon;
    private float lastInputTime = 0f;
    private const float INPUT_COOLDOWN = 0.1f;

    void Start()
    {
        playerCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (buildingSystem == null) buildingSystem = FindObjectOfType<BuildingSystem>();
        if (firstPersonController == null) firstPersonController = FindObjectOfType<FirstPersonController>();

        ValidateWeaponDatabase();
        InitializeBuildingHammer();
        DeactivateAllWeapons();
        SetInitialWeapon();
    }

    void ValidateWeaponDatabase()
    {
        foreach (var weapon in weaponDatabase)
        {
            if (!weapon.IsValid())
            {
                Debug.LogWarning($"Weapon '{weapon.weaponName}' has missing prefab references!");
            }
        }
    }

    void InitializeBuildingHammer()
    {
        if (defaultHammerData != null && defaultHammerData.IsValid())
        {
            GameObject hammer = Instantiate(defaultHammerData.equipPrefab, playerCamera.transform);
            hammer.transform.localPosition = Vector3.zero;
            hammer.transform.localRotation = Quaternion.identity;
            hammer.SetActive(false);
            hammer.name = defaultHammerData.weaponName;
            buildingHammer.SetWeapon(hammer, defaultHammerData);
        }
    }

    // Find weapon data by name
    WeaponData FindWeaponData(string weaponName)
    {
        foreach (var weapon in weaponDatabase)
        {
            if (weapon.weaponName == weaponName)
                return weapon;
        }
        return null;
    }

    // Find weapon data by category
    WeaponData FindWeaponDataByCategory(WeaponCategory category)
    {
        foreach (var weapon in weaponDatabase)
        {
            if (weapon.weaponCategory == category)
                return weapon;
        }
        return null;
    }

    void Update()
    {
        CheckForNearbyWeapons();
        HandleWeaponPickup();
        HandleInput();
        HandleWeaponActions();
    }

    void HandleInput()
    {
        if (Time.time - lastInputTime < INPUT_COOLDOWN) return;

        if (Input.GetKeyDown(KeyCode.Q) && TryWeaponAction(() => CycleWeapons())) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) && TryWeaponAction(() => SwitchToSlot(WeaponType.Primary))) return;
        if (Input.GetKeyDown(KeyCode.Alpha2) && TryWeaponAction(() => SwitchToSlot(WeaponType.Secondary))) return;
        if (Input.GetKeyDown(KeyCode.Alpha3) && TryWeaponAction(() => SwitchToSlot(WeaponType.Melee))) return;
        if (Input.GetKeyDown(unequipKey) && TryWeaponAction(() => UnequipCurrentWeapon())) return;
    }

    bool TryWeaponAction(System.Action action)
    {
        if (!CanSwitchWeapons()) return false;
        action.Invoke();
        lastInputTime = Time.time;
        return true;
    }

    bool CanSwitchWeapons()
    {
        return weaponsEnabled && (buildingSystem == null || !buildingSystem.IsBuildingMode());
    }

    void SwitchToSlot(WeaponType weaponType)
    {
        WeaponSlot slot = GetWeaponSlot(weaponType);
        if (slot.HasWeapon())
            SetWeapon(weaponType);
        else
            SwitchToEmptySlot(weaponType);
    }

    void CycleWeapons()
    {
        WeaponType[] order = { WeaponType.Primary, WeaponType.Secondary, WeaponType.Melee };
        int currentIndex = System.Array.IndexOf(order, currentWeaponType);
        if (currentIndex == -1) currentIndex = -1;

        for (int i = 1; i <= order.Length; i++)
        {
            int nextIndex = (currentIndex + i) % order.Length;
            WeaponType nextWeapon = order[nextIndex];

            if (GetWeaponSlot(nextWeapon).HasWeapon())
            {
                SetWeapon(nextWeapon);
                return;
            }
        }
    }

    void UnequipCurrentWeapon()
    {
        // Cannot unequip building hammer
        if (currentWeaponType == WeaponType.BuildingHammer)
        {
            Debug.Log("Cannot unequip building hammer");
            return;
        }

        WeaponSlot currentSlot = GetWeaponSlot(currentWeaponType);
        if (!currentSlot.HasWeapon())
        {
            Debug.Log("No weapon to unequip");
            return;
        }

        // Calculate throw position in front of player
        Vector3 throwPosition = transform.position + playerCamera.transform.forward * 2f + Vector3.up;

        // Drop/throw the weapon using the proper pickup prefab
        ThrowWeapon(currentWeaponType, throwPosition);

        // Switch to an available weapon or go unarmed
        SwitchToNextAvailableWeapon();

        Debug.Log($"Unequipped and threw {currentSlot.weaponName}");
    }

    void ThrowWeapon(WeaponType weaponType, Vector3 throwPosition)
    {
        WeaponSlot slot = GetWeaponSlot(weaponType);
        if (!slot.HasWeapon() || slot.weaponData == null) return;

        // Instantiate the proper pickup prefab from weapon data
        GameObject pickup = Instantiate(slot.weaponData.pickupPrefab, throwPosition, Quaternion.identity);

        // Get or add WeaponPickup component
        WeaponPickup pickupComponent = pickup.GetComponent<WeaponPickup>();
        if (pickupComponent == null)
        {
            pickupComponent = pickup.AddComponent<WeaponPickup>();
        }

        // Setup the pickup component with weapon data
        pickupComponent.weaponPrefab = slot.weaponData.equipPrefab;
        pickupComponent.weaponName = slot.weaponData.weaponName;
        pickupComponent.weaponCategory = slot.weaponData.weaponCategory;

        // Setup physics if needed
        Rigidbody rb = pickup.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = pickup.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.angularDamping = 0.5f;
            rb.linearDamping = 0.1f;
        }

        // Add collider if needed
        Collider col = pickup.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = pickup.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = Vector3.one * 0.5f;
        }

        // Throw the weapon forward with some upward arc
        Vector3 throwDirection = playerCamera.transform.forward + Vector3.up * 0.3f;
        rb.AddForce(throwDirection.normalized * unequipThrowForce, ForceMode.Impulse);

        // Add random rotation for realistic effect
        Vector3 randomTorque = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);

        // Destroy the equipped weapon and clear slot
        Destroy(slot.weaponPrefab);
        slot.ClearWeapon();
    }

    void SwitchToNextAvailableWeapon()
    {
        // Try to switch to next available weapon in order of preference
        if (primaryWeapon.HasWeapon())
            SetWeapon(WeaponType.Primary);
        else if (secondaryWeapon.HasWeapon())
            SetWeapon(WeaponType.Secondary);
        else if (meleeWeapon.HasWeapon())
            SetWeapon(WeaponType.Melee);
        else
        {
            // No weapons available, go unarmed
            currentWeaponType = WeaponType.Primary; // Default to primary slot but empty
            DeactivateAllWeapons();
        }
    }

    void CheckForNearbyWeapons()
    {
        if (nearbyWeapon != null && Vector3.Distance(transform.position, nearbyWeapon.transform.position) > pickupRange)
        {
            if (nearbyWeapon.pickupUI != null) nearbyWeapon.pickupUI.SetActive(false);
            nearbyWeapon = null;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, weaponPickupLayer))
        {
            WeaponPickup pickup = hit.collider.GetComponent<WeaponPickup>();
            if (pickup != null && pickup != nearbyWeapon)
            {
                nearbyWeapon = pickup;
                if (pickup.pickupUI != null) pickup.pickupUI.SetActive(true);
            }
        }
    }

    void HandleWeaponPickup()
    {
        if (Input.GetKeyDown(KeyCode.E) && nearbyWeapon != null)
            PickupWeapon(nearbyWeapon);
    }

    public void PickupWeapon(WeaponPickup weaponPickup)
    {
        if (weaponPickup == null) return;

        // Find the weapon data for this pickup
        WeaponData weaponData = FindWeaponData(weaponPickup.weaponName);
        if (weaponData == null)
        {
            Debug.LogWarning($"No weapon data found for {weaponPickup.weaponName}");
            return;
        }

        WeaponType slotType = GetWeaponSlotType(weaponData.weaponCategory);
        if (slotType == WeaponType.BuildingHammer) return;

        WeaponSlot targetSlot = GetWeaponSlot(slotType);
        if (targetSlot.HasWeapon()) DropWeapon(slotType, weaponPickup.transform.position);

        // Instantiate the equip prefab from weapon data
        GameObject newWeapon = Instantiate(weaponData.equipPrefab, playerCamera.transform);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        newWeapon.SetActive(false);
        newWeapon.name = weaponData.weaponName;

        targetSlot.SetWeapon(newWeapon, weaponData);

        // Play pickup sound (use weapon-specific sound if available)
        AudioClip soundToPlay = weaponData.pickupSound != null ? weaponData.pickupSound : weaponPickupSound;
        if (weaponSwitchAudioSource && soundToPlay)
            weaponSwitchAudioSource.PlayOneShot(soundToPlay);

        if (currentWeaponType == slotType || !GetWeaponSlot(currentWeaponType).HasWeapon())
            SetWeapon(slotType);

        if (weaponPickup.pickupUI != null) weaponPickup.pickupUI.SetActive(false);
        Destroy(weaponPickup.gameObject);
        nearbyWeapon = null;
    }

    WeaponType GetWeaponSlotType(WeaponCategory category)
    {
        return category switch
        {
            WeaponCategory.AssaultRifle or WeaponCategory.SniperRifle => WeaponType.Primary,
            WeaponCategory.Pistol or WeaponCategory.FlareGun => WeaponType.Secondary,
            WeaponCategory.Axe or WeaponCategory.Knife or WeaponCategory.Machete => WeaponType.Melee,
            WeaponCategory.Hammer => WeaponType.BuildingHammer,
            _ => WeaponType.Primary
        };
    }

    WeaponSlot GetWeaponSlot(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Primary => primaryWeapon,
            WeaponType.Secondary => secondaryWeapon,
            WeaponType.Melee => meleeWeapon,
            WeaponType.BuildingHammer => buildingHammer,
            _ => primaryWeapon
        };
    }

    void SetInitialWeapon()
    {
        if (primaryWeapon.HasWeapon()) SetWeapon(WeaponType.Primary);
        else if (secondaryWeapon.HasWeapon()) SetWeapon(WeaponType.Secondary);
        else if (meleeWeapon.HasWeapon()) SetWeapon(WeaponType.Melee);
        else { currentWeaponType = WeaponType.Primary; DeactivateAllWeapons(); }
    }

    void SwitchToEmptySlot(WeaponType weaponType)
    {
        if (!weaponsEnabled) return;
        currentWeaponType = weaponType;
        DeactivateAllWeapons();
    }

    public void SetWeapon(WeaponType weaponType)
    {
        if (!weaponsEnabled) return;

        WeaponSlot targetSlot = GetWeaponSlot(weaponType);
        if (!targetSlot.HasWeapon()) { SwitchToEmptySlot(weaponType); return; }

        currentWeaponType = weaponType;
        DeactivateAllWeapons();

        if (targetSlot.weaponPrefab != null)
            targetSlot.weaponPrefab.SetActive(true);

        if (weaponSwitchAudioSource && weaponSwitchSound)
            weaponSwitchAudioSource.PlayOneShot(weaponSwitchSound);
    }

    void DeactivateAllWeapons()
    {
        if (primaryWeapon.weaponPrefab != null) primaryWeapon.weaponPrefab.SetActive(false);
        if (secondaryWeapon.weaponPrefab != null) secondaryWeapon.weaponPrefab.SetActive(false);
        if (meleeWeapon.weaponPrefab != null) meleeWeapon.weaponPrefab.SetActive(false);
        if (buildingHammer.weaponPrefab != null) buildingHammer.weaponPrefab.SetActive(false);
    }

    void HandleWeaponActions()
    {
        if (Input.GetMouseButtonDown(0) && weaponsEnabled && currentWeaponType == WeaponType.Melee)
        {
            WeaponSlot meleeSlot = GetWeaponSlot(WeaponType.Melee);
            if (meleeSlot.HasWeapon() && meleeSlot.weaponName.ToLower().Contains("axe"))
                ChopTree();
        }
    }

    void ChopTree()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, axeRange, treeLayer)) return;

        Tree tree = hit.collider.GetComponent<Tree>() ??
                   hit.collider.GetComponentInParent<Tree>();
        TreeLog treeLog = hit.collider.GetComponent<TreeLog>() ??
                         hit.collider.GetComponentInParent<TreeLog>();

        if (tree != null) tree.TakeDamage(1);
        else if (treeLog != null) treeLog.TakeDamage(1);
    }

    void DropWeapon(WeaponType weaponType, Vector3 dropPosition)
    {
        if (weaponType == WeaponType.BuildingHammer) return;

        WeaponSlot slot = GetWeaponSlot(weaponType);
        if (!slot.HasWeapon() || slot.weaponData == null) return;

        // Use the pickup prefab from weapon data
        GameObject pickup = Instantiate(slot.weaponData.pickupPrefab, dropPosition + Vector3.up * 0.5f, Quaternion.identity);

        WeaponPickup pickupComponent = pickup.GetComponent<WeaponPickup>();
        if (pickupComponent == null)
            pickupComponent = pickup.AddComponent<WeaponPickup>();

        pickupComponent.weaponPrefab = slot.weaponData.equipPrefab;
        pickupComponent.weaponName = slot.weaponData.weaponName;
        pickupComponent.weaponCategory = slot.weaponData.weaponCategory;

        Rigidbody rb = pickup.GetComponent<Rigidbody>();
        if (rb == null) rb = pickup.AddComponent<Rigidbody>();

        rb.AddForce(new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), Random.Range(-2f, 2f)), ForceMode.Impulse);

        Destroy(slot.weaponPrefab);
        slot.ClearWeapon();
    }

    public void SetWeaponsEnabled(bool enabled)
    {
        weaponsEnabled = enabled;
        if (!enabled) DeactivateAllWeapons();
        else if (GetWeaponSlot(currentWeaponType).HasWeapon()) SetWeapon(currentWeaponType);
    }

    public void OnBuildingModeChanged(bool isBuildingMode)
    {
        if (isBuildingMode)
        {
            if (buildingHammer.HasWeapon()) SetWeapon(WeaponType.BuildingHammer);
        }
        else
        {
            if (primaryWeapon.HasWeapon()) SetWeapon(WeaponType.Primary);
            else if (secondaryWeapon.HasWeapon()) SetWeapon(WeaponType.Secondary);
            else if (meleeWeapon.HasWeapon()) SetWeapon(WeaponType.Melee);
            else DeactivateAllWeapons();
        }
    }

    // Public getters
    public WeaponType GetCurrentWeaponType() => currentWeaponType;
    public bool HasWeapon(WeaponType weaponType) => GetWeaponSlot(weaponType).HasWeapon();
    public string GetCurrentWeaponName() => GetWeaponSlot(currentWeaponType).weaponName;
    public bool IsNearWeapon() => nearbyWeapon != null;
    public WeaponData[] GetWeaponDatabase() => weaponDatabase;

    // Public method to change unequip keybind at runtime
    public void SetUnequipKey(KeyCode newKey) => unequipKey = newKey;
    public KeyCode GetUnequipKey() => unequipKey;

    // Debug methods
    [ContextMenu("Switch to Primary")] public void DebugSwitchToPrimary() => SwitchToSlot(WeaponType.Primary);
    [ContextMenu("Switch to Melee")] public void DebugSwitchToMelee() => SwitchToSlot(WeaponType.Melee);
    [ContextMenu("Cycle Weapons")] public void DebugCycleWeapons() => CycleWeapons();
    [ContextMenu("Unequip Current Weapon")] public void DebugUnequipWeapon() => UnequipCurrentWeapon();
}