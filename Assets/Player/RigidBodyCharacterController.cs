using UnityEngine;

public class RigidBodyCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float acceleration = 10f;
    public float deceleration = 10f;
    public float airControl = 2f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundMask = 1;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;
    public bool lockCursor = true;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isGrounded;
    private float currentSpeed;

    // Input
    private float horizontalInput;
    private float verticalInput;
    private bool isSprinting;
    private bool jumpInput;

    // Mouse look
    private float xRotation = 0f;
    private float mouseX;
    private float mouseY;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Set rigidbody properties for smooth movement
        rb.linearDamping = 0f; // We'll handle drag manually
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth visual movement

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // If no camera transform assigned, try to find Main Camera
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }
    }

    private void Update()
    {
        HandleInput();
        HandleMouseLook();
        CheckGrounded();
        ControlSpeed();

        // Unlock cursor with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = lockCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !lockCursor;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        HandleJump();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        jumpInput = Input.GetKeyDown(KeyCode.Space); // Changed to GetKeyDown for single jump

        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
    }

    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        // Rotate player body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void CheckGrounded()
    {
        // Simple ground check from player's bottom
        Vector3 rayOrigin = transform.position;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundMask);

        // Debug - you can see this in Scene view
        Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        // Additional debug info
        if (jumpInput)
        {
            Debug.Log($"Jump pressed! Grounded: {isGrounded}");
        }
    }

    private void ControlSpeed()
    {
        currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
    }

    private void MovePlayer()
    {
        // Calculate movement direction relative to player's rotation
        moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

        // Get current horizontal velocity
        Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 targetVel = moveDirection * currentSpeed;

        if (isGrounded)
        {
            // Smooth acceleration/deceleration on ground
            Vector3 velocityDiff = targetVel - currentVel;

            // Use different rates for acceleration vs deceleration
            float accelRate = (moveDirection.magnitude > 0.1f) ? acceleration : deceleration;

            // Apply smooth velocity change
            Vector3 movement = velocityDiff * accelRate * Time.fixedDeltaTime;

            // Clamp the movement to prevent overshooting
            if (movement.magnitude > velocityDiff.magnitude)
                movement = velocityDiff;

            rb.AddForce(movement, ForceMode.VelocityChange);
        }
        else
        {
            // Reduced air control for more realistic physics
            Vector3 airMovement = moveDirection * airControl;
            rb.AddForce(airMovement, ForceMode.Force);
        }
    }

    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            Debug.Log("JUMPING!");
            // Reset Y velocity first, then add jump force
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }
    }
}