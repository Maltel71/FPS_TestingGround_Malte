using UnityEngine;

public class HomingMissileV2 : MonoBehaviour
{
    [Header("Missile Settings")]
    public float speed = 10f;
    public float turnSpeed = 5f;
    public float damage = 25f;
    public float lifetime = 5f;

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
}