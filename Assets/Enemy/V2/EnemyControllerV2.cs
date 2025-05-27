using UnityEngine;

public class EnemyControllerV2 : MonoBehaviour
{
    [Header("Waypoint Movement")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;
    public float stoppingDistance = 0.5f;

    [Header("Player Detection & Combat")]
    public float detectionRange = 10f;
    public float fireRate = 1f;
    public GameObject homingMissilePrefab;
    public Transform firePoint;

    [Header("Physics")]
    public float rotationSpeed = 5f;

    private Rigidbody rb;
    private Transform player;
    private int currentWaypointIndex = 0;
    private Vector3 targetPosition;
    private float nextFireTime = 0f;
    private bool playerDetected = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (firePoint == null)
            firePoint = transform;

        SetNextWaypoint();
    }

    void Update()
    {
        CheckPlayerDistance();

        if (playerDetected)
        {
            FacePlayer();
            TryShootMissile();
        }
    }

    void FixedUpdate()
    {
        if (!playerDetected)
            MoveToWaypoint();
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasDetected = playerDetected;
        playerDetected = distance <= detectionRange;

        // Debug logging
        if (playerDetected && !wasDetected)
            Debug.Log($"{gameObject.name}: Player detected at distance {distance:F1}");
        else if (!playerDetected && wasDetected)
            Debug.Log($"{gameObject.name}: Player lost at distance {distance:F1}");
    }

    void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void TryShootMissile()
    {
        if (Time.time >= nextFireTime && homingMissilePrefab != null)
        {
            // Spawn missile completely independent of enemy
            GameObject missile = Instantiate(homingMissilePrefab, firePoint.position, firePoint.rotation);

            // Ensure missile is not parented to enemy
            missile.transform.parent = null;

            // Set missile target
            HomingMissileV2 homingScript = missile.GetComponent<HomingMissileV2>();
            if (homingScript != null)
                homingScript.SetTarget(player);

            nextFireTime = Time.time + (1f / fireRate);

            Debug.Log($"{gameObject.name}: Fired missile at {Time.time}");
        }
    }

    void MoveToWaypoint()
    {
        if (waypoints.Length == 0) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);

        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }

        // Check if reached waypoint
        if (Vector3.Distance(transform.position, targetPosition) <= stoppingDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            SetNextWaypoint();
        }
    }

    void SetNextWaypoint()
    {
        if (waypoints.Length > 0)
            targetPosition = waypoints[currentWaypointIndex].position;
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw waypoint path
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

                if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            // Connect last to first
            if (waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }

        // Draw line to player when detected
        if (playerDetected && player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}