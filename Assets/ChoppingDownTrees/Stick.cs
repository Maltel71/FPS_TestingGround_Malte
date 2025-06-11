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

    private bool canPickup = false;
    private GameObject player;

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
        // Play pickup sound
        if (audioSource != null && pickupSound != null)
            audioSource.PlayOneShot(pickupSound);

        // Add wood to counter
        if (WoodCounter.Instance != null)
        {
            WoodCounter.Instance.AddWood(1);
        }

        Debug.Log($"Picked up {itemName}!");

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Show pickup range in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}