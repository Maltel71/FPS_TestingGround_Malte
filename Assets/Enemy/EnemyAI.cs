using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float sightRange = 15f;
    public float attackRange = 10f;
    public float fieldOfViewAngle = 120f;
    public float lostPlayerTimeout = 3f; // How long to chase after losing sight
    public LayerMask playerLayer = -1;
    public LayerMask groundLayer = -1;

    [Header("Movement")]
    public float walkPointRange = 10f;
    public float rotationSpeed = 180f; // Degrees per second when tracking player
    public NavMeshAgent navAgent;

    [Header("Combat")]
    public int burstSize = 3;
    public float timeBetweenShots = 0.3f;
    public float timeBetweenAttacks = 2f;
    public float bulletDamage = 15f;

    [Header("Accuracy Settings")]
    public float baseInaccuracy = 2f;
    public float maxInaccuracy = 8f;
    public float inaccuracyPerShot = 1f;
    public float accuracyRecoverySpeed = 3f;

    [Header("References")]
    public Transform gunBarrel;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip shootSound;

    [Header("Animation Settings")]
    public Animator animator; // Drag the child model's animator here
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string isAttackingParameterName = "IsAttacking";
    [SerializeField] private string isDeadParameterName = "IsDead";
    [SerializeField] private string deathTriggerParameterName = "Death"; // Trigger for death animation

    [Header("Debug")]
    public bool showDebug = true;

    [Header("Current Status (Runtime Info)")]
    [SerializeField] private string currentStateDisplay = "Patrol";
    [SerializeField] private float distanceToPlayer = 0f;
    [SerializeField] private bool playerInSight = false;
    [SerializeField] private float timeSinceLastSeen = 0f;
    [SerializeField] private bool isAttacking = false;

    // Private variables
    private Transform player;
    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool alreadyAttacked;
    private bool takeDamage;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 damageSourcePosition; // New: where damage came from
    private float lastSeenPlayerTime;
    private float currentInaccuracy = 0f;
    private float lastShotTime = 0f;
    private bool hasEverSeenPlayer = false; // New: track if we've ever seen the player

    // Animation parameter hashes for performance
    private int speedHash;
    private int isAttackingHash;
    private int isDeadHash;
    private int deathTriggerHash;
    private bool hasTriggeredDeath = false; // Prevent multiple death triggers

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Auto-find animator if not assigned
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Initialize animation parameters
        InitializeAnimationParameters();

        if (showDebug)
            Debug.Log("Enemy AI initialized with NavMesh and Animation support");
    }

    void InitializeAnimationParameters()
    {
        if (animator == null)
        {
            Debug.LogWarning($"No Animator found on {gameObject.name} or its children. Animation features disabled.");
            return;
        }

        // Cache parameter hashes for better performance
        speedHash = Animator.StringToHash(speedParameterName);
        isAttackingHash = Animator.StringToHash(isAttackingParameterName);
        isDeadHash = Animator.StringToHash(isDeadParameterName);
        deathTriggerHash = Animator.StringToHash(deathTriggerParameterName);

        // Validate that parameters exist
        ValidateAnimatorParameters();
    }

    void ValidateAnimatorParameters()
    {
        if (animator == null) return;

        bool hasSpeedParam = HasParameter(speedParameterName);
        bool hasAttackingParam = HasParameter(isAttackingParameterName);
        bool hasDeadParam = HasParameter(isDeadParameterName);
        bool hasDeathTriggerParam = HasParameter(deathTriggerParameterName);

        if (!hasSpeedParam)
            Debug.LogWarning($"Parameter '{speedParameterName}' not found in animator controller!");
        if (!hasAttackingParam)
            Debug.LogWarning($"Parameter '{isAttackingParameterName}' not found in animator controller!");
        if (!hasDeadParam)
            Debug.LogWarning($"Parameter '{isDeadParameterName}' not found in animator controller!");
        if (!hasDeathTriggerParam)
            Debug.LogWarning($"Parameter '{deathTriggerParameterName}' not found in animator controller!");
    }

    bool HasParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    void Update()
    {
        if (player == null) return;

        // Don't update AI logic if dead
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.IsDead())
        {
            UpdateAnimations(); // Still update animations for death
            return;
        }

        // Check player detection
        bool playerInSightRange = CanSeePlayer();
        float distanceToPlayerActual = Vector3.Distance(transform.position, player.position);
        bool playerInAttackRange = playerInSightRange && distanceToPlayerActual < attackRange;
        bool hasLostPlayer = !playerInSightRange && !takeDamage && (Time.time - lastSeenPlayerTime > lostPlayerTimeout);

        // Update Inspector status display
        UpdateInspectorInfo(playerInSightRange);

        // Update accuracy recovery
        if (!alreadyAttacked && Time.time - lastShotTime > 1f)
        {
            currentInaccuracy = Mathf.MoveTowards(currentInaccuracy, 0f, accuracyRecoverySpeed * Time.deltaTime);
        }

        // State logic with proper lost player handling
        if (hasLostPlayer)
        {
            // Lost player completely - return to patrol
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Lost player completely - returning to patrol");
            Patroling();
        }
        else if (!playerInSightRange && !takeDamage && lastSeenPlayerTime == 0f)
        {
            // Never detected player - normal patrol
            Patroling();
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            // Can see player but too far - chase
            ChasePlayer();
        }
        else if (playerInAttackRange)
        {
            // Close enough and can see - attack
            AttackPlayer();
        }
        else if (!playerInSightRange && (takeDamage || Time.time - lastSeenPlayerTime <= lostPlayerTimeout))
        {
            // Lost sight but recently saw player - go to last known position
            SearchLastKnownPosition();
        }
        else
        {
            // Fallback - should not happen but safety net
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Fallback state - going to patrol");
            Patroling();
        }

        // Update animations
        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Check for death first - this overrides all other animations
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.IsDead())
        {
            HandleDeathAnimation();
            return; // Don't update other animations if dead
        }

        // Update movement speed
        float currentSpeed = navAgent.velocity.magnitude;
        if (HasParameter(speedParameterName))
        {
            animator.SetFloat(speedHash, currentSpeed);
        }

        // Update attacking state
        if (HasParameter(isAttackingParameterName))
        {
            animator.SetBool(isAttackingHash, isAttacking);
        }
    }

    void HandleDeathAnimation()
    {
        // Trigger death animation only once
        if (!hasTriggeredDeath)
        {
            hasTriggeredDeath = true;

            // Trigger the death animation
            if (HasParameter(deathTriggerParameterName))
            {
                animator.SetTrigger(deathTriggerHash);
            }

            // Set dead state
            if (HasParameter(isDeadParameterName))
            {
                animator.SetBool(isDeadHash, true);
            }

            // Stop all movement
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }

            // Set speed to 0
            if (HasParameter(speedParameterName))
            {
                animator.SetFloat(speedHash, 0f);
            }

            // Stop attacking
            if (HasParameter(isAttackingParameterName))
            {
                animator.SetBool(isAttackingHash, false);
            }

            Debug.Log($"{gameObject.name} triggered death animation");
        }
    }

    bool CanSeePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > sightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfViewAngle / 2f) return false;

        // Line of sight check
        Vector3 rayStart = transform.position + Vector3.up * 1.5f;
        Vector3 rayEnd = player.position + Vector3.up * 1f;
        Vector3 rayDirection = (rayEnd - rayStart).normalized;
        float rayDistance = Vector3.Distance(rayStart, rayEnd);

        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance))
        {
            if (hit.collider.CompareTag("Player"))
            {
                lastSeenPlayerTime = Time.time;
                lastKnownPlayerPosition = player.position;
                hasEverSeenPlayer = true; // Mark that we've seen the player
                return true;
            }
        }
        else
        {
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = player.position;
            hasEverSeenPlayer = true; // Mark that we've seen the player
            return true;
        }

        return false;
    }

    void Patroling()
    {
        if (!walkPointSet)
            SearchWalkPoint();

        if (walkPointSet)
            navAgent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 2f)
            walkPointSet = false;
    }

    void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer))
            walkPointSet = true;
    }

    void ChasePlayer()
    {
        navAgent.SetDestination(player.position);
        navAgent.isStopped = false;

        if (showDebug && Time.frameCount % 60 == 0)
            Debug.Log("Chasing player");
    }

    void SearchLastKnownPosition()
    {
        Vector3 searchTarget;

        // Decide where to search based on available information
        if (hasEverSeenPlayer && lastKnownPlayerPosition != Vector3.zero)
        {
            // Prefer visual detection position if we've actually seen the player
            searchTarget = lastKnownPlayerPosition;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log($"Searching last SEEN position - {Time.time - lastSeenPlayerTime:F1}s ago");
        }
        else if (damageSourcePosition != Vector3.zero)
        {
            // Use damage source if we've never seen the player but took damage
            searchTarget = damageSourcePosition;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Searching damage source position (shot from behind)");
        }
        else
        {
            // Fallback to current player position
            searchTarget = player.position;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Searching current player position (fallback)");
        }

        navAgent.SetDestination(searchTarget);
        navAgent.isStopped = false;
    }

    void AttackPlayer()
    {
        navAgent.SetDestination(transform.position);
        navAgent.isStopped = true;

        // Continuously rotate to face player during attack
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // Keep rotation horizontal only

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (!alreadyAttacked)
        {
            StartCoroutine(ShootBurst());
        }
    }

    IEnumerator ShootBurst()
    {
        alreadyAttacked = true;
        isAttacking = true;

        for (int i = 0; i < burstSize; i++)
        {
            FireShot();
            yield return new WaitForSeconds(timeBetweenShots);
        }

        yield return new WaitForSeconds(timeBetweenAttacks);
        alreadyAttacked = false;
        isAttacking = false;
    }

    void FireShot()
    {
        if (gunBarrel == null) gunBarrel = transform;

        // Increase inaccuracy
        currentInaccuracy = Mathf.Min(currentInaccuracy + inaccuracyPerShot, maxInaccuracy);
        lastShotTime = Time.time;

        // Calculate total inaccuracy
        float distanceToPlayer = Vector3.Distance(gunBarrel.position, player.position);
        float totalInaccuracy = baseInaccuracy + currentInaccuracy + (distanceToPlayer * 0.1f);

        // Effects
        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        // Calculate shot direction with spread
        Vector3 baseDirection = (player.position + Vector3.up - gunBarrel.position).normalized;
        Vector3 shootDirection = baseDirection;

        if (totalInaccuracy > 0f)
        {
            float spreadAngle = totalInaccuracy * Mathf.Deg2Rad;
            float randomAngle = Random.Range(0f, 2f * Mathf.PI);
            float randomSpread = Random.Range(0f, spreadAngle);

            Vector3 right = Vector3.Cross(baseDirection, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, baseDirection).normalized;
            Vector3 spreadOffset = (right * Mathf.Cos(randomAngle) + up * Mathf.Sin(randomAngle)) * Mathf.Sin(randomSpread);

            shootDirection = (baseDirection + spreadOffset).normalized;
        }

        // Raycast for hit
        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, shootDirection, out hit, attackRange))
        {
            PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(bulletDamage);
                if (showDebug) Debug.Log($"Player hit for {bulletDamage} damage!");
            }

            // Physics impact
            Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
            if (hitRb != null)
                hitRb.AddForceAtPosition(shootDirection * 10f, hit.point, ForceMode.Impulse);
        }

        if (showDebug)
            Debug.DrawRay(gunBarrel.position, shootDirection * attackRange, Color.red, 1f);
    }

    public void TakeDamage(float damage)
    {
        // Store where the damage came from (player's current position)
        if (player != null)
        {
            damageSourcePosition = player.position;

            // If we've never seen the player visually, use damage position as "last known"
            if (!hasEverSeenPlayer)
            {
                lastKnownPlayerPosition = damageSourcePosition;
                lastSeenPlayerTime = Time.time; // Set this so the search logic works
            }
        }

        // Force enemy to detect and chase player when damaged
        StartCoroutine(TakeDamageCoroutine());

        if (showDebug)
        {
            if (hasEverSeenPlayer)
                Debug.Log("Enemy damaged - using last SEEN position for search");
            else
                Debug.Log("Enemy damaged - using damage source position (shot from behind)");
        }
    }

    IEnumerator TakeDamageCoroutine()
    {
        takeDamage = true;

        // If we've never seen the player, update the last seen time so search logic works
        if (!hasEverSeenPlayer)
        {
            lastSeenPlayerTime = Time.time;
        }

        yield return new WaitForSeconds(5f); // Search for 5 seconds after damage
        takeDamage = false;
    }

    void UpdateInspectorInfo(bool playerDetected)
    {
        // Update state display
        float distanceToPlayerActual = Vector3.Distance(transform.position, player.position);
        bool hasLostPlayer = !playerDetected && !takeDamage && (Time.time - lastSeenPlayerTime > lostPlayerTimeout);

        if (hasLostPlayer)
            currentStateDisplay = "Patrol";
        else if (!playerDetected && !takeDamage && lastSeenPlayerTime == 0f)
            currentStateDisplay = "Patrol";
        else if (playerDetected && distanceToPlayerActual > attackRange)
            currentStateDisplay = "Chase";
        else if (playerDetected && distanceToPlayerActual <= attackRange)
            currentStateDisplay = "Attack";
        else if (!playerDetected && (takeDamage || Time.time - lastSeenPlayerTime <= lostPlayerTimeout))
            currentStateDisplay = "Search";
        else
            currentStateDisplay = "Patrol"; // Fallback

        // Add action info
        if (isAttacking) currentStateDisplay += " (Shooting)";
        else if (navAgent.velocity.magnitude > 0.1f) currentStateDisplay += " (Moving)";
        else currentStateDisplay += " (Idle)";

        // Update player info
        if (player != null)
        {
            distanceToPlayer = distanceToPlayerActual;
            playerInSight = playerDetected;
            timeSinceLastSeen = Time.time - lastSeenPlayerTime;
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Field of view
        if (showDebug)
        {
            Gizmos.color = Color.blue;
            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * sightRange;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * sightRange;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        }
    }

    // Public methods for animation system integration
    public void ForceDetectPlayer()
    {
        takeDamage = true;
        lastSeenPlayerTime = Time.time;
    }

    public bool IsPlayerDetected()
    {
        return Time.time - lastSeenPlayerTime < 1f;
    }

    public string GetCurrentState()
    {
        return currentStateDisplay;
    }

    public bool IsCurrentlyAttacking()
    {
        return isAttacking;
    }

    public float GetMovementSpeed()
    {
        return navAgent != null ? navAgent.velocity.magnitude : 0f;
    }

    // Method to manually trigger death animation (useful for testing)
    public void TriggerDeathAnimation()
    {
        if (!hasTriggeredDeath && animator != null)
        {
            HandleDeathAnimation();
        }
    }
}