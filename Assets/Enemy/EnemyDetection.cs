using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    public Collider detectionCollider; // Manually assign this in inspector
    public LayerMask playerLayerMask = 1; // Player layer
    public string playerTag = "Player";

    [Header("References")]
    public EnemyCharacterController enemyController;
    public EnemyShooting enemyShooting;

    private Transform detectedPlayer;
    private bool playerInRange = false;

    void Start()
    {
        // Check if detection collider is assigned
        if (detectionCollider == null)
        {
            Debug.LogError("Detection Collider not assigned on " + gameObject.name + "! Please assign a collider in the inspector.");
            enabled = false;
            return;
        }

        // Ensure it's set as trigger
        detectionCollider.isTrigger = true;

        // Auto-find components if not assigned
        if (enemyController == null)
            enemyController = GetComponent<EnemyCharacterController>();
        if (enemyShooting == null)
            enemyShooting = GetComponentInChildren<EnemyShooting>();
    }

    void Update()
    {
        if (playerInRange && detectedPlayer != null)
        {
            // Face the player
            FacePlayer();

            // Enable shooting
            if (enemyShooting != null)
            {
                enemyShooting.SetTarget(detectedPlayer);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Make sure the trigger event is from our detection collider
        if (other.CompareTag(playerTag) && IsInLayerMask(other.gameObject, playerLayerMask))
        {
            detectedPlayer = other.transform;
            playerInRange = true;

            // Disable waypoint movement
            if (enemyController != null)
                enemyController.enabled = false;

            Debug.Log("Player detected by " + gameObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            detectedPlayer = null;
            playerInRange = false;

            // Re-enable waypoint movement
            if (enemyController != null)
                enemyController.enabled = true;

            // Stop shooting
            if (enemyShooting != null)
            {
                enemyShooting.SetTarget(null);
            }

            Debug.Log("Player left detection range of " + gameObject.name);
        }
    }

    void FacePlayer()
    {
        Vector3 direction = (detectedPlayer.position - transform.position).normalized;
        direction.y = 0; // Keep rotation horizontal only

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) > 0;
    }

    // Gizmos for visualization - now shows the bounds of the assigned collider
    void OnDrawGizmosSelected()
    {
        if (detectionCollider != null)
        {
            Gizmos.color = Color.red;

            // Draw different shapes based on collider type
            if (detectionCollider is SphereCollider sphere)
            {
                Gizmos.matrix = Matrix4x4.TRS(detectionCollider.transform.position, detectionCollider.transform.rotation, detectionCollider.transform.lossyScale);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (detectionCollider is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(detectionCollider.transform.position, detectionCollider.transform.rotation, detectionCollider.transform.lossyScale);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (detectionCollider is CapsuleCollider capsule)
            {
                Gizmos.matrix = Matrix4x4.TRS(detectionCollider.transform.position, detectionCollider.transform.rotation, detectionCollider.transform.lossyScale);
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        if (playerInRange && detectedPlayer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, detectedPlayer.position);
        }
    }
}