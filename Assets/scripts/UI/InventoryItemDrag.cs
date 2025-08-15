using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for event interfaces

public class InventoryItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform rectTransform; // RectTransform of the item image
    private CanvasGroup canvasGroup; // CanvasGroup to control transparency and raycasting
    private Canvas canvas; // Reference to the parent Canvas

    // The original slot this item came from
    private ISlot originalSlot; // Changed type from InventorySlot to ISlot

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("InventoryItemDrag: Canvas not found in parent. Dragging may not work correctly.");
        }
    }

    public void SetOriginalSlot(ISlot slot)
    {
        originalSlot = slot;
    }

    public ISlot GetOriginalSlot()
    {
        return originalSlot; // Change return type to ISlot
    }

    // IBeginDragHandler
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalSlot = transform.parent.GetComponent<ISlot>(); // Changed GetComponent to ISlot
        
        // Only allow dragging if there is an item in the slot
        if (originalSlot == null || originalSlot.GetItem() == null)
        {
            eventData.pointerDrag = null; // Prevent drag from starting
            return;
        }

        // Debug.Log("Begin Dragging: " + gameObject.name);
        transform.SetParent(canvas.transform); // Set the item as a child of the Canvas to appear on top
        transform.SetAsLastSibling(); // Ensure it renders on top of other UI elements

        canvasGroup.alpha = .6f; // Make it semi-transparent while dragging
        canvasGroup.blocksRaycasts = false; // Disable raycasting on the dragged item itself so we can drop on slots underneath
    }

    // IDragHandler
    public void OnDrag(PointerEventData eventData)
    {
        // Debug.Log("Dragging: " + gameObject.name);
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor; // Move with mouse
    }

    // IEndDragHandler
    public void OnEndDrag(PointerEventData eventData)
    {
        // Debug.Log("End Dragging: " + gameObject.name);
        canvasGroup.alpha = 1f; // Restore full opacity
        canvasGroup.blocksRaycasts = true; // Re-enable raycasting

        // The ResetPosition() logic has been moved to the respective UI Handlers' OnDropItem methods,
        // or should be called if the item was not successfully dropped on a valid slot.
        // If transform.parent is still Canvas.transform, it means it was not dropped on any valid slot.
        if (transform.parent == canvas.transform)
        {
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        // We need to cast originalSlot back to MonoBehaviour to access transform.SetParent
        MonoBehaviour originalSlotMB = originalSlot as MonoBehaviour;
        if (originalSlotMB != null)
        {
            transform.SetParent(originalSlotMB.transform); // Move it back to its original slot
            rectTransform.anchoredPosition = Vector2.zero; // Reset local position
            transform.SetSiblingIndex(0); // Ensure it renders at the bottom of the slot hierarchy
        }
        else
        {
            // If for some reason originalSlot is null, destroy or hide the item
            Debug.LogWarning("Original slot not found for dragged item. Destroying or hiding item.");
            gameObject.SetActive(false); // Or Destroy(gameObject);
        }
    }

    // IPointerClickHandler to detect double-clicks for equipping
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"InventoryItemDrag: OnPointerClick called on {gameObject.name}. Button: {eventData.button}, ClickCount: {eventData.clickCount}");

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            Debug.Log($"InventoryItemDrag: Double-click detected on {gameObject.name}.");
            // Safely get item, uiHandler, and playerEquipment/playerInventory from originalSlot
            Item clickedItem = originalSlot?.GetItem();
            InventoryUIHandler inventoryUIHandler = originalSlot?.GetInventoryUIHandler();
            EquipmentUIHandler equipmentUIHandler = originalSlot?.GetEquipmentUIHandler();

            if (clickedItem != null && (inventoryUIHandler != null || equipmentUIHandler != null))
            {
                // Determine if item is equippable or consumable
                if (clickedItem.itemType == Item.ItemType.Weapon || clickedItem.itemType == Item.ItemType.Armor)
                {
                    if (inventoryUIHandler != null && inventoryUIHandler.playerEquipment != null) // From Inventory
                    {
                        Debug.Log($"InventoryItemDrag: Attempting to equip item {clickedItem.itemName} from Inventory via double-click.");
                        inventoryUIHandler.playerEquipment.EquipItem(clickedItem);
                    }
                    else if (equipmentUIHandler != null && equipmentUIHandler.playerEquipment != null) // From Equipment
                    {
                        Debug.Log($"InventoryItemDrag: Attempting to unequip item {clickedItem.itemName} from Equipment via double-click.");
                        equipmentUIHandler.playerEquipment.UnequipItem(clickedItem.equipSlot);
                    }
                    else
                    {
                         Debug.LogWarning($"InventoryItemDrag: PlayerEquipment not found for equipping/unequipping.");
                    }
                }
                else if (clickedItem.itemType == Item.ItemType.Consumable)
                {
                    if (inventoryUIHandler != null && inventoryUIHandler.playerInventory != null) // From Inventory
                    {
                        Debug.Log($"InventoryItemDrag: Attempting to use consumable item {clickedItem.itemName} via double-click.");
                        inventoryUIHandler.playerInventory.UseItem(clickedItem);
                    }
                    else
                    {
                        Debug.LogWarning($"InventoryItemDrag: PlayerInventory reference is null for using consumable.");
                    }
                }
                else
                {
                    Debug.Log($"InventoryItemDrag: Item {clickedItem.itemName} is not equippable or consumable.");
                }
            }
            else
            {
                if (originalSlot == null) Debug.LogWarning($"InventoryItemDrag: Original slot is null for {gameObject.name}. Cannot click.");
                if (clickedItem == null) Debug.LogWarning($"InventoryItemDrag: Item is null in original slot {originalSlot?.GetType().Name}. Cannot click.");
                if (inventoryUIHandler == null && equipmentUIHandler == null) Debug.LogWarning($"InventoryItemDrag: No UI Handler found for original slot {originalSlot?.GetType().Name}. Cannot click.");
            }
        }
    }
}
