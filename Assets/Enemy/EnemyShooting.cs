using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float fireRate = 1f; // Shots per second
    public float shootingRange = 15f;
    public float damage = 10f;
    public LayerMask shootLayerMask = -1; // What can be hit

    [Header("Aim Settings")]
    public Transform gunPoint; // The point where bullets come from
    public Transform armPivot; // The arm/gun that rotates to aim
    public float aimSpeed = 5f;

    [Header("Effects")]
    public AudioClip shootSound;
    public GameObject muzzleFlashPrefab;
    public float muzzleFlashDuration = 0.1f;

    [Header("Debug")]
    public bool showDebugRays = true;

    private Transform target;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private bool isShooting = false;

    void Start()
    {
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Auto-assign gunPoint if not set
        if (gunPoint == null)
            gunPoint = transform;
    }

    void Update()
    {
        if (target != null)
        {
            AimAtTarget();

            if (CanShootTarget())
            {
                if (!isShooting)
                {
                    isShooting = true;
                }

                if (Time.time >= nextFireTime)
                {
                    Shoot();
                    nextFireTime = Time.time + (1f / fireRate);
                }
            }
            else
            {
                isShooting = false;
            }
        }
        else
        {
            isShooting = false;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null)
        {
            isShooting = false;
        }
    }

    void AimAtTarget()
    {
        if (armPivot == null || target == null) return;

        Vector3 direction = (target.position - armPivot.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        armPivot.rotation = Quaternion.Slerp(armPivot.rotation, targetRotation, Time.deltaTime * aimSpeed);
    }

    bool CanShootTarget()
    {
        if (target == null) return false;

        float distanceToTarget = Vector3.Distance(gunPoint.position, target.position);
        if (distanceToTarget > shootingRange) return false;

        // Raycast to check if we have line of sight
        Vector3 direction = (target.position - gunPoint.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(gunPoint.position, direction, out hit, shootingRange, shootLayerMask))
        {
            // Check if we hit the target or something else
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return false;
    }

    void Shoot()
    {
        if (target == null) return;

        Vector3 direction = (target.position - gunPoint.position).normalized;

        // Perform raycast for hit detection
        RaycastHit hit;
        if (Physics.Raycast(gunPoint.position, direction, out hit, shootingRange, shootLayerMask))
        {
            // Check if we hit the player
            if (hit.transform == target || hit.transform.IsChildOf(target))
            {
                // Deal damage to player
                PlayerHealth playerHealth = hit.transform.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                    playerHealth = hit.transform.GetComponentInParent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }

                Debug.Log("Enemy hit player for " + damage + " damage");
            }

            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(gunPoint.position, direction * hit.distance, Color.red, 0.5f);
            }
        }
        else
        {
            // No hit - draw debug ray to max range
            if (showDebugRays)
            {
                Debug.DrawRay(gunPoint.position, direction * shootingRange, Color.blue, 0.5f);
            }
        }

        // Play sound effect
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Show muzzle flash
        if (muzzleFlashPrefab != null)
        {
            ShowMuzzleFlash();
        }
    }

    void ShowMuzzleFlash()
    {
        GameObject flash = Instantiate(muzzleFlashPrefab, gunPoint.position, gunPoint.rotation);

        // Destroy muzzle flash after duration
        Destroy(flash, muzzleFlashDuration);
    }

    // Gizmos for visualization
    void OnDrawGizmosSelected()
    {
        if (gunPoint == null) return;

        // Draw shooting range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gunPoint.position, shootingRange);

        // Draw aim direction
        if (target != null)
        {
            Gizmos.color = isShooting ? Color.red : Color.yellow;
            Gizmos.DrawLine(gunPoint.position, target.position);
        }

        // Draw gun point
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(gunPoint.position, 0.1f);
    }
}