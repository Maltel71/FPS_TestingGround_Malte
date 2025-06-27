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

    [Header("Sound Detection")]
    public float footstepHearingRange = 8f; // How far enemy can hear footsteps
    public float gunshotHearingRange = 25f; // How far enemy can hear gunshots
    public float soundAlertDuration = 5f; // How long to investigate sounds
    public bool showSoundRanges = true; // Show sound ranges in gizmos

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
    [SerializeField] private bool playerHeard = false; // New: tracks if player was heard
    [SerializeField] private float timeSinceLastSeen = 0f;
    [SerializeField] private float timeSinceLastHeard = 0f; // New: tracks sound detection
    [SerializeField] private bool isAttacking = false;

    // Private variables
    private Transform player;
    private FirstPersonController playerController; // New: reference to player controller
    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool alreadyAttacked;
    private bool takeDamage;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 damageSourcePosition; // New: where damage came from
    private Vector3 lastHeardSoundPosition; // New: where sound came from
    private float lastSeenPlayerTime;
    private float lastHeardSoundTime; // New: when sound was last heard
    private float currentInaccuracy = 0f;
    private float lastShotTime = 0f;
    private bool hasEverSeenPlayer = false; // New: track if we've ever seen the player
    private bool isInvestigatingSound = false; // New: track if investigating sound

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
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<FirstPersonController>();
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Auto-find animator if not assigned
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Initialize animation parameters
        InitializeAnimationParameters();

        if (showDebug)
            Debug.Log("Enemy AI initialized with NavMesh, Animation, and Sound Detection support");
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

        // Check player detection (sight and sound)
        bool playerInSightRange = CanSeePlayer();
        bool playerInSoundRange = CanHearPlayer(); // New: sound detection
        float distanceToPlayerActual = Vector3.Distance(transform.position, player.position);
        bool playerInAttackRange = playerInSightRange && distanceToPlayerActual < attackRange;

        // Player is lost if we can't see them, can't hear them, took no damage, and enough time has passed
        bool hasLostPlayer = !playerInSightRange && !playerInSoundRange && !takeDamage &&
                            (Time.time - lastSeenPlayerTime > lostPlayerTimeout) &&
                            (Time.time - lastHeardSoundTime > soundAlertDuration);

        // Update Inspector status display
        UpdateInspectorInfo(playerInSightRange, playerInSoundRange);

        // Update accuracy recovery
        if (!alreadyAttacked && Time.time - lastShotTime > 1f)
        {
            currentInaccuracy = Mathf.MoveTowards(currentInaccuracy, 0f, accuracyRecoverySpeed * Time.deltaTime);
        }

        // State logic with proper lost player handling
        if (hasLostPlayer && !isInvestigatingSound)
        {
            // Lost player completely - return to patrol
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Lost player completely - returning to patrol");
            Patroling();
        }
        else if (!playerInSightRange && !playerInSoundRange && !takeDamage && lastSeenPlayerTime == 0f && lastHeardSoundTime == 0f)
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
        else if (!playerInSightRange && (takeDamage || playerInSoundRange ||
                 Time.time - lastSeenPlayerTime <= lostPlayerTimeout ||
                 Time.time - lastHeardSoundTime <= soundAlertDuration))
        {
            // Lost sight but recently saw/heard player or took damage - investigate
            InvestigateLastKnownPosition();
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

    // New method: Check if player can be heard
    bool CanHearPlayer()
    {
        if (player == null || playerController == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check for footstep sounds
        if (distanceToPlayer <= footstepHearingRange)
        {
            // Only hear if player is moving and not crouching
            Vector3 playerVelocity = playerController.GetComponent<CharacterController>().velocity;
            float playerSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;

            // Get crouch state - we'll need to add a public getter to FirstPersonController
            bool isPlayerCrouching = IsPlayerCrouching();

            if (playerSpeed > 0.5f && !isPlayerCrouching) // Player is moving and not crouching
            {
                lastHeardSoundTime = Time.time;
                lastHeardSoundPosition = player.position;

                if (showDebug && Time.frameCount % 60 == 0)
                    Debug.Log($"Enemy heard player footsteps at distance {distanceToPlayer:F1}m");

                return true;
            }
        }

        return false;
    }

    // Helper method to check if player is crouching
    // Note: You'll need to add a public getter to FirstPersonController for this
    bool IsPlayerCrouching()
    {
        // This is a simple approach - you might need to modify FirstPersonController
        // to expose the isCrouching variable publicly
        if (playerController != null)
        {
            // For now, we'll use a simple height check as approximation
            CharacterController playerCC = playerController.GetComponent<CharacterController>();
            if (playerCC != null)
            {
                // Assume normal height is around 2, crouch height is around 1.2
                return playerCC.height < 1.5f;
            }
        }
        return false;
    }

    // New method: Handle gunshot sounds (called from WeaponShooting)
    public void OnGunshotHeard(Vector3 shotPosition)
    {
        float distanceToShot = Vector3.Distance(transform.position, shotPosition);

        if (distanceToShot <= gunshotHearingRange)
        {
            lastHeardSoundTime = Time.time;
            lastHeardSoundPosition = shotPosition;

            // Gunshots are more urgent than footsteps
            if (!hasEverSeenPlayer)
            {
                lastKnownPlayerPosition = shotPosition;
                lastSeenPlayerTime = Time.time; // Treat gunshot as "seeing" for search logic
            }

            if (showDebug)
                Debug.Log($"Enemy heard gunshot at distance {distanceToShot:F1}m");
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
        isInvestigatingSound = false;

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
        isInvestigatingSound = false;

        if (showDebug && Time.frameCount % 60 == 0)
            Debug.Log("Chasing player");
    }

    void InvestigateLastKnownPosition()
    {
        Vector3 searchTarget;
        isInvestigatingSound = true;

        // Prioritize the most recent information
        if (hasEverSeenPlayer && lastKnownPlayerPosition != Vector3.zero &&
            (lastSeenPlayerTime > lastHeardSoundTime || lastHeardSoundPosition == Vector3.zero))
        {
            // Prefer visual detection position if we've seen the player recently
            searchTarget = lastKnownPlayerPosition;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log($"Investigating last SEEN position - {Time.time - lastSeenPlayerTime:F1}s ago");
        }
        else if (lastHeardSoundPosition != Vector3.zero && Time.time - lastHeardSoundTime <= soundAlertDuration)
        {
            // Use sound position if we heard something recently
            searchTarget = lastHeardSoundPosition;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log($"Investigating sound position - {Time.time - lastHeardSoundTime:F1}s ago");
        }
        else if (damageSourcePosition != Vector3.zero)
        {
            // Use damage source if we've been shot but can't see the player
            searchTarget = damageSourcePosition;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Investigating damage source position");
        }
        else
        {
            // Fallback to current player position
            searchTarget = player.position;
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.Log("Investigating current player position (fallback)");
        }

        navAgent.SetDestination(searchTarget);
        navAgent.isStopped = false;
    }

    void AttackPlayer()
    {
        navAgent.SetDestination(transform.position);
        navAgent.isStopped = true;
        isInvestigatingSound = false;

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

    void UpdateInspectorInfo(bool playerDetected, bool playerHeardNow)
    {
        // Update state display
        float distanceToPlayerActual = Vector3.Distance(transform.position, player.position);
        bool hasLostPlayer = !playerDetected && !playerHeardNow && !takeDamage &&
                            (Time.time - lastSeenPlayerTime > lostPlayerTimeout) &&
                            (Time.time - lastHeardSoundTime > soundAlertDuration);

        if (hasLostPlayer && !isInvestigatingSound)
            currentStateDisplay = "Patrol";
        else if (!playerDetected && !playerHeardNow && !takeDamage && lastSeenPlayerTime == 0f && lastHeardSoundTime == 0f)
            currentStateDisplay = "Patrol";
        else if (playerDetected && distanceToPlayerActual > attackRange)
            currentStateDisplay = "Chase";
        else if (playerDetected && distanceToPlayerActual <= attackRange)
            currentStateDisplay = "Attack";
        else if (!playerDetected && (takeDamage || playerHeardNow ||
                 Time.time - lastSeenPlayerTime <= lostPlayerTimeout ||
                 Time.time - lastHeardSoundTime <= soundAlertDuration))
            currentStateDisplay = "Investigate";
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
            playerHeard = playerHeardNow;
            timeSinceLastSeen = Time.time - lastSeenPlayerTime;
            timeSinceLastHeard = Time.time - lastHeardSoundTime;
        }
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

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Sound detection ranges
        if (showSoundRanges)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, footstepHearingRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, gunshotHearingRange);
        }

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
        return Time.time - lastSeenPlayerTime < 1f || Time.time - lastHeardSoundTime < 1f;
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