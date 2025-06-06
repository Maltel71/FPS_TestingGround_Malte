using Unity.Netcode;
using UnityEngine;

public class NetworkedGrenadeScript : NetworkBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    public float explosionDamage = 50f; // New damage parameter
    public float fuseTime = 3f;
    public float lightDuration = 0.5f;
    public float destroyDelay = 2f;

    [Header("Effect Prefabs")]
    public GameObject explosionParticlesPrefab;
    public GameObject explosionSoundPrefab;
    public GameObject explosionLightPrefab;

    [Header("Grenade Parts")]
    public GameObject grenadeMesh;

    private bool hasExploded = false;

    void Start()
    {
        // Start the fuse timer
        Invoke(nameof(Explode), fuseTime);
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Only the server should handle explosion logic
        if (IsServer)
        {
            ExplodeServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ExplodeServerRpc()
    {
        // Hide the grenade mesh on all clients
        ExplodeClientRpc();

        // Apply explosion effects (server-side)
        ApplyExplosionEffects();

        // Destroy the grenade after delay
        Destroy(gameObject, destroyDelay);
    }

    [ClientRpc]
    void ExplodeClientRpc()
    {
        // Hide the grenade mesh
        if (grenadeMesh != null)
            grenadeMesh.SetActive(false);

        // Instantiate and play explosion effects
        if (explosionParticlesPrefab != null)
        {
            GameObject particles = Instantiate(explosionParticlesPrefab, transform.position, transform.rotation);
            Destroy(particles, 5f);
        }

        if (explosionSoundPrefab != null)
        {
            GameObject sound = Instantiate(explosionSoundPrefab, transform.position, transform.rotation);
            AudioSource audioSource = sound.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
                Destroy(sound, audioSource.clip.length + 0.5f);
            }
        }

        if (explosionLightPrefab != null)
        {
            GameObject lightObj = Instantiate(explosionLightPrefab, transform.position, transform.rotation);
            Light light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.enabled = true;
                Destroy(lightObj, lightDuration);
            }
        }
    }

    void ApplyExplosionEffects()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider col in colliders)
        {
            // Apply physics force
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null && rb != GetComponent<Rigidbody>())
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            // Apply damage to players
            PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Calculate damage based on distance (closer = more damage)
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageMultiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                float finalDamage = explosionDamage * damageMultiplier;

                playerHealth.TakeDamageServerRpc(finalDamage);
            }

            // Apply damage to other objects (like BreakableCube)
            BreakableCube breakable = col.GetComponent<BreakableCube>();
            if (breakable != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageMultiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                float finalDamage = explosionDamage * damageMultiplier;

                breakable.TakeDamage(finalDamage);
            }
        }
    }
}