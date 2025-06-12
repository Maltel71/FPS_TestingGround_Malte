using UnityEngine;

public class Stick : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 2f;
    public string itemName = "Wood Stick";

    [Header("Visual")]
    public GameObject highlightEffect;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public AudioClip collisionSound;

    [Header("Collision Audio Settings")]
    public float collisionCooldown = 0.3f;
    public float minCollisionForce = 1f;

    private bool canPickup = false;
    private GameObject player;
    private float lastCollisionTime = 0f;

    void Start()
    {
        // Find player (assumes player has "Player" tag)
        player = GameObject.FindGameObjectWithTag("Player");

        // If no AudioSource assigned, try to get one from this GameObject
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    void Update()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            canPickup = distance <= pickupRange;

            // Show/hide highlight effect
            if (highlightEffect != null)
                highlightEffect.SetActive(canPickup);

            // Handle pickup input
            if (canPickup && Input.GetKeyDown(KeyCode.E))
            {
                PickupItem();
            }
        }
    }

    void PickupItem()
    {
        // Add wood to counter first
        if (WoodCounter.Instance != null)
        {
            WoodCounter.Instance.AddWood(1);
        }

        Debug.Log($"Picked up {itemName}!");

        // Hide the stick and disable interaction
        HideStickComponents();

        // Play pickup sound and destroy after sound finishes
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
            StartCoroutine(DestroyAfterPickupSound(pickupSound.length));
        }
        else
        {
            // No sound, destroy after short delay
            StartCoroutine(DestroyAfterPickupSound(0.1f));
        }
    }

    void HideStickComponents()
    {
        // Hide all mesh renderers
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disable all colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Disable rigidbody physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // Hide highlight effect immediately
        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        // Disable pickup detection
        canPickup = false;
    }

    System.Collections.IEnumerator DestroyAfterPickupSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if enough time has passed since last collision sound
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        // Check if collision was strong enough
        if (collision.relativeVelocity.magnitude < minCollisionForce)
            return;

        // Play collision sound
        if (audioSource != null && collisionSound != null)
        {
            audioSource.PlayOneShot(collisionSound);
            lastCollisionTime = Time.time;
            Debug.Log($"Stick collision with {collision.gameObject.name}");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show pickup range in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
