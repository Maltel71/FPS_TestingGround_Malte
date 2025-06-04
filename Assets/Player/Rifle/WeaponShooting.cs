using UnityEngine;
using System.Collections; // Added for coroutine
// Remove potential ambiguous reference
using Debug = UnityEngine.Debug;

public class WeaponShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float shootForce = 20f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float semiAutoFireRate = 0.25f;
    [SerializeField] private float fullAutoFireRate = 0.1f;
    [SerializeField] private KeyCode toggleModeKey = KeyCode.T;
    [SerializeField] private LayerMask shootableLayers;

    [Header("Ammo Settings")]
    [SerializeField] private int maxAmmoPerMag = 30;
    [SerializeField] private float reloadTime = 3f;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("Accuracy Settings")]
    [SerializeField] private float maxInaccuracy = 5f;
    [SerializeField] private float inaccuracyPerShot = 0.5f;
    [SerializeField] private float accuracyRecoveryDelay = 1f;
    [SerializeField] private float accuracyRecoverySpeed = 2f;

    [Header("Recoil Settings")]
    [SerializeField] private Transform recoilTarget;
    [SerializeField] private float recoilAmount = 10f;
    [SerializeField] private float recoilSpeed = 15f;
    [SerializeField] private float returnSpeed = 8f;

    [Header("References")]
    [SerializeField] private Transform barrelTip;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem smokeEffect;
    [SerializeField] private ParticleSystem bulletCasings; // Added bullet casing particle system
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private Camera playerCamera;

    [Header("Audio Settings")]
    [SerializeField] private float pitchVariation = 0.1f;

    [Header("Muzzle Flash Light")]
    [SerializeField] private Light muzzleFlashLight;
    [SerializeField] private float lightDuration = 0.1f;

    [Header("Impact VFX")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private float impactVFXLifetime = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private float debugRayDuration = 1f;
    [SerializeField] private Color debugRayColor = Color.red;

    private float nextFireTime = 0f;
    private Vector3 currentRecoil;
    private Vector3 targetRecoil;
    private bool isFullAuto = false;
    private float currentInaccuracy = 0f;
    private float lastShotTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;

    private void Awake()
    {
        // Initialize ammo
        currentAmmo = maxAmmoPerMag;

        // Auto-find camera if not assigned
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Auto-find audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Create audio source if none exists
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Make sound 3D
        }

        // Ensure muzzle flash light starts disabled
        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.enabled = false;
        }
    }

    private void Update()
    {
        // Handle reload input
        if (Input.GetKeyDown(reloadKey) && !isReloading && currentAmmo < maxAmmoPerMag)
        {
            StartCoroutine(ReloadCoroutine());
        }

        // Don't allow shooting while reloading
        if (isReloading) return;

        // Toggle firing mode
        if (Input.GetKeyDown(toggleModeKey))
        {
            isFullAuto = !isFullAuto;
        }

        // Handle shooting input based on firing mode
        bool shouldShoot = isFullAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        float currentFireRate = isFullAuto ? fullAutoFireRate : semiAutoFireRate;

        if (shouldShoot && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                nextFireTime = Time.time + currentFireRate;
            }
            else
            {
                // Play empty sound
                if (audioSource != null && emptySound != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
            }
        }

        // Handle accuracy recovery
        if (Time.time - lastShotTime > accuracyRecoveryDelay)
        {
            currentInaccuracy = Mathf.MoveTowards(currentInaccuracy, 0f, accuracyRecoverySpeed * Time.deltaTime);
        }

        // Handle recoil
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRecoil = Vector3.Slerp(currentRecoil, targetRecoil, recoilSpeed * Time.deltaTime);

        // Apply recoil to assigned target
        if (recoilTarget != null)
        {
            recoilTarget.localRotation = Quaternion.Euler(currentRecoil);
        }

        // Show constant debug ray in Scene view
        if (showDebugRay && playerCamera != null)
        {
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * range, debugRayColor);
        }
    }

    private void Shoot()
    {
        // Consume ammo
        currentAmmo--;

        // Update shot time and increase inaccuracy
        lastShotTime = Time.time;
        currentInaccuracy = Mathf.Min(currentInaccuracy + inaccuracyPerShot, maxInaccuracy);

        // Add recoil
        targetRecoil += new Vector3(-recoilAmount, Random.Range(-recoilAmount * 0.3f, recoilAmount * 0.3f), 0);

        // Play muzzle flash effect
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Play smoke effect
        if (smokeEffect != null)
        {
            smokeEffect.Play();
        }

        // Play bullet casing effect
        if (bulletCasings != null)
        {
            bulletCasings.Play();
        }

        // Play shoot sound with pitch variation
        if (audioSource != null && shootSound != null)
        {
            // Apply random pitch variation
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(shootSound);
        }

        // Trigger muzzle flash light
        if (muzzleFlashLight != null)
        {
            StartCoroutine(MuzzleFlashLightCoroutine());
        }

        // Calculate ray direction with accuracy
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        // Apply inaccuracy
        if (currentInaccuracy > 0f)
        {
            Vector3 spread = new Vector3(
                Random.Range(-currentInaccuracy, currentInaccuracy),
                Random.Range(-currentInaccuracy, currentInaccuracy),
                0f
            );
            rayDirection = (rayDirection + playerCamera.transform.TransformDirection(spread * 0.01f)).normalized;
        }

        // Create a ray
        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;

        // Show debug ray when shooting
        if (showDebugRay)
        {
            Debug.DrawRay(rayOrigin, rayDirection * range, debugRayColor, debugRayDuration);
        }

        // Perform raycast
        if (Physics.Raycast(ray, out hit, range, shootableLayers))
        {
            // Check for BreakableCube and apply damage
            BreakableCube breakable = hit.collider.GetComponent<BreakableCube>();
            if (breakable != null)
            {
                breakable.TakeDamage(breakable.damagePerShot);
            }

            // Check if hit object has a rigidbody to apply force
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                // Calculate force direction from gun to hit point
                Vector3 forceDirection = hit.point - barrelTip.position;

                // Apply force at hit point
                rb.AddForceAtPosition(forceDirection.normalized * shootForce, hit.point, ForceMode.Impulse);
            }

            // Spawn impact VFX
            if (impactVFXPrefab != null)
            {
                GameObject impactVFX = Instantiate(impactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactVFX, impactVFXLifetime);
            }
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        // Play reload sound
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        // Wait for reload time
        yield return new WaitForSeconds(reloadTime);

        // Refill ammo
        currentAmmo = maxAmmoPerMag;
        isReloading = false;
    }

    private IEnumerator MuzzleFlashLightCoroutine()
    {
        // Enable the light
        muzzleFlashLight.enabled = true;

        // Wait for the specified duration
        yield return new WaitForSeconds(lightDuration);

        // Disable the light
        muzzleFlashLight.enabled = false;
    }

    // Public methods for UI or other systems to access ammo info
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmoPerMag;
    public bool IsReloading() => isReloading;
}