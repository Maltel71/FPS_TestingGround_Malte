using UnityEngine;

public class BreakableCube : MonoBehaviour
{
    [Header("Breaking Settings")]
    public GameObject fracturedPrefab;
    public float breakThreshold = 50f;
    public float damagePerShot = 20f;

    [Header("Explosion Settings")]
    public float explosionForce = 300f;
    public float explosionRadius = 5f;

    [Header("Fall Damage")]
    public float fallDamageMultiplier = 0.5f;
    public float minVelocityForDamage = 5f;

    [Header("Debug")]
    public bool showDamageInConsole = false;
    public float currentDamage = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(float damage)
    {
        AddDamage(damage);

        if (showDamageInConsole)
            Debug.Log($"{name} took {damage} raycast damage");
    }

    // This works automatically with your WeaponShooting raycast system
    // Your WeaponShooting script will apply force via AddForceAtPosition
    void OnCollisionEnter(Collision collision)
    {
        // Handle fall damage from high-speed collisions
        if (collision.relativeVelocity.magnitude > minVelocityForDamage)
        {
            float fallDamage = collision.relativeVelocity.magnitude * fallDamageMultiplier;
            AddDamage(fallDamage);

            if (showDamageInConsole)
                Debug.Log($"{name} took {fallDamage:F1} fall damage");
        }

        // Handle damage from being shot (detect force application)
        if (collision.impulse.magnitude > damagePerShot * 0.5f)
        {
            AddDamage(damagePerShot);

            if (showDamageInConsole)
                Debug.Log($"{name} took {damagePerShot} shot damage");
        }
    }

    void AddDamage(float damage)
    {
        currentDamage += damage;

        if (currentDamage >= breakThreshold)
        {
            BreakCube();
        }
    }

    void BreakCube()
    {
        Vector3 explosionCenter = transform.position;

        if (fracturedPrefab != null)
        {
            // Spawn fractured version
            GameObject fractured = Instantiate(fracturedPrefab, transform.position, transform.rotation);

            // Apply explosion force to all pieces
            Rigidbody[] pieces = fractured.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody piece in pieces)
            {
                piece.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, 0.5f, ForceMode.Impulse);
            }
        }

        // Apply explosion force to nearby objects
        Collider[] nearbyObjects = Physics.OverlapSphere(explosionCenter, explosionRadius);
        foreach (Collider col in nearbyObjects)
        {
            Rigidbody nearbyRb = col.GetComponent<Rigidbody>();
            if (nearbyRb != null && nearbyRb != rb)
            {
                nearbyRb.AddExplosionForce(explosionForce * 0.5f, explosionCenter, explosionRadius, 0.3f, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }
}