using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSlot
    {
        public GameObject weaponPrefab;
        public string weaponName;

        public bool HasWeapon() => weaponPrefab != null;

        public void SetWeapon(GameObject weapon, string name)
        {
            weaponPrefab = weapon;
            weaponName = name;
        }

        public void ClearWeapon()
        {
            if (weaponPrefab != null) weaponPrefab.SetActive(false);
            weaponPrefab = null;
            weaponName = "";
        }
    }

    [Header("Weapon Slots")]
    [SerializeField] private WeaponSlot primaryWeapon = new WeaponSlot();
    [SerializeField] private WeaponSlot secondaryWeapon = new WeaponSlot();
    [SerializeField] private WeaponSlot meleeWeapon = new WeaponSlot();
    [SerializeField] private WeaponSlot buildingHammer = new WeaponSlot();

    [Header("Building Hammer")]
    [SerializeField] private GameObject defaultHammerPrefab;
    [SerializeField] private string defaultHammerName = "Building Hammer";

    [Header("Settings")]
    public float axeRange = 3f;
    public LayerMask treeLayer = 1 << 8;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask weaponPickupLayer = -1;

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

        InitializeBuildingHammer();
        DeactivateAllWeapons();
        SetInitialWeapon();
    }

    void InitializeBuildingHammer()
    {
        if (defaultHammerPrefab != null)
        {
            GameObject hammer = Instantiate(defaultHammerPrefab, playerCamera.transform);
            hammer.transform.localPosition = Vector3.zero;
            hammer.transform.localRotation = Quaternion.identity;
            hammer.SetActive(false);
            hammer.name = defaultHammerName;
            buildingHammer.SetWeapon(hammer, defaultHammerName);
        }
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

        WeaponType slotType = GetWeaponSlotType(weaponPickup.weaponCategory);
        if (slotType == WeaponType.BuildingHammer) return;

        WeaponSlot targetSlot = GetWeaponSlot(slotType);
        if (targetSlot.HasWeapon()) DropWeapon(slotType, weaponPickup.transform.position);

        GameObject newWeapon = Instantiate(weaponPickup.weaponPrefab, playerCamera.transform);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        newWeapon.SetActive(false);
        newWeapon.name = weaponPickup.weaponName;

        targetSlot.SetWeapon(newWeapon, weaponPickup.weaponName);

        if (weaponSwitchAudioSource && weaponPickupSound)
            weaponSwitchAudioSource.PlayOneShot(weaponPickupSound);

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
        if (!slot.HasWeapon()) return;

        GameObject pickup = new GameObject($"{slot.weaponName}_Pickup");
        pickup.transform.position = dropPosition + Vector3.up * 0.5f;

        WeaponPickup pickupComponent = pickup.AddComponent<WeaponPickup>();
        pickupComponent.weaponPrefab = slot.weaponPrefab;
        pickupComponent.weaponName = slot.weaponName;
        pickupComponent.usePhysics = true;

        BoxCollider col = pickup.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 0.5f;

        Rigidbody rb = pickup.AddComponent<Rigidbody>();
        rb.AddForce(new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), Random.Range(-2f, 2f)), ForceMode.Impulse);

        pickupComponent.CreateVisualRepresentation();
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

    // Debug methods
    [ContextMenu("Switch to Primary")] public void DebugSwitchToPrimary() => SwitchToSlot(WeaponType.Primary);
    [ContextMenu("Switch to Melee")] public void DebugSwitchToMelee() => SwitchToSlot(WeaponType.Melee);
    [ContextMenu("Cycle Weapons")] public void DebugCycleWeapons() => CycleWeapons();
}