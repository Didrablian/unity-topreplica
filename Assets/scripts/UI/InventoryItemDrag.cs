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

    private RectTransform inventoryPanelRectTransform; // Reference to the inventory panel's RectTransform
    private PlayerInventory playerInventory; // Reference to the PlayerInventory
    private PlayerEquipment playerEquipment; // Reference to PlayerEquipment

    // New: Reference to the Player's transform for dropping items
    public Transform playerTransform; // Assign in inspector or find dynamically

    // New: Reference to the current item being dragged
    private InventoryItem currentInventoryItem;
    // New: Reference to the Image component for the item being dragged
    private Image image;
    // New: Reference to the original parent of the dragged item
    private Transform originalParent;

    // New: Reference to the Dark Background Image (if used)
    public GameObject darkBGImage; // Assign in inspector, or find dynamically if always present

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("InventoryItemDrag: Canvas not found in parent. Dragging may not work correctly.");
        }

        playerInventory = FindObjectOfType<PlayerInventory>();
        playerEquipment = FindObjectOfType<PlayerEquipment>();

        // Find the inventory panel dynamically if not assigned
        if (inventoryPanelRectTransform == null)
        {
            // Assuming the inventory panel is the parent of the slots container or a known parent
            // You might need to adjust this path based on your actual UI hierarchy
            Transform parentTransform = transform.parent;
            while (parentTransform != null && inventoryPanelRectTransform == null)
            {
                if (parentTransform.name == "InventoryPanel") // Or a tag/component check
                {
                    inventoryPanelRectTransform = parentTransform.GetComponent<RectTransform>();
                }
                parentTransform = parentTransform.parent;
            }
            if (inventoryPanelRectTransform == null)
            {
                Debug.LogWarning("InventoryPanel RectTransform not found. Item dropping outside panel may not work.");
            }
        }

        // New: Find the player transform dynamically if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Assuming your player has the "Player" tag
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("Player GameObject not found. Cannot drop items in world.");
            }
        }

        // New: Find DarkBGImage dynamically if not assigned
        if (darkBGImage == null)
        {
            darkBGImage = GameObject.FindGameObjectWithTag("DarkPanel"); // Find by tag
            if (darkBGImage == null)
            {
                Debug.LogWarning("DarkBGPanel (darkBGImage) not found with tag 'DarkPanel'. Dropping items on it might not be explicitly handled.");
            }
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
        // Debug.Log("Beginning Drag: " + gameObject.name);
        image = GetComponent<Image>();
        originalParent = transform.parent; // Store the original parent
        GetComponent<CanvasGroup>().blocksRaycasts = false; // Disable raycasts so we can drop on other UI elements
        image.raycastTarget = false; // Disable raycast target during drag

        InventorySlot currentSlot = originalParent.GetComponent<InventorySlot>();
        if (currentSlot != null) // Check if the parent is actually an InventorySlot
        {
            originalSlot = currentSlot; // Store the original slot
            currentInventoryItem = playerInventory.items[System.Array.IndexOf(playerInventory.uiHandler.slots, originalSlot)];
            // Debug.Log($"Starting drag from slot: {originalSlot.name}, Item: {currentInventoryItem.itemData.itemName}");
        }
        
        // Only allow dragging if there is an item in the slot
        if (originalSlot == null || currentInventoryItem == null)
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
        Debug.Log("OnEndDrag called.");
        // Ensure the dragged item is back to its original parent and state
        transform.SetParent(originalParent);
        GetComponent<CanvasGroup>().blocksRaycasts = true; // Re-enable raycasts
        image.raycastTarget = true; // Re-enable raycast target

        // Always update the UI after a drag operation that might have changed inventory state
        if (playerInventory != null && playerInventory.uiHandler != null)
        {
            playerInventory.uiHandler.UpdateInventoryUI();
        }

        // If the item was successfully equipped, the currentInventoryItem will be nullified by TryEquipItem
        // So we need to ensure currentInventoryItem is still valid before processing a drop.
        if (currentInventoryItem == null)
        {
            Debug.Log("currentInventoryItem is null after drag. Assuming item was equipped or handled elsewhere.");
            return; // Exit if item was already handled (e.g., equipped)
        }

        Debug.Log($"OnEndDrag - eventData.pointerEnter: {eventData.pointerEnter?.name ?? "NULL"}");

        // Check if the pointer is over any UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Pointer is over a UI element.");
            // Case 1: Dropped onto an InventorySlot
            if (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<InventorySlot>() != null)
            {
                InventorySlot dropSlot = eventData.pointerEnter.GetComponent<InventorySlot>();
                Debug.Log($"Dropped on InventorySlot: {dropSlot.name}");

                // Handle equipping or moving within inventory
                if (currentInventoryItem.itemData.itemType == Item.ItemType.Weapon || currentInventoryItem.itemData.itemType == Item.ItemType.Armor)
                {
                    if (dropSlot.slotType != Item.EquipmentSlotType.None && currentInventoryItem.itemData.equipSlot == dropSlot.slotType)
                    {
                        Debug.Log($"Attempting to equip {currentInventoryItem.itemData.itemName} to slot: {dropSlot.slotType}");
                        TryEquipItem(dropSlot, currentInventoryItem);
                        currentInventoryItem = null; // Clear the current dragged item after successful equip
                    }
                    else if (dropSlot.slotType != Item.EquipmentSlotType.None && currentInventoryItem.itemData.equipSlot != dropSlot.slotType)
                    {
                        Debug.Log($"Cannot equip {currentInventoryItem.itemData.itemName} to an incompatible slot type {dropSlot.slotType}. Swapping items.");
                        playerInventory.MoveItem((InventorySlot)originalSlot, dropSlot); // Cast originalSlot to InventorySlot
                    }
                    else
                    {
                        Debug.Log($"Moving item to inventory slot: {dropSlot.name}");
                        playerInventory.MoveItem((InventorySlot)originalSlot, dropSlot); // Cast originalSlot to InventorySlot
                    }
                }
                else // If it's not an equipable item
                {
                    Debug.Log($"Moving non-equippable item to inventory slot: {dropSlot.name}");
                    playerInventory.MoveItem((InventorySlot)originalSlot, dropSlot); // Cast originalSlot to InventorySlot
                }
            }
            // Case 2: Dropped onto the darkBGImage (treat as dropping in world)
            else if (eventData.pointerEnter != null && eventData.pointerEnter.gameObject == darkBGImage)
            {
                Debug.Log($"Dropped {currentInventoryItem.itemData.itemName} on DarkBGPanel. Treating as drop in world.");
                DropItemInWorld(currentInventoryItem.itemData, currentInventoryItem.quantity);
                playerInventory.RemoveItem(currentInventoryItem.itemData, currentInventoryItem.quantity); // Remove from inventory
                currentInventoryItem = null; // Clear the current dragged item after successful drop
            }
            else
            {
                Debug.Log("Dropped on a UI element, but not an InventorySlot or DarkBGPanel. Reverting.");
                // Already handled by SetParent(originalParent) at the start of OnEndDrag
            }
        }
        // Case 3: Dropped outside any UI element (truly outside panel)
        else if (IsPointerOutsidePanel(eventData.position))
        {
            Debug.Log($"Dropped {currentInventoryItem.itemData.itemName} truly outside any UI panel (in game world).");
            DropItemInWorld(currentInventoryItem.itemData, currentInventoryItem.quantity);
            playerInventory.RemoveItem(currentInventoryItem.itemData, currentInventoryItem.itemData.isStackable ? currentInventoryItem.quantity : 1); // Remove quantity or 1 if not stackable
            currentInventoryItem = null; // Clear the current dragged item after successful drop
        }
        else
        {
            Debug.Log("Dropped in game world but not explicitly outside panel (e.g., on a non-UI game object). Reverting.");
            // Already handled by SetParent(originalParent) at the start of OnEndDrag
        }

        // Reset currentInventoryItem at the very end to prevent issues with subsequent drags
        currentInventoryItem = null; // Ensure it's nullified after processing a drop
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

    // New: Check if the pointer is outside the inventory panel
    private bool IsPointerOutsidePanel(Vector2 pointerPosition)
    {
        if (inventoryPanelRectTransform == null) return true; // If panel not found, assume outside

        return !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRectTransform, pointerPosition);
    }

    // New: Method to drop item in the world
    private void DropItemInWorld(Item itemToDrop, int quantity)
    {
        if (itemToDrop == null)
        {
            Debug.LogWarning("Cannot drop item: Item data is null.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("Cannot drop item: Player Transform is null.");
            return;
        }

        if (itemToDrop.itemPrefab == null)
        {
            Debug.LogWarning($"Cannot drop {itemToDrop.itemName}: Item Prefab is not assigned in the Item ScriptableObject.");
            return;
        }

        Vector3 dropPosition = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 0.5f; // Drop in front and slightly above the player
        Debug.Log($"Attempting to drop {itemToDrop.itemName} at position: {dropPosition}");

        GameObject droppedGameObject = Instantiate(itemToDrop.itemPrefab, dropPosition, Quaternion.identity);

        if (droppedGameObject == null)
        {
            Debug.LogError($"Failed to instantiate prefab for {itemToDrop.itemName}.");
            return;
        }

        Debug.Log($"Successfully instantiated {itemToDrop.itemName} prefab: {droppedGameObject.name} at {droppedGameObject.transform.position}");

        // Add a WorldItem component to the newly spawned prefab if it doesn't have one
        WorldItem worldItem = droppedGameObject.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            worldItem = droppedGameObject.AddComponent<WorldItem>();
            Debug.Log($"Added WorldItem component to {droppedGameObject.name}.");
        }
        worldItem.itemData = itemToDrop; // Assign the item data
        Debug.Log($"Assigned itemData to WorldItem: {worldItem.itemData.itemName}");

        // Re-enable destroy timer for dropped item
        Destroy(droppedGameObject, 30f); 

        Debug.Log($"Dropped {quantity} x {itemToDrop.itemName} in the world.");
    }

    // New: Method to handle equipping an item
    private void TryEquipItem(InventorySlot dropSlot, InventoryItem itemToEquip)
    {
        if (playerEquipment == null)
        {
            Debug.LogWarning("PlayerEquipment reference is null. Cannot equip item.");
            return;
        }

        if (itemToEquip == null || itemToEquip.itemData == null)
        {
            Debug.LogWarning("Attempted to equip a null item or item with null data.");
            return;
        }

        // Check if the item can be equipped to this slot type
        if (itemToEquip.itemData.itemType == Item.ItemType.Weapon || itemToEquip.itemData.itemType == Item.ItemType.Armor)
        {
            if (dropSlot.slotType == itemToEquip.itemData.equipSlot)
            {
                // If there's an item currently in the equipment slot, unequip it first
                InventoryItem currentlyEquippedItem = playerEquipment.GetEquippedItem(dropSlot.slotType);
                if (currentlyEquippedItem != null)
                {
                    Debug.Log($"Unequipping {currentlyEquippedItem.itemData.itemName} from {dropSlot.slotType}.");
                    playerEquipment.UnequipItem(dropSlot.slotType); // This will add the item back to inventory or drop it
                }

                // Equip the new item
                Debug.Log($"Equipping {itemToEquip.itemData.itemName} to {dropSlot.slotType}.");
                playerEquipment.EquipItem(itemToEquip.itemData); // Equip the new item
                playerInventory.RemoveItem(itemToEquip.itemData, itemToEquip.quantity); // Remove from inventory
            }
            else
            {
                Debug.LogWarning($"Cannot equip {itemToEquip.itemData.itemName} to {dropSlot.slotType} slot. Mismatched slot types.");
            }
        }
        else
        {
            Debug.LogWarning($"Item {itemToEquip.itemData.itemName} is not an equipable type (Weapon/Armor).");
        }
    }
}
