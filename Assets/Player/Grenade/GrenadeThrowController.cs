using UnityEngine;

public class GrenadeThrowController : MonoBehaviour
{
    [Header("Throwing Settings")]
    public GameObject grenadePrefab;
    public Transform throwPoint;
    public float throwForce = 10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowGrenade();
        }
    }

    void ThrowGrenade()
    {
        if (grenadePrefab == null || throwPoint == null) return;

        // Instantiate grenade at throw point
        GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, throwPoint.rotation);

        // Add throwing force
        Rigidbody grenadeRb = grenade.GetComponent<Rigidbody>();
        if (grenadeRb != null)
        {
            grenadeRb.AddForce(throwPoint.forward * throwForce, ForceMode.VelocityChange);
        }
    }
}