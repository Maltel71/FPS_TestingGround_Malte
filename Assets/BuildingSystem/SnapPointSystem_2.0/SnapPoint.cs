using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("Snap Point Settings")]
    public SnapPointType snapType = SnapPointType.Universal;
    public bool isOccupied = false;
    public float snapRange = 1.0f;

    [Header("Visual Settings")]
    public bool showGizmos = true;
    public Color availableColor = Color.green;
    public Color occupiedColor = Color.red;
    public float gizmoSize = 0.2f;

    private BuildableBlock parentBlock;

    public enum SnapPointType
    {
        Universal,      // Can connect to any snap point
        Wall,          // Only connects to other wall points
        Floor,         // Only connects to other floor points
        Corner,        // Only connects to other corner points
        Custom         // Custom connection rules
    }

    void Start()
    {
        parentBlock = GetComponentInParent<BuildableBlock>();
        if (parentBlock != null)
        {
            parentBlock.RegisterSnapPoint(this);
        }
    }

    public bool CanSnapTo(SnapPoint other)
    {
        if (other == null || other.isOccupied || this.isOccupied)
            return false;

        // Check if snap types are compatible
        if (snapType == SnapPointType.Universal || other.snapType == SnapPointType.Universal)
            return true;

        return snapType == other.snapType;
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    public BuildableBlock GetParentBlock()
    {
        return parentBlock;
    }

    public Vector3 GetSnapPosition()
    {
        return transform.position;
    }

    public Quaternion GetSnapRotation()
    {
        return transform.rotation;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = isOccupied ? occupiedColor : availableColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);

        // Draw direction arrow
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * gizmoSize * 2);
    }
}