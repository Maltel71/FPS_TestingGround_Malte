using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class Carriable : MonoBehaviour
{
    [Header("Break Force Settings")]
    [SerializeField] private float breakForce = 5f;

    private Rigidbody rb;
    private Collider col;
    private bool isBeingCarried = false;
    private FirstPersonController carrier;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void StartCarrying(FirstPersonController controller)
    {
        isBeingCarried = true;
        carrier = controller;
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;
    }

    public void StopCarrying()
    {
        isBeingCarried = false;
        carrier = null;
        rb.useGravity = true;
        rb.freezeRotation = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
    }

    public bool IsBeingCarried()
    {
        return isBeingCarried;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBeingCarried && carrier != null)
        {
            // Calculate the impact force
            float impactForce = collision.relativeVelocity.magnitude * rb.mass;

            // If the impact force exceeds the break force, drop the object
            if (impactForce > breakForce)
            {
                carrier.ForceDropObject();
            }
        }
    }
}