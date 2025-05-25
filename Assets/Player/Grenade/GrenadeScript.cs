using UnityEngine;

public class GrenadeScript : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
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

        // Hide the grenade mesh
        if (grenadeMesh != null)
            grenadeMesh.SetActive(false);

        // Instantiate and play explosion effects
        if (explosionParticlesPrefab != null)
        {
            GameObject particles = Instantiate(explosionParticlesPrefab, transform.position, transform.rotation);
            Destroy(particles, 5f); // Clean up particles after 5 seconds
        }

        if (explosionSoundPrefab != null)
        {
            GameObject sound = Instantiate(explosionSoundPrefab, transform.position, transform.rotation);
            AudioSource audioSource = sound.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
                Destroy(sound, audioSource.clip.length + 0.5f); // Destroy after clip finishes
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

        // Apply explosion force to nearby rigidbodies
        ApplyExplosionForce();

        // Destroy the grenade after sound finishes
        Destroy(gameObject, destroyDelay);
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
}