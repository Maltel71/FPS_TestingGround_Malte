using UnityEngine;

public class EnemyCharacterController : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;
    public float stoppingDistance = 0.5f;

    [Header("Physics")]
    public float maxSpeed = 5f;
    public float rotationSpeed = 5f;

    private Rigidbody rb;
    private int currentWaypointIndex = 0;
    private Vector3 targetPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (waypoints.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No waypoints assigned to " + gameObject.name);
            enabled = false;
            return;
        }

        // Freeze rotation to prevent tipping over
        rb.freezeRotation = true;

        SetNextWaypoint();
    }

    void FixedUpdate()
    {
        MoveTowardsTarget();
        CheckWaypointReached();
    }

    void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep movement horizontal only

        Vector3 targetVelocity = direction * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity

        // Limit max speed
        if (targetVelocity.magnitude > maxSpeed)
            targetVelocity = targetVelocity.normalized * maxSpeed;

        // Smooth velocity change
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);

        // Smooth rotation to face movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    void CheckWaypointReached()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget <= stoppingDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            SetNextWaypoint();
        }
    }

    void SetNextWaypoint()
    {
        if (waypoints.Length > 0)
        {
            targetPosition = waypoints[currentWaypointIndex].position;
        }
    }

    // Optional: Draw waypoint path in Scene view
    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // Draw waypoint
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

            // Draw path
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // Draw line back to first waypoint
        if (waypoints.Length > 2 && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }

        // Draw current target
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, stoppingDistance);
    }
}