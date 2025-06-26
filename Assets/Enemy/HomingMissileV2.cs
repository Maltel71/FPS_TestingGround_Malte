using UnityEngine;

public class HomingMissileV2 : MonoBehaviour
{
    [Header("Missile Settings")]
    public float speed = 10f;
    public float turnSpeed = 5f;
    public float damage = 25f;
    public float lifetime = 5f;

    [Header("Explosion Settings")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public float upwardModifier = 1f;
    public LayerMask affectedLayers = -1; // All layers by default
    public GameObject explosionEffectPrefab; // Optional explosion visual effect

    private Transform target;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Use Invoke instead of Destroy with lifetime to be more explicit
        Invoke(nameof(DestroyMissile), lifetime);
    }

    void DestroyMissile()
    {
        // Trigger explosion before destroying
        Explode();

        // Only destroy this specific missile GameObject
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.linearVelocity = Vector3.Slerp(rb.linearVelocity, direction * speed, Time.fixedDeltaTime * turnSpeed);
            transform.LookAt(transform.position + rb.linearVelocity);
        }
        else
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Explode()
    {
        // Spawn explosion effect if available
        if (explosionEffectPrefab != null)
        {
            GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            // Destroy the effect after a few seconds (adjust as needed)
            Destroy(explosionEffect, 3f);
        }

        // Find all colliders within explosion radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, affectedLayers);

        foreach (Collider hit in colliders)
        {
            // Skip the missile itself
            if (hit.gameObject == gameObject) continue;

            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                // Apply explosion force
                hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardModifier, ForceMode.Impulse);
            }

            // Apply damage to player if within explosion radius
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // Calculate damage based on distance (optional - you can remove this for constant damage)
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float damageFalloff = Mathf.Clamp01(1f - (distance / explosionRadius));
                    float finalDamage = damage * damageFalloff;

                    playerHealth.TakeDamage(finalDamage);
                }
            }
        }

        Debug.Log($"Missile exploded at {transform.position} affecting {colliders.Length} objects");
    }

    void OnTriggerEnter(Collider other)
    {
        // Make sure we're only destroying the missile, not anything else
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);

            DestroyMissile();
        }
        else if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            DestroyMissile();
        }
    }

    // Visualize explosion radius in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}