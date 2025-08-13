using UnityEngine;

public class WorldItem : MonoBehaviour
{
    public Item itemData; // Reference to the Item ScriptableObject this world item represents

    [Header("Visuals (Optional)")]
    public SpriteRenderer spriteRenderer; // Optional: Assign if using a sprite for visualization
    public MeshFilter meshFilter;       // Optional: Assign if using a mesh for visualization
    public MeshRenderer meshRenderer;   // Optional: Assign if using a mesh for visualization

    void Start()
    {
        // Optional: Update visual based on itemData icon/prefab if not set manually
        if (itemData != null && spriteRenderer != null && itemData.icon != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }
        // You would typically handle 3D model loading here if itemData had a prefab reference.
    }

    void OnTriggerEnter(Collider other)
    {
        // We will implement player pickup logic here later (in PlayerInteraction or TopDownControls)
        // For now, it just holds the itemData.
    }
}
