using UnityEngine;
using System.Collections.Generic;

public class BuildableBlock : MonoBehaviour
{
    [Header("Block Settings")]
    public string blockName = "Block";
    public int blockID = 0;

    [Header("Snap Points")]
    private List<SnapPoint> snapPoints = new List<SnapPoint>();

    [Header("Placement Settings")]
    public bool isPlaced = false;
    public float destructionDelay = 0.1f;

    [Header("Audio")]
    public AudioClip placementSound;
    public AudioClip destructionSound;

    private Rigidbody blockRigidbody;
    private Collider[] blockColliders;

    void Awake()
    {
        blockRigidbody = GetComponent<Rigidbody>();
        blockColliders = GetComponentsInChildren<Collider>();

        // Find all snap points in children
        SnapPoint[] childSnapPoints = GetComponentsInChildren<SnapPoint>();
        foreach (SnapPoint snapPoint in childSnapPoints)
        {
            RegisterSnapPoint(snapPoint);
        }
    }

    void Start()
    {
        // Disable physics until placed
        if (blockRigidbody != null)
        {
            blockRigidbody.isKinematic = true;
        }
    }

    public void RegisterSnapPoint(SnapPoint snapPoint)
    {
        if (!snapPoints.Contains(snapPoint))
        {
            snapPoints.Add(snapPoint);
        }
    }

    public List<SnapPoint> GetSnapPoints()
    {
        return new List<SnapPoint>(snapPoints);
    }

    public SnapPoint GetClosestAvailableSnapPoint(Vector3 position)
    {
        SnapPoint closest = null;
        float closestDistance = float.MaxValue;

        foreach (SnapPoint snapPoint in snapPoints)
        {
            if (!snapPoint.isOccupied)
            {
                float distance = Vector3.Distance(position, snapPoint.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = snapPoint;
                }
            }
        }

        return closest;
    }

    public void PlaceBlock()
    {
        if (isPlaced) return;

        isPlaced = true;

        // Enable physics
        if (blockRigidbody != null)
        {
            blockRigidbody.isKinematic = false;
        }

        // Enable colliders
        foreach (Collider col in blockColliders)
        {
            col.enabled = true;
        }

        // Add block tag
        gameObject.tag = "Block";

        // Play placement sound
        if (placementSound != null)
        {
            AudioSource.PlayClipAtPoint(placementSound, transform.position);
        }
    }

    public void DestroyBlock()
    {
        if (!isPlaced) return;

        // Mark all snap points as available
        foreach (SnapPoint snapPoint in snapPoints)
        {
            snapPoint.SetOccupied(false);
        }

        // Play destruction sound
        if (destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(destructionSound, transform.position);
        }

        // Destroy after delay
        Destroy(gameObject, destructionDelay);
    }

    public void SetSnapPointsOccupied(bool occupied)
    {
        foreach (SnapPoint snapPoint in snapPoints)
        {
            snapPoint.SetOccupied(occupied);
        }
    }

    public bool HasAvailableSnapPoints()
    {
        foreach (SnapPoint snapPoint in snapPoints)
        {
            if (!snapPoint.isOccupied)
                return true;
        }
        return false;
    }
}