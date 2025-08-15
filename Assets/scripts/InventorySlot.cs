using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for event interfaces
using TMPro; // Required for TextMeshProUGUI
using System.Collections; // Required for Coroutines

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, ISlot
{
    public Image icon; // Reference to the UI Image component for the item icon
    public Item item; // The item currently in this slot (this is itemData from InventoryItem)
    public GameObject itemImagePrefab; // Prefab for the draggable item image

    // New: Reference to the TextMeshProUGUI for stack count
    public TextMeshProUGUI stackCountText;

    public InventoryUIHandler uiHandler; // Changed from private to public
    private GameObject currentItemImageGameObject; // To keep track of the instantiated item image

    // New: CanvasGroup for stackCountText to control raycasting
    private CanvasGroup stackTextCanvasGroup;

    // New: Reference to the Outline component for visual feedback
    private Outline outline;

    // New: Colors for highlighting
    public Color normalColor = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Example: Dark transparent grey
    public Color highlightColor = new Color(0.9f, 0.9f, 0.1f, 0.8f); // Example: Yellowish transparent

    // New: Outline thickness
    public float outlineThickness = 2f; // Default thickness

    // New: Coroutine reference for continuous tooltip update
    private Coroutine tooltipUpdateCoroutine;

    // New: ISlot implementation
    public Item GetItem()
    {
        return item;
    }

    public InventoryUIHandler GetInventoryUIHandler()
    {
        return uiHandler;
    }

    public EquipmentUIHandler GetEquipmentUIHandler()
    {
        return null; // Inventory slot doesn't have an EquipmentUIHandler directly
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    void Awake()
    {
        uiHandler = GetComponentInParent<InventoryUIHandler>();
        if (uiHandler == null)
        {
            Debug.LogError("InventorySlot: InventoryUIHandler not found in parent for " + gameObject.name + ". Make sure InventoryUIHandler is on a parent GameObject.");
        }

        // Get the Outline component
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            Debug.LogWarning("InventorySlot: Outline component not found on " + gameObject.name + ". Add an Outline UI component to this GameObject for visual highlighting.");
        }
        else
        {
            outline.effectDistance = new Vector2(outlineThickness, outlineThickness); // Set thickness
        }
        SetOutlineColor(normalColor); // Set initial color

        // Get or add CanvasGroup to stackCountText and configure it for no raycast blocking
        if (stackCountText != null)
        {
            stackTextCanvasGroup = stackCountText.GetComponent<CanvasGroup>();
            if (stackTextCanvasGroup == null)
            {
                stackTextCanvasGroup = stackCountText.gameObject.AddComponent<CanvasGroup>();
            }
            stackTextCanvasGroup.blocksRaycasts = false; // Prevent it from blocking mouse events
            stackTextCanvasGroup.interactable = false; // Prevent it from being interactable
        }
    }

    // Change AddItem to accept InventoryItem
    public void AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || inventoryItem.itemData == null)
        {
            ClearSlot();
            return;
        }

        Debug.Log($"InventorySlot: AddItem called for {gameObject.name} with item: {inventoryItem.itemData.itemName}, quantity: {inventoryItem.quantity}");
        item = inventoryItem.itemData; // Store the ItemData for tooltip and other references

        if (itemImagePrefab != null)
        {
            if (currentItemImageGameObject != null)
            {
                Destroy(currentItemImageGameObject); // Destroy previous image if any
            }
            currentItemImageGameObject = Instantiate(itemImagePrefab, icon.transform); // Instantiate item image as child of icon
            Image itemImage = currentItemImageGameObject.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.sprite = inventoryItem.itemData.icon;
                itemImage.enabled = true;
                Debug.Log($"InventorySlot: Set icon for {gameObject.name} to {inventoryItem.itemData.icon.name}");
            }
            else
            {
                Debug.LogError("InventorySlot: Instantiated item image prefab does not have an Image component on " + currentItemImageGameObject.name);
            }
            InventoryItemDrag itemDrag = currentItemImageGameObject.GetComponent<InventoryItemDrag>();
            if (itemDrag != null)
            {
                itemDrag.SetOriginalSlot(this);
            }
            else
            {
                Debug.LogError("InventorySlot: Instantiated item image prefab does not have an InventoryItemDrag component on " + currentItemImageGameObject.name);
            }

            // NEW: Debugging sibling indices
            Debug.Log($"InventorySlot: Before SiblingIndex Adjustment - Slot ({gameObject.name}) children:\n  Icon: {icon.name} (Index: {icon.transform.GetSiblingIndex()})\n  StackText: {(stackCountText != null ? stackCountText.name : "N/A")} (Index: {(stackCountText != null ? stackCountText.transform.GetSiblingIndex().ToString() : "N/A")})\n  ClonedItem: {currentItemImageGameObject.name} (Parent: {currentItemImageGameObject.transform.parent.name}, Index: {currentItemImageGameObject.transform.GetSiblingIndex()})");

            // Ensure item icon clone is rendered below the stack count text
            // To ensure consistent ordering, we should make stackCountText always the last sibling
            // and then put the item image just before it.
            if (stackCountText != null)
            {
                stackCountText.transform.SetAsLastSibling(); // Make stack text always render on top
                currentItemImageGameObject.transform.SetSiblingIndex(stackCountText.transform.GetSiblingIndex() - 1); // Place item icon right before it
            }

            // NEW: Debugging sibling indices after adjustment
            Debug.Log($"InventorySlot: After SiblingIndex Adjustment - Slot ({gameObject.name}) children:\n  Icon: {icon.name} (Index: {icon.transform.GetSiblingIndex()})\n  StackText: {(stackCountText != null ? stackCountText.name : "N/A")} (Index: {(stackCountText != null ? stackCountText.transform.GetSiblingIndex().ToString() : "N/A")})\n  ClonedItem: {currentItemImageGameObject.name} (Parent: {currentItemImageGameObject.transform.parent.name}, Index: {currentItemImageGameObject.transform.GetSiblingIndex()})");

        }
        else
        {
            Debug.LogWarning("InventorySlot: itemImagePrefab is not assigned in InventorySlot for " + gameObject.name + ". Item icon will be set directly to the slot's Image component, but drag functionality will be limited.");
            // Fallback to directly setting icon sprite if prefab is not set
            icon.sprite = inventoryItem.itemData.icon;
            icon.enabled = true; // Make the icon visible
        }

        // Update stack count text based on actual quantity
        if (stackCountText != null)
        {
            if (inventoryItem.itemData.isStackable && inventoryItem.quantity > 1)
            {
                stackCountText.text = "x" + inventoryItem.quantity.ToString();
                stackCountText.enabled = true;
            }
            else
            {
                stackCountText.text = "x1"; // For single items, show x1 for clarity
                stackCountText.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("InventorySlot: stackCountText is not assigned for " + gameObject.name);
        }
    }

    public void ClearSlot()
    {
        item = null;
        if (currentItemImageGameObject != null)
        {
            Destroy(currentItemImageGameObject);
            currentItemImageGameObject = null;
        }
        icon.sprite = null; // Clear icon sprite (if directly used)
        icon.enabled = false; // Hide the icon (if directly used)
        SetOutlineColor(normalColor); // Reset outline color when cleared

        // Hide stack count text when slot is cleared
        if (stackCountText != null)
        {
            stackCountText.enabled = false;
            stackCountText.text = "";
        }
    }

    // IDropHandler
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            InventoryItemDrag draggedItem = eventData.pointerDrag.GetComponent<InventoryItemDrag>();
            if (draggedItem != null && uiHandler != null)
            {
                uiHandler.OnDropItem(draggedItem, draggedItem.GetOriginalSlot(), this); // Pass draggedItem, sourceSlot, and dropSlot
                // Reset outline color after drop, as OnPointerExit might not be called immediately
                SetOutlineColor(normalColor);
            }
        }
    }

    // NEW: IPointerClickHandler to detect double-clicks (Moving this to InventoryItemDrag.cs)
    // Removed: public void OnPointerClick(PointerEventData eventData)
    // Removed: {
    // Removed:     Debug.Log($"InventorySlot: OnPointerClick called on {gameObject.name}. Button: {eventData.button}, ClickCount: {eventData.clickCount}");
    // Removed:     if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
    // Removed:     {
    // Removed:         Debug.Log($"InventorySlot: Double-click detected on {gameObject.name}.");
    // Removed:         if (item != null && uiHandler != null && uiHandler.playerEquipment != null)
    // Removed:         {
    // Removed:             Debug.Log($"InventorySlot: Attempting to equip item {item.itemName} via double-click.");
    // Removed:             uiHandler.playerEquipment.EquipItem(item);
    // Removed:         }
    // Removed:         else
    // Removed:         {
    // Removed:             if (item == null) Debug.LogWarning($"InventorySlot: Item is null on {gameObject.name}. Cannot equip.");
    // Removed:             if (uiHandler == null) Debug.LogWarning($"InventorySlot: UI Handler is null on {gameObject.name}. Cannot equip.");
    // Removed:             if (uiHandler != null && uiHandler.playerEquipment == null) Debug.LogWarning($"InventorySlot: PlayerEquipment is null on UI Handler. Cannot equip.");
    // Removed:         }
    // Removed:     }
    // Removed: }

    // IPointerEnterHandler (for tooltip & highlighting)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null && uiHandler != null)
        {
            uiHandler.ShowItemTooltip(item); // Show initial tooltip

            // Start coroutine to update tooltip dynamically based on Shift key
            if (tooltipUpdateCoroutine != null)
            {
                StopCoroutine(tooltipUpdateCoroutine);
            }
            tooltipUpdateCoroutine = StartCoroutine(CheckShiftForTooltip());
        }
        // Also highlight when dragging an item over this slot
        if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<InventoryItemDrag>() != null)
        {
            SetOutlineColor(highlightColor);
        }
    }

    // NEW: Coroutine for continuous tooltip update
    private IEnumerator CheckShiftForTooltip()
    {
        bool wasShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        // Yield once to ensure next frame's input is fresh after OnPointerEnter
        yield return null; 

        while (true)
        {
            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (isShiftPressed != wasShiftPressed)
            {
                // Shift state changed, update tooltip
                if (uiHandler != null && item != null) // Ensure references are valid
                {
                    uiHandler.ShowItemTooltip(item);
                }
                wasShiftPressed = isShiftPressed;
            }
            yield return null; // Wait for the next frame
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        // We no longer need to call ShowItemTooltip here, as the coroutine handles continuous updates
        // The primary purpose of OnPointerMove for tooltip is now handled by the coroutine.
        // Keeping this method here for other potential future uses if needed.

        // If the tooltip is currently active and there's an item, update it
        // This allows dynamic comparison (e.g., holding/releasing Shift while hovering)
        if (uiHandler != null && uiHandler.tooltipManager != null && uiHandler.tooltipManager.tooltipPanel.activeSelf)
        {
            // The actual tooltip content update is now handled by the coroutine
            // This block is left in case you want other logic dependent on mouse movement.
            // if (item != null)
            // {
            //     uiHandler.ShowItemTooltip(item);
            // }
            // else
            // {
            //     uiHandler.HideItemTooltip(); // Hide if somehow no item but tooltip is active
            // }
        }
    }

    // IPointerExitHandler (for tooltip & highlighting)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiHandler != null)
        {
            uiHandler.HideItemTooltip();
        }
        SetOutlineColor(normalColor); // Reset outline color

        // Stop the tooltip update coroutine when mouse exits
        if (tooltipUpdateCoroutine != null)
        {
            StopCoroutine(tooltipUpdateCoroutine);
            tooltipUpdateCoroutine = null; // Clear reference
        }
    }

    // Helper method to set outline color
    private void SetOutlineColor(Color color)
    {
        if (outline != null)
        {
            outline.effectColor = color;
        }
    }
}
