using UnityEngine;
using System.Collections;
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
    [SerializeField] private ParticleSystem bulletCasings;
    [SerializeField] private Camera playerCamera;

    [Header("Audio Sources & Clips")]
    [SerializeField] private AudioSource shootAudioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioSource emptyAudioSource;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioSource reloadAudioSource;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioSource fireModeAudioSource;
    [SerializeField] private AudioClip fireModeSound;
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

    [Header("References")]
    [SerializeField] private PauseMenuManager pauseMenuManager;
    [SerializeField] private WeaponController weaponController; // Reference to check if weapons enabled

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
        currentAmmo = maxAmmoPerMag;

        if (playerCamera == null)
            playerCamera = Camera.main;

        // Find weapon controller if not assigned
        if (weaponController == null)
            weaponController = FindObjectOfType<WeaponController>();

        // Find pause menu manager if not assigned
        if (pauseMenuManager == null)
            pauseMenuManager = FindObjectOfType<PauseMenuManager>();

        if (muzzleFlashLight != null)
            muzzleFlashLight.enabled = false;
    }

    private void Update()
    {
        // Check if options menu is open - if so, skip all weapon input
        if (pauseMenuManager != null && pauseMenuManager.IsOptionsMenuOpen())
            return;

        // Check if this weapon component is active (weapons disabled when carrying)
        if (!gameObject.activeInHierarchy)
            return;

        if (Input.GetKeyDown(reloadKey) && !isReloading && currentAmmo < maxAmmoPerMag)
        {
            StartCoroutine(ReloadCoroutine());
        }

        if (isReloading) return;

        if (Input.GetKeyDown(toggleModeKey))
        {
            isFullAuto = !isFullAuto;

            // Play firemode switch sound
            if (fireModeAudioSource != null && fireModeSound != null)
            {
                fireModeAudioSource.PlayOneShot(fireModeSound);
            }

            Debug.Log($"Fire mode: {(isFullAuto ? "Full Auto" : "Semi Auto")}");
        }

        bool shouldShoot = isFullAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        float currentFireRate = isFullAuto ? fullAutoFireRate : semiAutoFireRate;

        if (shouldShoot && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                nextFireTime = Time.time + currentFireRate;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (emptyAudioSource != null && emptySound != null)
                {
                    emptyAudioSource.PlayOneShot(emptySound);
                }
            }
        }

        if (Time.time - lastShotTime > accuracyRecoveryDelay)
        {
            currentInaccuracy = Mathf.MoveTowards(currentInaccuracy, 0f, accuracyRecoverySpeed * Time.deltaTime);
        }

        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRecoil = Vector3.Slerp(currentRecoil, targetRecoil, recoilSpeed * Time.deltaTime);

        if (recoilTarget != null)
        {
            recoilTarget.localRotation = Quaternion.Euler(currentRecoil);
        }

        if (showDebugRay && playerCamera != null)
        {
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * range, debugRayColor);
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        lastShotTime = Time.time;
        currentInaccuracy = Mathf.Min(currentInaccuracy + inaccuracyPerShot, maxInaccuracy);

        targetRecoil += new Vector3(-recoilAmount, Random.Range(-recoilAmount * 0.3f, recoilAmount * 0.3f), 0);

        if (muzzleFlash != null) muzzleFlash.Play();
        if (smokeEffect != null) smokeEffect.Play();
        if (bulletCasings != null) bulletCasings.Play();

        if (shootAudioSource != null && shootSound != null)
        {
            shootAudioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            shootAudioSource.PlayOneShot(shootSound);
        }

        // Play empty sound after firing the last shot
        if (currentAmmo == 0 && emptyAudioSource != null && emptySound != null)
        {
            emptyAudioSource.PlayOneShot(emptySound);
        }

        if (muzzleFlashLight != null)
        {
            StartCoroutine(MuzzleFlashLightCoroutine());
        }

        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        if (currentInaccuracy > 0f)
        {
            Vector3 spread = new Vector3(
                Random.Range(-currentInaccuracy, currentInaccuracy),
                Random.Range(-currentInaccuracy, currentInaccuracy),
                0f
            );
            rayDirection = (rayDirection + playerCamera.transform.TransformDirection(spread * 0.01f)).normalized;
        }

        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(rayOrigin, rayDirection * range, debugRayColor, debugRayDuration);
        }

        if (Physics.Raycast(ray, out hit, range, shootableLayers))
        {
            // Enemy damage
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(25f); // Adjust damage as needed
            }

            // Breakable objects
            BreakableCube breakable = hit.collider.GetComponent<BreakableCube>();
            if (breakable != null)
            {
                breakable.TakeDamage(breakable.damagePerShot);
            }

            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 forceDirection = hit.point - barrelTip.position;
                rb.AddForceAtPosition(forceDirection.normalized * shootForce, hit.point, ForceMode.Impulse);
            }

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

        if (reloadAudioSource != null && reloadSound != null)
        {
            reloadAudioSource.PlayOneShot(reloadSound);
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmoPerMag;
        isReloading = false;
    }

    private IEnumerator MuzzleFlashLightCoroutine()
    {
        muzzleFlashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleFlashLight.enabled = false;
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmoPerMag;
    public bool IsReloading() => isReloading;
}