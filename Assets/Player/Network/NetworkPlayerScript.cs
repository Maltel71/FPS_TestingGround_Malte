using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetworkFirstPersonController : NetworkBehaviour
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

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;
    private float standingHeight;
    private Vector3 standingCameraPos;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera != null)
                cameraTransform = mainCamera.transform;
            else
                Debug.LogError("No camera assigned to NetworkFirstPersonController and none found in children.");
        }

        // Store original values
        standingHeight = controller.height;
        if (cameraTransform != null)
            standingCameraPos = cameraTransform.localPosition;

        // Only the owner should control this player
        if (!IsOwner)
        {
            // Disable camera for non-owner clients
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(false);

            // Disable the CharacterController for non-owners to avoid conflicts
            controller.enabled = false;
            return;
        }

        // Owner setup
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // Only process input if we own this player
        if (!IsOwner) return;

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
        ApplyGravity();
    }

    private void HandleCrouch()
    {
        bool wantsToCrouch = Input.GetKey(KeyCode.LeftControl);

        if (wantsToCrouch != isCrouching)
        {
            isCrouching = wantsToCrouch;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float targetCameraY = isCrouching ? standingCameraPos.y - (standingHeight - crouchHeight) * 0.5f : standingCameraPos.y;

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        if (cameraTransform != null)
        {
            Vector3 newCameraPos = cameraTransform.localPosition;
            newCameraPos.y = Mathf.Lerp(newCameraPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = newCameraPos;
        }
    }

    private void HandleSprint()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
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

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
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
}