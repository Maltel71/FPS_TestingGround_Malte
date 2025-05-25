using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float rotationSpeed = 200f;

    [Header("Random Movement")]
    [Tooltip("How much the missile wobbles (0 = no wobble, higher = more wobble)")]
    public float wobbleStrength = 2f;
    [Tooltip("How fast the wobble changes (lower = smoother, higher = more erratic)")]
    public float wobbleFrequency = 3f;

    [Header("Target Settings")]
    [SerializeField] private Transform target; // Set by spawner, not in inspector

    [Header("Destruction Settings")]
    public float lifetime = 10f;
    public float explosionRadius = 3f;
    public float explosionForce = 500f;
    public bool destroyOnContact = true;

    [Header("Particle Effects")]
    public GameObject trailParticlesPrefab;
    public GameObject explosionParticlesPrefab;
    public bool attachTrailToMissile = true;

    [Header("Audio Settings")]
    public AudioSource thrusterAudioSource;
    public AudioSource explosionAudioSource;

    private Rigidbody rb;
    private float spawnTime;
    private GameObject activeTrailEffect;
    private bool hasExploded = false; // Prevent multiple explosions
    private Vector3 randomOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        // If no rigidbody is attached, add one
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Disable gravity for the missile
        rb.useGravity = false;

        // Start thruster sound if assigned
        if (thrusterAudioSource != null)
        {
            thrusterAudioSource.Play();
        }

        // Spawn trail particles if assigned
        if (trailParticlesPrefab != null)
        {
            if (attachTrailToMissile)
            {
                // Attach trail particles to the missile
                activeTrailEffect = Instantiate(trailParticlesPrefab, transform);
            }
            else
            {
                // Spawn trail particles at missile position but not attached
                activeTrailEffect = Instantiate(trailParticlesPrefab, transform.position, transform.rotation);
            }
        }
    }

    void FixedUpdate()
    {
        // Check if missile should be destroyed due to lifetime
        if (Time.time - spawnTime > lifetime)
        {
            DestroyMissile();
            return;
        }

        // Update random movement offset
        UpdateRandomMovement();

        // Move and rotate towards target
        if (target != null)
        {
            HomingMovement();
        }
        else
        {
            // Move forward if no target
            rb.linearVelocity = transform.forward * speed;
        }
    }

    void UpdateRandomMovement()
    {
        float time = Time.time * wobbleFrequency;
        randomOffset = new Vector3(
            Mathf.PerlinNoise(time, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, time) - 0.5f,
            Mathf.PerlinNoise(time, time) - 0.5f
        ) * wobbleStrength;
    }

    void HomingMovement()
    {
        // Calculate direction to target with random offset
        Vector3 targetPosition = target.position + randomOffset;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Calculate rotation needed
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Smoothly rotate towards target
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        // Move forward
        rb.linearVelocity = transform.forward * speed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void OnTriggerEnter(Collider other)
    {
        if (destroyOnContact && !hasExploded)
        {
            // Check if we hit the target or another object
            if (other.transform == target || other.CompareTag("Player") || other.CompareTag("Enemy"))
            {
                // You can add explosion effects or damage here
                Debug.Log("Missile hit: " + other.name);
                DestroyMissile();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (destroyOnContact && !hasExploded)
        {
            // Handle collision-based destruction
            Debug.Log("Missile collided with: " + collision.gameObject.name);
            DestroyMissile();
        }
    }

    void ApplyExplosionForce()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider col in colliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null && rb != GetComponent<Rigidbody>())
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
    }

    void DestroyMissile()
    {
        // Prevent multiple explosions
        if (hasExploded) return;
        hasExploded = true;

        // Apply explosion force to nearby rigidbodies
        ApplyExplosionForce();

        // Disable missile mesh immediately
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Also disable any child mesh renderers
        MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in childRenderers)
        {
            renderer.enabled = false;
        }

        // Stop thruster sound
        if (thrusterAudioSource != null && thrusterAudioSource.isPlaying)
        {
            thrusterAudioSource.Stop();
        }

        // Play explosion sound if assigned
        if (explosionAudioSource != null)
        {
            explosionAudioSource.Play();
        }

        // Handle trail particles cleanup - stop immediately
        if (activeTrailEffect != null)
        {
            ParticleSystem trailPS = activeTrailEffect.GetComponent<ParticleSystem>();
            if (trailPS != null)
            {
                trailPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // Also stop any trail renderer
            TrailRenderer trailRenderer = activeTrailEffect.GetComponent<TrailRenderer>();
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }
        }

        // Stop any trail renderers on the missile itself
        TrailRenderer[] missileTrails = GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trail in missileTrails)
        {
            trail.enabled = false;
        }

        // Spawn explosion particles if assigned
        if (explosionParticlesPrefab != null)
        {
            GameObject explosion = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);

            // Auto-destroy explosion effect after a delay (optional)
            ParticleSystem explosionPS = explosion.GetComponent<ParticleSystem>();
            if (explosionPS != null)
            {
                Destroy(explosion, explosionPS.main.duration + explosionPS.main.startLifetime.constantMax);
            }
            else
            {
                // Fallback: destroy after 5 seconds if no particle system found
                Destroy(explosion, 5f);
            }
        }

        // Handle trail particles cleanup
        if (activeTrailEffect != null)
        {
            if (attachTrailToMissile)
            {
                // If attached, it will be destroyed with the missile
                // But stop emission first for a cleaner effect
                ParticleSystem trailPS = activeTrailEffect.GetComponent<ParticleSystem>();
                if (trailPS != null)
                {
                    var emission = trailPS.emission;
                    emission.enabled = false;
                }
            }
            else
            {
                // If not attached, stop emission and let it fade out naturally
                ParticleSystem trailPS = activeTrailEffect.GetComponent<ParticleSystem>();
                if (trailPS != null)
                {
                    var emission = trailPS.emission;
                    emission.enabled = false;
                    Destroy(activeTrailEffect, trailPS.main.startLifetime.constantMax);
                }
            }
        }

        // Delay destruction to allow explosion sound to play
        if (explosionAudioSource != null && explosionAudioSource.clip != null)
        {
            // Destroy after the explosion sound finishes
            Destroy(gameObject, explosionAudioSource.clip.length);
        }
        else
        {
            // Immediate destruction if no explosion sound
            Destroy(gameObject);
        }
    }

    // Optional: Visualize explosion radius in scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}