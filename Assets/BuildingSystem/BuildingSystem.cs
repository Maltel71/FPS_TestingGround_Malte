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

    [Header("Grid Settings")]
    public float gridSize = 1f; // Size of each grid square
    public bool showGridGizmos = true; // For debugging in scene view

    [Header("Audio")]
    public AudioSource placementAudioSource;
    public AudioClip[] blockPlacementSounds = new AudioClip[4];
    public float placementPitchVariation = 0.2f;
    public AudioSource blockSwitchAudioSource;
    public AudioClip blockSwitchSound;
    public AudioSource rotationAudioSource; // New audio source for rotation
    public AudioClip rotationSound; // Sound for rotation
    public AudioSource buildModeAudioSource;
    public AudioClip buildModeEnterSound;
    public AudioClip buildModeExitSound;

    [Header("References")]
    public FirstPersonController fpsController;
    public WeaponShooting weaponShooting;
    public WeaponController weaponController;

    private bool buildingMode = false;
    private int currentBlockIndex = 0;
    private float currentRotation = 0f; // Current rotation in degrees (0, 45, 90, 135, etc.)
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

        if (weaponController == null)
            weaponController = FindObjectOfType<WeaponController>();

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
            HandleRotation();
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

        // Play build mode sound
        if (buildModeAudioSource != null)
        {
            AudioClip soundToPlay = buildingMode ? buildModeEnterSound : buildModeExitSound;
            if (soundToPlay != null)
            {
                buildModeAudioSource.PlayOneShot(soundToPlay);
            }
        }

        // Notify weapon controller about build mode change
        if (weaponController != null)
        {
            weaponController.OnBuildingModeChanged(buildingMode);
        }

        // Disable/enable other systems
        if (weaponShooting != null)
            weaponShooting.enabled = !buildingMode;

        Debug.Log($"Building mode: {(buildingMode ? "ON" : "OFF")}");
    }

    void UpdateGhostPosition()
    {
        // Use screen center raycast
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // Raycast against everything (not just building layer) to detect blocks too
        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance))
        {
            Vector3 targetPosition;

            // If we hit a block, place on top of it
            if (hit.collider.CompareTag("Block"))
            {
                // Get the top surface of the hit block
                Bounds blockBounds = hit.collider.bounds;
                targetPosition = new Vector3(hit.point.x, blockBounds.max.y, hit.point.z);
            }
            else
            {
                // Hit ground or other surface, place on the hit point
                targetPosition = hit.point;
            }

            // Snap to grid
            Vector3 snappedPosition = SnapToGrid(targetPosition);

            ghostObject.transform.position = snappedPosition;
            ghostObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
            ghostObject.SetActive(true);

            // Check if position is valid for placement
            canPlaceBlock = IsValidPlacementPosition(snappedPosition);
            UpdateGhostMaterial();
        }
        else
        {
            ghostObject.SetActive(false);
            canPlaceBlock = false;
        }
    }

    Vector3 SnapToGrid(Vector3 worldPosition)
    {
        // Snap to grid - this snaps the position to the corner of each grid square
        float snappedX = Mathf.Round(worldPosition.x / gridSize) * gridSize;
        float snappedZ = Mathf.Round(worldPosition.z / gridSize) * gridSize;

        // For Y, we can either snap to a grid or keep it more flexible for stacking
        // Using Round instead of Floor allows for better vertical stacking
        float snappedY = Mathf.Round(worldPosition.y / gridSize) * gridSize;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Rotate by 45 degrees each time
            currentRotation += 45f;

            // Keep rotation between 0-360 degrees
            if (currentRotation >= 360f)
                currentRotation = 0f;

            // Update ghost object rotation
            ghostObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            // Play rotation sound
            if (rotationAudioSource != null && rotationSound != null)
            {
                rotationAudioSource.PlayOneShot(rotationSound);
            }

            Debug.Log($"Rotated to: {currentRotation} degrees");
        }
    }

    void HandleBlockSelection()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int previousBlockIndex = currentBlockIndex;

            if (scroll > 0)
                currentBlockIndex = (currentBlockIndex + 1) % blockPrefabs.Length;
            else
                currentBlockIndex = (currentBlockIndex - 1 + blockPrefabs.Length) % blockPrefabs.Length;

            // Play block switch sound only if block actually changed
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
        // Simple overlap check - similar to Minecraft
        // Use a small box to check if the exact position is occupied
        Collider[] overlapping = Physics.OverlapBox(position, Vector3.one * 0.4f, Quaternion.Euler(0, currentRotation, 0));

        foreach (Collider col in overlapping)
        {
            // If there's already a block at this exact position, can't place
            if (col.CompareTag("Block"))
            {
                // Check if the centers are very close (same grid position)
                float distance = Vector3.Distance(position, col.transform.position);
                if (distance < 0.1f) // Very close = same grid position
                    return false;
            }
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
        Quaternion placeRot = Quaternion.Euler(0, currentRotation, 0);

        // Double-check validity before placing
        if (!IsValidPlacementPosition(placePos))
            return;

        GameObject newBlock = Instantiate(blockPrefabs[currentBlockIndex], placePos, placeRot);
        newBlock.tag = "Block";

        // Add to building layer if it's not already there
        newBlock.layer = LayerMaskToLayer(buildingLayer);

        // Play placement sound for the current block type
        if (placementAudioSource != null && currentBlockIndex < blockPlacementSounds.Length)
        {
            AudioClip placementSound = blockPlacementSounds[currentBlockIndex];
            if (placementSound != null)
            {
                // Add pitch variation for more natural sound
                placementAudioSource.pitch = 1f + Random.Range(-placementPitchVariation, placementPitchVariation);
                placementAudioSource.PlayOneShot(placementSound);
            }
        }

        Debug.Log($"Placed block at {placePos} with rotation {currentRotation}°");
    }

    void RemoveBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // Use a broader layermask or no layermask to detect blocks
        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance))
        {
            if (hit.collider.CompareTag("Block"))
            {
                Destroy(hit.collider.gameObject);

                // Optional: Play a destruction sound
                if (placementAudioSource != null)
                {
                    // Use the same sound as placement, with lower pitch
                    placementAudioSource.pitch = 0.7f + Random.Range(-0.1f, 0.1f);
                    if (blockPlacementSounds.Length > currentBlockIndex && blockPlacementSounds[currentBlockIndex] != null)
                    {
                        placementAudioSource.PlayOneShot(blockPlacementSounds[currentBlockIndex]);
                    }
                }
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

        // Set initial ghost material and rotation
        renderer.material = canPlaceBlock ? ghostMaterial : invalidGhostMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ghostObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }

    // Debug visualization for the grid in Scene view
    void OnDrawGizmos()
    {
        if (!showGridGizmos || !buildingMode) return;

        // Draw a simple grid around the player for debugging
        Gizmos.color = Color.white;
        Vector3 playerPos = transform.position;

        // Draw grid lines in a 20x20 area around player
        for (int x = -10; x <= 10; x++)
        {
            for (int z = -10; z <= 10; z++)
            {
                Vector3 gridPoint = new Vector3(
                    Mathf.Floor(playerPos.x / gridSize) * gridSize + (x * gridSize),
                    playerPos.y,
                    Mathf.Floor(playerPos.z / gridSize) * gridSize + (z * gridSize)
                );

                Gizmos.DrawWireCube(gridPoint + Vector3.one * (gridSize * 0.5f), Vector3.one * gridSize);
            }
        }
    }

    // Public getter for other systems to check build mode status
    public bool IsBuildingMode() => buildingMode;

    // Public getter for current rotation
    public float GetCurrentRotation() => currentRotation;
}