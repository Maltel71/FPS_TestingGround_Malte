using UnityEngine;
using System.Collections.Generic;

public class SnapBuildingSystem : MonoBehaviour
{
    [Header("Building Settings")]
    public BuildingBlockDatabase blockDatabase; // New: Use database instead of array
    public GameObject[] blockPrefabs = new GameObject[4]; // Keep for backwards compatibility
    public Material ghostMaterial;
    public Material invalidGhostMaterial;
    public LayerMask buildableLayer = -1;
    public LayerMask blockLayer = -1;
    public float maxBuildDistance = 10f;
    public float snapDistance = 2f;
    public float playerCollisionRadius = 1f;

    [Header("Snap Settings")]
    public bool enableSnapToGround = true;
    public float groundSnapDistance = 0.5f;
    public float snapActivationDistance = 0.8f;
    public LayerMask groundLayer = -1;

    [Header("Audio")]
    public AudioSource placementAudioSource;
    public AudioClip[] blockPlacementSounds = new AudioClip[4];
    public float placementPitchVariation = 0.2f;
    public AudioSource blockSwitchAudioSource;
    public AudioClip blockSwitchSound;
    public AudioSource buildModeAudioSource;
    public AudioClip buildModeEnterSound;
    public AudioClip buildModeExitSound;
    public AudioClip rotationSound;
    public AudioClip insufficientResourcesSound;

    [Header("References")]
    public FirstPersonController fpsController;
    public WeaponShooting weaponShooting;
    public WeaponController weaponController;
    public BuildingBlocksMenu buildingMenu;

    private bool buildingMode = false;
    private int currentBlockIndex = 0;
    private GameObject ghostObject;
    private Camera playerCamera;
    private bool wasMouseLocked;
    private MeshRenderer ghostRenderer;
    private bool canPlaceBlock = true;
    private float currentRotationY = 0f;

    // Snap system variables
    private SnapPoint targetSnapPoint;
    private Vector3 freeformPosition;
    private bool isSnapping = false;
    private List<BuildableBlock> placedBlocks = new List<BuildableBlock>();

    void Start()
    {
        // Find components if not assigned
        if (fpsController == null)
            fpsController = FindObjectOfType<FirstPersonController>();

        if (weaponShooting == null)
            weaponShooting = FindObjectOfType<WeaponShooting>();

        if (weaponController == null)
            weaponController = FindObjectOfType<WeaponController>();

        if (buildingMenu == null)
            buildingMenu = FindObjectOfType<BuildingBlocksMenu>();

        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        CreateGhostObject();

        // Store initial cursor state
        wasMouseLocked = Cursor.lockState == CursorLockMode.Locked;

        // Find existing blocks in scene
        FindExistingBlocks();

        // Initialize block database if available
        if (blockDatabase != null && blockPrefabs.Length == 0)
        {
            InitializeFromDatabase();
        }
    }

    void InitializeFromDatabase()
    {
        if (blockDatabase == null) return;

        // Create blockPrefabs array from database
        blockPrefabs = new GameObject[blockDatabase.GetBlockCount()];
        for (int i = 0; i < blockDatabase.GetBlockCount(); i++)
        {
            BuildingBlockData blockData = blockDatabase.GetBlockData(i);
            if (blockData != null)
            {
                blockPrefabs[i] = blockData.blockPrefab;
            }
        }
    }

    void Update()
    {
        HandleInput();

        if (buildingMode)
        {
            UpdateGhostPosition();
            HandleBlockSelection();
            HandleBlockRotation();
            HandleBlockPlacement();
        }
    }

    void FindExistingBlocks()
    {
        BuildableBlock[] existingBlocks = FindObjectsOfType<BuildableBlock>();
        foreach (BuildableBlock block in existingBlocks)
        {
            if (block.isPlaced && !placedBlocks.Contains(block))
            {
                placedBlocks.Add(block);
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleBuildingMode();
        }

        // Handle building menu toggle (only in building mode)
        if (buildingMode && Input.GetKeyDown(KeyCode.B))
        {
            if (buildingMenu != null)
            {
                if (buildingMenu.IsMenuOpen())
                    buildingMenu.CloseMenu();
                else
                    buildingMenu.OpenMenu();
            }
        }
    }

    void HandleBlockRotation()
    {
        // Don't allow rotation when menu is open
        if (buildingMenu != null && buildingMenu.IsMenuOpen()) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotationY += 45f;
            if (currentRotationY >= 360f)
                currentRotationY = 0f;

            UpdateGhostRotation();

            if (blockSwitchAudioSource != null && rotationSound != null)
            {
                blockSwitchAudioSource.PlayOneShot(rotationSound);
            }
        }
    }

    void UpdateGhostRotation()
    {
        if (ghostObject != null)
        {
            Quaternion baseRotation = Quaternion.Euler(0, currentRotationY, 0);

            if (isSnapping && targetSnapPoint != null)
            {
                ghostObject.transform.rotation = targetSnapPoint.GetSnapRotation() * baseRotation;
            }
            else
            {
                ghostObject.transform.rotation = baseRotation;
            }
        }
    }

    void ToggleBuildingMode()
    {
        buildingMode = !buildingMode;
        ghostObject.SetActive(buildingMode);

        if (buildModeAudioSource != null)
        {
            AudioClip soundToPlay = buildingMode ? buildModeEnterSound : buildModeExitSound;
            if (soundToPlay != null)
            {
                buildModeAudioSource.PlayOneShot(soundToPlay);
            }
        }

        if (weaponController != null)
        {
            weaponController.OnBuildingModeChanged(buildingMode);
        }

        if (weaponShooting != null)
            weaponShooting.enabled = !buildingMode;

        // Close building menu when exiting building mode
        if (!buildingMode && buildingMenu != null && buildingMenu.IsMenuOpen())
        {
            buildingMenu.CloseMenu();
        }

        Debug.Log($"Building mode: {(buildingMode ? "ON" : "OFF")}");
    }

    void UpdateGhostPosition()
    {
        // Don't update ghost when menu is open
        if (buildingMenu != null && buildingMenu.IsMenuOpen())
        {
            ghostObject.SetActive(false);
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // Reset snap state
        targetSnapPoint = null;
        isSnapping = false;

        bool hasValidPosition = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildableLayer))
        {
            freeformPosition = hit.point + hit.normal * 0.1f;

            // Optionally snap to ground
            if (enableSnapToGround)
            {
                if (Physics.Raycast(freeformPosition + Vector3.up * groundSnapDistance,
                                  Vector3.down, out RaycastHit groundHit,
                                  groundSnapDistance * 2, groundLayer))
                {
                    freeformPosition = groundHit.point;
                }
            }

            ghostObject.transform.position = freeformPosition;
            hasValidPosition = true;
        }

        // Only check for snapping if we're close enough to a snap point
        if (hasValidPosition)
        {
            SnapPoint closestSnapPoint = FindClosestSnapPoint(ghostObject.transform.position);

            if (closestSnapPoint != null)
            {
                float distanceToSnapPoint = Vector3.Distance(ghostObject.transform.position, closestSnapPoint.GetSnapPosition());

                // Only snap if we're within the activation distance
                if (distanceToSnapPoint <= snapActivationDistance)
                {
                    targetSnapPoint = closestSnapPoint;
                    isSnapping = true;
                    ghostObject.transform.position = closestSnapPoint.GetSnapPosition();
                }
            }
        }

        ghostObject.SetActive(hasValidPosition);
        UpdateGhostRotation();

        // Check if we can afford the current block
        canPlaceBlock = hasValidPosition && CanAffordCurrentBlock();
        UpdateGhostMaterial();
    }

    bool CanAffordCurrentBlock()
    {
        if (blockDatabase == null || ResourceManager.Instance == null)
            return true; // Default to true if no resource system

        BuildingBlockData blockData = blockDatabase.GetBlockData(currentBlockIndex);
        if (blockData == null) return true;

        return blockData.CanAfford(
            ResourceManager.Instance.GetWood(),
            ResourceManager.Instance.GetStone(),
            ResourceManager.Instance.GetClay()
        );
    }

    SnapPoint FindClosestSnapPoint(Vector3 position)
    {
        SnapPoint closestSnapPoint = null;
        float closestDistance = float.MaxValue;

        foreach (BuildableBlock block in placedBlocks)
        {
            if (block == null) continue;

            foreach (SnapPoint snapPoint in block.GetSnapPoints())
            {
                if (snapPoint.isOccupied) continue;

                float distance = Vector3.Distance(position, snapPoint.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSnapPoint = snapPoint;
                }
            }
        }

        return closestSnapPoint;
    }

    void HandleBlockSelection()
    {
        // Don't allow block selection when menu is open
        if (buildingMenu != null && buildingMenu.IsMenuOpen()) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int previousBlockIndex = currentBlockIndex;

            if (scroll > 0)
                currentBlockIndex = (currentBlockIndex + 1) % blockPrefabs.Length;
            else
                currentBlockIndex = (currentBlockIndex - 1 + blockPrefabs.Length) % blockPrefabs.Length;

            if (previousBlockIndex != currentBlockIndex)
            {
                if (blockSwitchAudioSource != null && blockSwitchSound != null)
                {
                    blockSwitchAudioSource.PlayOneShot(blockSwitchSound);
                }

                UpdateGhostMesh();
            }
        }
    }

    void HandleBlockPlacement()
    {
        // Don't allow placement when menu is open
        if (buildingMenu != null && buildingMenu.IsMenuOpen()) return;

        if (Input.GetMouseButtonDown(0) && ghostObject.activeInHierarchy && canPlaceBlock)
        {
            PlaceBlock();
        }

        if (Input.GetMouseButtonDown(1))
        {
            RemoveBlock();
        }
    }

    void UpdateGhostMaterial()
    {
        if (ghostRenderer != null)
        {
            // Use invalid material if can't afford, otherwise use normal ghost material
            ghostRenderer.material = canPlaceBlock ? ghostMaterial : invalidGhostMaterial;
        }
    }

    void PlaceBlock()
    {
        // Check resource costs
        BuildingBlockData blockData = null;
        if (blockDatabase != null)
        {
            blockData = blockDatabase.GetBlockData(currentBlockIndex);

            if (blockData != null && ResourceManager.Instance != null)
            {
                // Check if we can afford it
                if (!blockData.CanAfford(
                    ResourceManager.Instance.GetWood(),
                    ResourceManager.Instance.GetStone(),
                    ResourceManager.Instance.GetClay()))
                {
                    // Play insufficient resources sound
                    if (placementAudioSource != null && insufficientResourcesSound != null)
                    {
                        placementAudioSource.PlayOneShot(insufficientResourcesSound);
                    }
                    Debug.Log($"Cannot afford {blockData.blockName}! Cost: {blockData.GetCostString()}");
                    return;
                }

                // Spend the resources
                bool success = ResourceManager.Instance.SpendResources(
                    blockData.woodCost,
                    blockData.stoneCost,
                    blockData.clayCost
                );

                if (!success)
                {
                    Debug.LogError("Failed to spend resources even though we checked we could afford it!");
                    return;
                }
            }
        }

        Vector3 placePos = ghostObject.transform.position;
        Quaternion placeRotation = ghostObject.transform.rotation;

        GameObject newBlockObject = Instantiate(blockPrefabs[currentBlockIndex], placePos, placeRotation);
        BuildableBlock newBlock = newBlockObject.GetComponent<BuildableBlock>();

        if (newBlock == null)
        {
            newBlock = newBlockObject.AddComponent<BuildableBlock>();
        }

        newBlock.PlaceBlock();
        placedBlocks.Add(newBlock);

        // Handle snapping
        if (isSnapping && targetSnapPoint != null)
        {
            targetSnapPoint.SetOccupied(true);

            // Find the closest snap point on the new block to connect
            SnapPoint newBlockSnapPoint = newBlock.GetClosestAvailableSnapPoint(targetSnapPoint.transform.position);
            if (newBlockSnapPoint != null)
            {
                newBlockSnapPoint.SetOccupied(true);
            }
        }

        // Play placement sound
        if (placementAudioSource != null && currentBlockIndex < blockPlacementSounds.Length)
        {
            AudioClip placementSound = blockPlacementSounds[currentBlockIndex];
            if (placementSound != null)
            {
                placementAudioSource.pitch = 1f + Random.Range(-placementPitchVariation, placementPitchVariation);
                placementAudioSource.PlayOneShot(placementSound);
            }
        }

        string blockName = blockData != null ? blockData.blockName : "Block";
        Debug.Log($"Placed {blockName}");
    }

    void RemoveBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance))
        {
            BuildableBlock block = hit.collider.GetComponent<BuildableBlock>();
            if (block != null && block.isPlaced)
            {
                // Free up snap points
                foreach (SnapPoint snapPoint in block.GetSnapPoints())
                {
                    snapPoint.SetOccupied(false);
                }

                placedBlocks.Remove(block);
                block.DestroyBlock();

                // Play destruction sound
                if (placementAudioSource != null && blockPlacementSounds.Length > currentBlockIndex)
                {
                    placementAudioSource.pitch = 0.7f + Random.Range(-0.1f, 0.1f);
                    if (blockPlacementSounds[currentBlockIndex] != null)
                    {
                        placementAudioSource.PlayOneShot(blockPlacementSounds[currentBlockIndex]);
                    }
                }
            }
        }
    }

    void CreateGhostObject()
    {
        ghostObject = new GameObject("GhostBlock");
        ghostObject.SetActive(false);
        ghostObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        UpdateGhostMesh();
    }

    void UpdateGhostMesh()
    {
        MeshRenderer renderer = ghostObject.GetComponent<MeshRenderer>();
        MeshFilter filter = ghostObject.GetComponent<MeshFilter>();

        if (renderer == null) renderer = ghostObject.AddComponent<MeshRenderer>();
        if (filter == null) filter = ghostObject.AddComponent<MeshFilter>();

        ghostRenderer = renderer;

        if (currentBlockIndex < blockPrefabs.Length && blockPrefabs[currentBlockIndex] != null)
        {
            MeshFilter prefabFilter = blockPrefabs[currentBlockIndex].GetComponent<MeshFilter>();
            if (prefabFilter != null)
            {
                filter.mesh = prefabFilter.sharedMesh;
            }
        }

        renderer.material = canPlaceBlock ? ghostMaterial : invalidGhostMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        UpdateGhostRotation();
    }

    // New method: Set selected block from menu
    public void SetSelectedBlock(int blockIndex)
    {
        if (blockIndex >= 0 && blockIndex < blockPrefabs.Length)
        {
            currentBlockIndex = blockIndex;
            UpdateGhostMesh();

            // Get block name for feedback
            string blockName = "Block";
            if (blockDatabase != null)
            {
                BuildingBlockData blockData = blockDatabase.GetBlockData(blockIndex);
                if (blockData != null)
                    blockName = blockData.blockName;
            }

            Debug.Log($"Selected building block: {blockName}");
        }
    }

    // Public getters
    public bool IsBuildingMode() => buildingMode;
    public float GetCurrentRotation() => currentRotationY;
    public bool IsSnapping() => isSnapping;
    public SnapPoint GetTargetSnapPoint() => targetSnapPoint;
    public int GetCurrentBlockIndex() => currentBlockIndex;
    public BuildingBlockData GetCurrentBlockData()
    {
        if (blockDatabase != null)
            return blockDatabase.GetBlockData(currentBlockIndex);
        return null;
    }
}