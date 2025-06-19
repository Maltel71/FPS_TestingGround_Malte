using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool lockCursor = true;

    [Header("Input Settings")]
    [SerializeField] private KeyCode carryKey = KeyCode.E;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("References")]
    [SerializeField] private PauseMenuManager pauseMenuManager;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private BuildingBlocksMenu buildingBlocksMenu;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float carryDistance = 1f;
    [SerializeField] private LayerMask pickupMask = -1;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float throwTorque = 5f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;
    private float standingHeight;
    private Vector3 standingCameraPos;
    private Carriable carriedObject;
    private Vector3 carryPosition;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera != null)
                cameraTransform = mainCamera.transform;
            else
                Debug.LogError("No camera assigned to FirstPersonController and none found in children.");
        }

        // Find pause menu manager if not assigned
        if (pauseMenuManager == null)
            pauseMenuManager = FindObjectOfType<PauseMenuManager>();

        // Find weapon controller if not assigned
        if (weaponController == null)
            weaponController = FindObjectOfType<WeaponController>();

        // Find building blocks menu if not assigned
        if (buildingBlocksMenu == null)
            buildingBlocksMenu = FindObjectOfType<BuildingBlocksMenu>();

        // Store original values
        standingHeight = controller.height;
        standingCameraPos = cameraTransform.localPosition;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // Check if any menu is open - if so, skip all input handling including look
        if ((pauseMenuManager != null && pauseMenuManager.IsOptionsMenuOpen()) ||
            (buildingBlocksMenu != null && buildingBlocksMenu.IsMenuOpen()))
            return;

        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        HandleCrouch();
        HandleSprint();
        HandleMovement();
        HandleLook();
        HandleJump();
        HandlePickup();
        UpdateCarriedObject();
        ApplyGravity();
    }

    private void HandleCrouch()
    {
        bool wantsToCrouch = Input.GetKey(crouchKey);

        if (wantsToCrouch != isCrouching)
        {
            isCrouching = wantsToCrouch;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float targetCameraY = isCrouching ? standingCameraPos.y - (standingHeight - crouchHeight) * 0.5f : standingCameraPos.y;

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        Vector3 newCameraPos = cameraTransform.localPosition;
        newCameraPos.y = Mathf.Lerp(newCameraPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
        cameraTransform.localPosition = newCameraPos;
    }

    private void HandleSprint()
    {
        isSprinting = Input.GetKey(sprintKey) && !isCrouching;
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : moveSpeed);
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void ApplyGravity()
    {
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandlePickup()
    {
        if (Input.GetKeyDown(carryKey))
        {
            if (carriedObject != null)
            {
                DropObject();
            }
            else
            {
                TryPickupObject();
            }
        }

        // Handle throwing with left mouse button
        if (Input.GetMouseButtonDown(0) && carriedObject != null)
        {
            ThrowObject();
        }
    }

    private void TryPickupObject()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask))
        {
            Carriable carriable = hit.collider.GetComponent<Carriable>();
            if (carriable != null && !carriable.IsBeingCarried())
            {
                carriedObject = carriable;
                carriedObject.StartCarrying(this); // Pass 'this' controller reference
                UpdateCarryPosition();

                // Disable weapons when picking up
                if (weaponController != null)
                    weaponController.SetWeaponsEnabled(false);
            }
        }
    }

    private void DropObject()
    {
        if (carriedObject != null)
        {
            carriedObject.StopCarrying();
            carriedObject = null;

            // Re-enable weapons when dropping
            if (weaponController != null)
                weaponController.SetWeaponsEnabled(true);
        }
    }

    // New method for force dropping when breakforce is exceeded
    public void ForceDropObject()
    {
        if (carriedObject != null)
        {
            carriedObject.StopCarrying();
            carriedObject = null;

            // Re-enable weapons when force dropping
            if (weaponController != null)
                weaponController.SetWeaponsEnabled(true);

            // Optional: Add some feedback like a sound or screen shake
            Debug.Log("Object dropped due to impact!");
        }
    }

    // New method for throwing objects
    private void ThrowObject()
    {
        if (carriedObject != null)
        {
            Rigidbody carriedRb = carriedObject.GetComponent<Rigidbody>();

            // Stop carrying
            carriedObject.StopCarrying();

            // Apply throw force in camera forward direction
            Vector3 throwDirection = cameraTransform.forward;
            carriedRb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

            // Add random rotation
            Vector3 randomTorque = new Vector3(
                Random.Range(-throwTorque, throwTorque),
                Random.Range(-throwTorque, throwTorque),
                Random.Range(-throwTorque, throwTorque)
            );
            carriedRb.AddTorque(randomTorque, ForceMode.Impulse);

            carriedObject = null;

            // Re-enable weapons when throwing
            if (weaponController != null)
                weaponController.SetWeaponsEnabled(true);

            Debug.Log("Object thrown!");
        }
    }

    // Public getter to check if player is carrying something
    public bool IsCarryingSomething()
    {
        return carriedObject != null;
    }

    private void UpdateCarriedObject()
    {
        if (carriedObject != null)
        {
            UpdateCarryPosition();

            Rigidbody carriedRb = carriedObject.GetComponent<Rigidbody>();

            // Calculate velocity needed to reach target position
            Vector3 velocityNeeded = (carryPosition - carriedObject.transform.position) / Time.fixedDeltaTime;

            // Apply the velocity directly
            carriedRb.linearVelocity = velocityNeeded;
        }
    }

    private void UpdateCarryPosition()
    {
        carryPosition = cameraTransform.position + cameraTransform.forward * carryDistance;
    }

    // Public method to change keybinds at runtime (useful for settings menu)
    public void SetCarryKey(KeyCode newKey)
    {
        carryKey = newKey;
    }

    public void SetCrouchKey(KeyCode newKey)
    {
        crouchKey = newKey;
    }

    public void SetSprintKey(KeyCode newKey)
    {
        sprintKey = newKey;
    }

    // Public getters for current keybinds
    public KeyCode GetCarryKey() => carryKey;
    public KeyCode GetCrouchKey() => crouchKey;
    public KeyCode GetSprintKey() => sprintKey;
}