using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Debug = UnityEngine.Debug;

public class NetworkWeaponShooting : NetworkBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float shootForce = 20f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float damage = 25f; // Damage per shot
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

    // Network variables for ammo
    private NetworkVariable<int> networkCurrentAmmo = new NetworkVariable<int>(
        30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> networkIsReloading = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float nextFireTime = 0f;
    private Vector3 currentRecoil;
    private Vector3 targetRecoil;
    private bool isFullAuto = false;
    private float currentInaccuracy = 0f;
    private float lastShotTime = 0f;

    // Local properties for easy access
    public int CurrentAmmo => networkCurrentAmmo.Value;
    public bool IsReloading => networkIsReloading.Value;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            networkCurrentAmmo.Value = maxAmmoPerMag;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (muzzleFlashLight != null)
            muzzleFlashLight.enabled = false;
    }

    private void Update()
    {
        // Only process input if we own this weapon
        if (!IsOwner) return;

        // Handle reload
        if (Input.GetKeyDown(reloadKey) && !IsReloading && CurrentAmmo < maxAmmoPerMag)
        {
            StartReloadServerRpc();
        }

        if (IsReloading) return;

        // Toggle fire mode
        if (Input.GetKeyDown(toggleModeKey))
        {
            isFullAuto = !isFullAuto;
            Debug.Log("Fire mode: " + (isFullAuto ? "Full Auto" : "Semi Auto"));
        }

        // Handle shooting
        bool shouldShoot = isFullAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        float currentFireRate = isFullAuto ? fullAutoFireRate : semiAutoFireRate;

        if (shouldShoot && Time.time >= nextFireTime)
        {
            if (CurrentAmmo > 0)
            {
                // Calculate shot data locally for immediate feedback
                Vector3 rayOrigin = playerCamera.transform.position;
                Vector3 rayDirection = CalculateShootDirection();

                // Send to server for authoritative processing
                ShootServerRpc(rayOrigin, rayDirection);

                // Apply local effects immediately for responsiveness
                ApplyLocalShootEffects();

                nextFireTime = Time.time + currentFireRate;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // Play empty sound locally
                if (emptyAudioSource != null && emptySound != null)
                {
                    emptyAudioSource.PlayOneShot(emptySound);
                }
            }
        }

        // Update accuracy recovery
        if (Time.time - lastShotTime > accuracyRecoveryDelay)
        {
            currentInaccuracy = Mathf.MoveTowards(currentInaccuracy, 0f, accuracyRecoverySpeed * Time.deltaTime);
        }

        // Update recoil
        UpdateRecoil();

        // Debug ray
        if (showDebugRay && playerCamera != null)
        {
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * range, debugRayColor);
        }
    }

    private Vector3 CalculateShootDirection()
    {
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

        return rayDirection;
    }

    private void ApplyLocalShootEffects()
    {
        lastShotTime = Time.time;
        currentInaccuracy = Mathf.Min(currentInaccuracy + inaccuracyPerShot, maxInaccuracy);
        targetRecoil += new Vector3(-recoilAmount, Random.Range(-recoilAmount * 0.3f, recoilAmount * 0.3f), 0);
    }

    private void UpdateRecoil()
    {
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRecoil = Vector3.Slerp(currentRecoil, targetRecoil, recoilSpeed * Time.deltaTime);

        if (recoilTarget != null)
        {
            recoilTarget.localRotation = Quaternion.Euler(currentRecoil);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 rayOrigin, Vector3 rayDirection)
    {
        // Validate on server
        if (networkCurrentAmmo.Value <= 0 || networkIsReloading.Value)
            return;

        // Decrease ammo
        networkCurrentAmmo.Value--;

        // Perform raycast on server for authoritative hit detection
        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range, shootableLayers))
        {
            // Check if we hit a player
            PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Apply damage to player
                playerHealth.TakeDamage(damage);
                Debug.Log($"Player hit for {damage} damage at distance {hit.distance:F1}m");
            }

            // Check for breakable objects
            BreakableCube breakable = hit.collider.GetComponent<BreakableCube>();
            if (breakable != null)
            {
                breakable.TakeDamage(breakable.damagePerShot);
            }

            // Apply physics force
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 forceDirection = hit.point - rayOrigin;
                rb.AddForceAtPosition(forceDirection.normalized * shootForce, hit.point, ForceMode.Impulse);
            }

            // Tell all clients to show impact effects
            ShowImpactEffectsClientRpc(hit.point, hit.normal);
        }

        // Tell all clients to show shoot effects
        ShowShootEffectsClientRpc();

        // Play empty sound if this was the last shot
        if (networkCurrentAmmo.Value == 0)
        {
            PlayEmptySoundClientRpc();
        }
    }

    [ClientRpc]
    private void ShowShootEffectsClientRpc()
    {
        // Visual effects
        if (muzzleFlash != null) muzzleFlash.Play();
        if (smokeEffect != null) smokeEffect.Play();
        if (bulletCasings != null) bulletCasings.Play();

        // Audio
        if (shootAudioSource != null && shootSound != null)
        {
            shootAudioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            shootAudioSource.PlayOneShot(shootSound);
        }

        // Muzzle flash light
        if (muzzleFlashLight != null)
        {
            StartCoroutine(MuzzleFlashLightCoroutine());
        }
    }

    [ClientRpc]
    private void ShowImpactEffectsClientRpc(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (impactVFXPrefab != null)
        {
            GameObject impactVFX = Instantiate(impactVFXPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(impactVFX, impactVFXLifetime);
        }
    }

    [ClientRpc]
    private void PlayEmptySoundClientRpc()
    {
        if (emptyAudioSource != null && emptySound != null)
        {
            emptyAudioSource.PlayOneShot(emptySound);
        }
    }

    [ServerRpc]
    private void StartReloadServerRpc()
    {
        if (networkIsReloading.Value || networkCurrentAmmo.Value >= maxAmmoPerMag)
            return;

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        networkIsReloading.Value = true;

        // Tell all clients to play reload sound
        PlayReloadSoundClientRpc();

        yield return new WaitForSeconds(reloadTime);

        networkCurrentAmmo.Value = maxAmmoPerMag;
        networkIsReloading.Value = false;
    }

    [ClientRpc]
    private void PlayReloadSoundClientRpc()
    {
        if (reloadAudioSource != null && reloadSound != null)
        {
            reloadAudioSource.PlayOneShot(reloadSound);
        }
    }

    private IEnumerator MuzzleFlashLightCoroutine()
    {
        muzzleFlashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleFlashLight.enabled = false;
    }

    // Public getters for UI
    public int GetCurrentAmmo() => CurrentAmmo;
    public int GetMaxAmmo() => maxAmmoPerMag;
    public bool GetIsReloading() => IsReloading;
    public bool GetIsFullAuto() => isFullAuto;
}