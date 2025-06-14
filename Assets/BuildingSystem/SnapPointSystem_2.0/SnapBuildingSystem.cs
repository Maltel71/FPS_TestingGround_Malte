using UnityEngine;
using System.Collections.Generic;

public class SnapBuildingSystem : MonoBehaviour
{
    [Header("Building Settings")]
    public GameObject[] blockPrefabs = new GameObject[4];
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
    public float snapActivationDistance = 0.8f; // How close to snap point before snapping activates
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

    [Header("References")]
    public FirstPersonController fpsController;
    public WeaponShooting weaponShooting;
    public WeaponController weaponController;

    private bool buildingMode = false;
    private int currentBlockIndex = 0;
    private GameObject ghostObject;
    private Camera playerCamera;
    private bool wasMouseLocked;
    private MeshRenderer ghostRenderer;
    private bool canPlaceBlock = true; // Always true now - no placement restrictions
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

        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        CreateGhostObject();

        // Store initial cursor state
        wasMouseLocked = Cursor.lockState == CursorLockMode.Locked;

        // Find existing blocks in scene
        FindExistingBlocks();
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
    }

    void HandleBlockRotation()
    {
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
                // Align with snap point rotation
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

        Debug.Log($"Building mode: {(buildingMode ? "ON" : "OFF")}");
    }

    void UpdateGhostPosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // Reset snap state
        targetSnapPoint = null;
        isSnapping = false;

        // Always try freeform placement first
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

        // Always allow placement - no restrictions
        canPlaceBlock = hasValidPosition;
        UpdateGhostMaterial();
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
        if (Input.GetMouseButtonDown(0) && ghostObject.activeInHierarchy)
        {
            PlaceBlock();
        }

        if (Input.GetMouseButtonDown(1))
        {
            RemoveBlock();
        }
    }

    bool IsValidPlacementPosition()
    {
        if (!ghostObject.activeInHierarchy) return false;

        Vector3 checkPosition = ghostObject.transform.position;

        // Check for overlapping blocks
        Collider[] overlapping = Physics.OverlapBox(
            checkPosition,
            Vector3.one * 0.3f,
            ghostObject.transform.rotation,
            blockLayer
        );

        foreach (Collider col in overlapping)
        {
            if (col.CompareTag("Block"))
            {
                return false;
            }
        }

        // Check distance to player
        float distanceToPlayer = Vector3.Distance(checkPosition, transform.position);
        if (distanceToPlayer < playerCollisionRadius)
            return false;

        return true;
    }

    void UpdateGhostMaterial()
    {
        if (ghostRenderer != null)
        {
            // Always use the normal ghost material - no invalid placement anymore
            ghostRenderer.material = ghostMaterial;
        }
    }

    void PlaceBlock()
    {
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

        if (blockPrefabs[currentBlockIndex] != null)
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

    public bool IsBuildingMode() => buildingMode;
    public float GetCurrentRotation() => currentRotationY;
    public bool IsSnapping() => isSnapping;
    public SnapPoint GetTargetSnapPoint() => targetSnapPoint;
}