using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("Building Settings")]
    public GameObject[] blockPrefabs = new GameObject[4];
    public Material ghostMaterial;
    public Material invalidGhostMaterial;
    public LayerMask buildingLayer = -1;
    public float maxBuildDistance = 10f;
    public float playerCollisionRadius = 1f;

    [Header("References")]
    public FirstPersonController fpsController;
    public WeaponShooting weaponShooting;

    private bool buildingMode = false;
    private int currentBlockIndex = 0;
    private GameObject ghostObject;
    private Camera playerCamera;
    private bool wasMouseLocked;
    private MeshRenderer ghostRenderer;
    private bool canPlaceBlock = true;

    void Start()
    {
        // Find components if not assigned
        if (fpsController == null)
            fpsController = FindObjectOfType<FirstPersonController>();

        if (weaponShooting == null)
            weaponShooting = FindObjectOfType<WeaponShooting>();

        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        CreateGhostObject();

        // Store initial cursor state
        wasMouseLocked = Cursor.lockState == CursorLockMode.Locked;
    }

    void Update()
    {
        HandleInput();

        if (buildingMode)
        {
            UpdateGhostPosition();
            HandleBlockSelection();
            HandleBlockPlacement();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleBuildingMode();
        }
    }

    void ToggleBuildingMode()
    {
        buildingMode = !buildingMode;
        ghostObject.SetActive(buildingMode);

        // Disable/enable other systems
        if (weaponShooting != null)
            weaponShooting.enabled = !buildingMode;

        // Don't change cursor lock state - let FPS controller handle it
        // The FPS controller already manages cursor locking
    }

    void UpdateGhostPosition()
    {
        // Use screen center raycast like your weapon system
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildingLayer))
        {
            // Place block on the surface hit by raycast
            Vector3 targetPos = hit.point + hit.normal * 0.5f;
            Vector3 gridPos = new Vector3(
                Mathf.Round(targetPos.x),
                Mathf.Round(targetPos.y),
                Mathf.Round(targetPos.z)
            );

            ghostObject.transform.position = gridPos;
            ghostObject.SetActive(true);

            // Check if position is valid for placement
            canPlaceBlock = IsValidPlacementPosition(gridPos);
            UpdateGhostMaterial();
        }
        else
        {
            ghostObject.SetActive(false);
            canPlaceBlock = false;
        }
    }

    void HandleBlockSelection()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
                currentBlockIndex = (currentBlockIndex + 1) % blockPrefabs.Length;
            else
                currentBlockIndex = (currentBlockIndex - 1 + blockPrefabs.Length) % blockPrefabs.Length;

            UpdateGhostMesh();
        }
    }

    void HandleBlockPlacement()
    {
        // Only place blocks when in building mode, ghost is visible, and position is valid
        if (Input.GetMouseButtonDown(0) && ghostObject.activeInHierarchy && canPlaceBlock)
        {
            PlaceBlock();
        }

        // Optional: Right-click to remove blocks
        if (Input.GetMouseButtonDown(1))
        {
            RemoveBlock();
        }
    }

    bool IsValidPlacementPosition(Vector3 position)
    {
        // Check if position is already occupied by another block
        Collider[] overlapping = Physics.OverlapBox(position, Vector3.one * 0.4f, Quaternion.identity);
        foreach (Collider col in overlapping)
        {
            if (col.CompareTag("Block"))
                return false;
        }

        // Check if position would collide with player
        float distanceToPlayer = Vector3.Distance(position, transform.position);
        if (distanceToPlayer < playerCollisionRadius)
            return false;

        return true;
    }

    void UpdateGhostMaterial()
    {
        if (ghostRenderer != null)
        {
            ghostRenderer.material = canPlaceBlock ? ghostMaterial : invalidGhostMaterial;
        }
    }
    void PlaceBlock()
    {
        Vector3 placePos = ghostObject.transform.position;

        // Double-check validity before placing
        if (!IsValidPlacementPosition(placePos))
            return;

        GameObject newBlock = Instantiate(blockPrefabs[currentBlockIndex], placePos, Quaternion.identity);
        newBlock.tag = "Block";

        // Add to building layer if it's not already there
        newBlock.layer = LayerMaskToLayer(buildingLayer);
    }

    void RemoveBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance))
        {
            if (hit.collider.CompareTag("Block"))
            {
                Destroy(hit.collider.gameObject);
            }
        }
    }

    int LayerMaskToLayer(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 0)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }

    void CreateGhostObject()
    {
        ghostObject = new GameObject("GhostBlock");
        ghostObject.SetActive(false);

        UpdateGhostMesh();
    }

    void UpdateGhostMesh()
    {
        // Clear existing components
        MeshRenderer renderer = ghostObject.GetComponent<MeshRenderer>();
        MeshFilter filter = ghostObject.GetComponent<MeshFilter>();

        if (renderer == null) renderer = ghostObject.AddComponent<MeshRenderer>();
        if (filter == null) filter = ghostObject.AddComponent<MeshFilter>();

        // Store reference to renderer for material updates
        ghostRenderer = renderer;

        // Copy mesh from current block prefab
        if (blockPrefabs[currentBlockIndex] != null)
        {
            MeshFilter prefabFilter = blockPrefabs[currentBlockIndex].GetComponent<MeshFilter>();
            if (prefabFilter != null)
            {
                filter.mesh = prefabFilter.sharedMesh;
            }
        }

        // Set initial ghost material
        renderer.material = canPlaceBlock ? ghostMaterial : invalidGhostMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}