using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapons")]
    public GameObject rifle;
    public GameObject axe;

    [Header("Settings")]
    public float axeRange = 3f;
    public LayerMask treeLayer = 1 << 8; // Set trees to layer 8

    private bool isAxeActive = false;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        // Start with rifle active
        SetWeapon(false);
    }

    void Update()
    {
        // Weapon switching
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isAxeActive = !isAxeActive;
            SetWeapon(isAxeActive);
        }

        // Axe chopping
        if (isAxeActive && Input.GetMouseButtonDown(0))
        {
            ChopTree();
        }
    }

    void SetWeapon(bool useAxe)
    {
        rifle.SetActive(!useAxe);
        axe.SetActive(useAxe);
    }

    void ChopTree()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, axeRange, treeLayer))
        {
            // First check the hit object itself
            Tree tree = hit.collider.GetComponent<Tree>();
            TreeLog treeLog = hit.collider.GetComponent<TreeLog>();

            // If not found, check the parent object
            if (tree == null && treeLog == null && hit.collider.transform.parent != null)
            {
                tree = hit.collider.transform.parent.GetComponent<Tree>();
                treeLog = hit.collider.transform.parent.GetComponent<TreeLog>();
            }

            // If still not found, check if the hit object is a parent with Tree/TreeLog in children
            if (tree == null && treeLog == null)
            {
                tree = hit.collider.GetComponentInParent<Tree>();
                treeLog = hit.collider.GetComponentInParent<TreeLog>();
            }

            if (tree != null)
            {
                tree.TakeDamage(1);
                Debug.Log("Hit tree!");
            }
            else if (treeLog != null)
            {
                treeLog.TakeDamage(1);
                Debug.Log("Hit log!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (isAxeActive && playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 rayStart = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;
            Gizmos.DrawRay(rayStart, rayDirection * axeRange);
        }
    }
}