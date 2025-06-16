using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject weaponPrefab; // The weapon prefab to instantiate when picked up
    public string weaponName = "Weapon";
    public WeaponController.WeaponCategory weaponCategory;

    [Header("Pickup Display")]
    public GameObject pickupUI; // UI element to show when player is near (optional)
    public string pickupText = "Press E to pickup";

    [Header("Physics Settings")]
    public bool usePhysics = true; // Whether to use physics or not
    public float groundCheckDistance = 0.1f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;

    private Rigidbody rb;
    private bool isGrounded = false;

    void Start()
    {
        // Add a collider if one doesn't exist
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * 0.8f; // Adjust size as needed
        }

        // Make sure the collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Add Rigidbody for physics if enabled
        if (usePhysics)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            // Set up physics properties
            rb.mass = 1f;
            rb.angularDamping = 0.5f;
            rb.linearDamping = 0.1f;
        }

        // Ensure pickup UI is initially hidden
        if (pickupUI != null)
            pickupUI.SetActive(false);

        // Set the layer to weapon pickup layer
        gameObject.layer = LayerMask.NameToLayer("Default"); // You might want to create a specific layer
    }

    void Update()
    {
        if (usePhysics)
        {
            CheckGrounded();
        }
    }

    void CheckGrounded()
    {
        // Simple ground check
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance + 0.1f))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // Method to set up the weapon pickup programmatically
    public void SetupWeapon(GameObject prefab, string name, WeaponController.WeaponCategory category)
    {
        weaponPrefab = prefab;
        weaponName = name;
        weaponCategory = category;

        // Update the GameObject name for easier identification
        gameObject.name = $"{weaponName}_Pickup";
    }

    // Method to create a visual representation of the weapon (optional)
    public void CreateVisualRepresentation()
    {
        if (weaponPrefab != null)
        {
            // Create a visual copy of the weapon
            GameObject visual = Instantiate(weaponPrefab, transform);
            visual.name = "VisualRepresentation";

            // Remove any scripts that shouldn't be on the pickup
            MonoBehaviour[] scripts = visual.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script.GetType() != typeof(Transform))
                {
                    Destroy(script);
                }
            }

            // Remove colliders from the visual representation to avoid conflicts
            Collider[] colliders = visual.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }

            // Remove rigidbodies from the visual representation
            Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                Destroy(rb);
            }

            // Scale down slightly for pickup representation
            visual.transform.localScale *= 0.8f;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
    }

    // Static method to create weapon pickups easily
    public static GameObject CreateWeaponPickup(Vector3 position, GameObject weaponPrefab, string weaponName, WeaponController.WeaponCategory category)
    {
        // Create pickup object
        GameObject pickupObj = new GameObject($"{weaponName}_Pickup");
        pickupObj.transform.position = position;

        // Add WeaponPickup component
        WeaponPickup pickup = pickupObj.AddComponent<WeaponPickup>();
        pickup.SetupWeapon(weaponPrefab, weaponName, category);

        // Add collider
        BoxCollider collider = pickupObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = Vector3.one * 0.8f;

        // Create visual representation
        pickup.CreateVisualRepresentation();

        return pickupObj;
    }

    void OnValidate()
    {
        // Ensure weapon name matches the category
        if (string.IsNullOrEmpty(weaponName) || weaponName == "Weapon")
        {
            weaponName = weaponCategory.ToString();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Optional: Add trigger events here if needed
        if (other.CompareTag("Player"))
        {
            // Could trigger additional effects when player enters pickup range
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Hide pickup UI when player leaves
        if (other.CompareTag("Player") && pickupUI != null)
        {
            pickupUI.SetActive(false);
        }
    }

    // Method for external systems to trigger pickup
    public void OnPickedUp()
    {
        // Play pickup sound
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        // Hide pickup UI
        if (pickupUI != null)
        {
            pickupUI.SetActive(false);
        }

        // The WeaponController will destroy this object after pickup
    }

    void OnDrawGizmosSelected()
    {
        // Draw pickup range visualization
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f); // Visual pickup range

        // Draw weapon category info
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
    }
}