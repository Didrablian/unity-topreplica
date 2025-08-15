using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for event interfaces
using TMPro; // Required for TextMeshProUGUI
using System.Collections; // Required for Coroutines

public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerMoveHandler, ISlot
{
    public Item.EquipmentSlotType slotType; // Defines what type of item this slot accepts
    public Image icon; // Reference to the UI Image component for the item icon
    public TextMeshProUGUI slotNameText; // To display the name of the slot when empty or for clarity
    public GameObject itemImagePrefab; // Prefab for the draggable item image (same as inventory)

    // The equipped item in this slot (this is itemData from PlayerEquipment)
    private Item equippedItem;

    private EquipmentUIHandler uiHandler; // Reference to the parent EquipmentUIHandler
    private GameObject currentItemImageGameObject; // To keep track of the instantiated item image

    // CanvasGroup for text to control raycasting
    private CanvasGroup slotNameCanvasGroup; 

    // Reference to the Outline component for visual feedback
    private Outline outline;

    // Coroutine reference for continuous tooltip update
    private Coroutine tooltipUpdateCoroutine;

    // Colors for highlighting (can be customized in Inspector)
    public Color normalColor = new Color(0.1f, 0.1f, 0.1f, 0.5f); 
    public Color highlightColor = new Color(0.9f, 0.9f, 0.1f, 0.8f); 
    public float outlineThickness = 2f; 

    // ISlot implementation
    public Item GetItem()
    {
        return equippedItem;
    }

    public InventoryUIHandler GetInventoryUIHandler()
    {
        return null; // Equipment slot doesn't have an InventoryUIHandler directly
    }

    public EquipmentUIHandler GetEquipmentUIHandler()
    {
        return uiHandler;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    void Awake()
    {
        uiHandler = GetComponentInParent<EquipmentUIHandler>();
        if (uiHandler == null)
        {
            Debug.LogError("EquipmentSlot: EquipmentUIHandler not found in parent for " + gameObject.name + ". Make sure EquipmentUIHandler is on a parent GameObject.");
        }

        // Get the Outline component
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            Debug.LogWarning("EquipmentSlot: Outline component not found on " + gameObject.name + ". Add an Outline UI component to this GameObject for visual highlighting.");
        }
        else
        {
            outline.effectDistance = new Vector2(outlineThickness, outlineThickness);
        }
        SetOutlineColor(normalColor); 

        // Get or add CanvasGroup to slotNameText and configure it for no raycast blocking
        if (slotNameText != null)
        {
            slotNameCanvasGroup = slotNameText.GetComponent<CanvasGroup>();
            if (slotNameCanvasGroup == null)
            {
                slotNameCanvasGroup = slotNameText.gameObject.AddComponent<CanvasGroup>();
            }
            slotNameCanvasGroup.blocksRaycasts = false; 
            slotNameCanvasGroup.interactable = false; 
        }
    }

    // Method to equip an item into this UI slot (visuals only)
    public void EquipItemUI(Item newItem)
    {
        equippedItem = newItem;
        if (equippedItem == null)
        {
            ClearSlot();
            return;
        }

        Debug.Log($"EquipmentSlot: EquipItemUI called for {gameObject.name} with item: {equippedItem.itemName}");

        // Clear any existing visual item
        if (currentItemImageGameObject != null)
        {
            Destroy(currentItemImageGameObject);
            currentItemImageGameObject = null;
        }

        // Instantiate new item image
        if (itemImagePrefab != null)
        {
            currentItemImageGameObject = Instantiate(itemImagePrefab, icon.transform); 
            Image itemImage = currentItemImageGameObject.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.sprite = equippedItem.icon;
                itemImage.enabled = true;
            }
            InventoryItemDrag itemDrag = currentItemImageGameObject.GetComponent<InventoryItemDrag>();
            if (itemDrag != null)
            {
                // This is important: Set the original slot to THIS equipment slot
                itemDrag.SetOriginalSlot(this); 
            }
            // NEW: Debugging sibling indices
            Debug.Log($"EquipmentSlot: Before SiblingIndex Adjustment - Slot ({gameObject.name}) children:\n  Icon: {icon.name} (Index: {icon.transform.GetSiblingIndex()})\n  SlotNameText: {(slotNameText != null ? slotNameText.name : "N/A")} (Index: {(slotNameText != null ? slotNameText.transform.GetSiblingIndex().ToString() : "N/A")})\n  ClonedItem: {currentItemImageGameObject.name} (Parent: {currentItemImageGameObject.transform.parent.name}, Index: {currentItemImageGameObject.transform.GetSiblingIndex()})");

            // Ensure item icon clone is rendered below the slot name text
            if (slotNameText != null)
            {
                slotNameText.transform.SetAsLastSibling(); 
                currentItemImageGameObject.transform.SetSiblingIndex(slotNameText.transform.GetSiblingIndex() - 1); 
            }
            // NEW: Debugging sibling indices after adjustment
            Debug.Log($"EquipmentSlot: After SiblingIndex Adjustment - Slot ({gameObject.name}) children:\n  Icon: {icon.name} (Index: {icon.transform.GetSiblingIndex()})\n  SlotNameText: {(slotNameText != null ? slotNameText.name : "N/A")} (Index: {(slotNameText != null ? slotNameText.transform.GetSiblingIndex().ToString() : "N/A")})\n  ClonedItem: {currentItemImageGameObject.name} (Parent: {currentItemImageGameObject.transform.parent.name}, Index: {currentItemImageGameObject.transform.GetSiblingIndex()})");

        }
        else
        {
            Debug.LogWarning("EquipmentSlot: itemImagePrefab is not assigned for " + gameObject.name + ". Fallback to directly setting icon sprite.");
            icon.sprite = equippedItem.icon;
            icon.enabled = true; 
        }

        // Hide slot name text when item is equipped
        if (slotNameText != null) slotNameText.enabled = false;
    }

    public void ClearSlot()
    {
        equippedItem = null;
        if (currentItemImageGameObject != null)
        {
            Destroy(currentItemImageGameObject);
            currentItemImageGameObject = null;
        }
        icon.sprite = null;
        icon.enabled = false; 
        SetOutlineColor(normalColor);

        // Show slot name text when slot is empty
        if (slotNameText != null) 
        {
            slotNameText.text = GetSlotNameDisplay();
            slotNameText.enabled = true; 
        }
    }

    // IDropHandler: Called when an item is dropped onto this slot
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            InventoryItemDrag draggedItem = eventData.pointerDrag.GetComponent<InventoryItemDrag>();
            if (draggedItem != null && uiHandler != null)
            {
                uiHandler.OnDropItem(draggedItem, draggedItem.GetOriginalSlot(), this); // Pass draggedItem, sourceSlot, and dropSlot
                SetOutlineColor(normalColor);
            }
        }
    }

    // IPointerClickHandler: For unequipping item on double-click
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"EquipmentSlot: OnPointerClick called on {gameObject.name}. Button: {eventData.button}, ClickCount: {eventData.clickCount}");

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            Debug.Log($"EquipmentSlot: Double-click detected on {gameObject.name}.");
            if (equippedItem != null && uiHandler != null && uiHandler.playerEquipment != null)
            {
                Debug.Log($"EquipmentSlot: Attempting to unequip item {equippedItem.itemName} from {slotType} via double-click.");
                uiHandler.playerEquipment.UnequipItem(slotType);
            }
            else
            {
                if (equippedItem == null) Debug.LogWarning($"EquipmentSlot: No item in slot {gameObject.name}. Cannot unequip.");
                if (uiHandler == null) Debug.LogWarning($"EquipmentSlot: UI Handler is null on {gameObject.name}. Cannot unequip.");
                if (uiHandler != null && uiHandler.playerEquipment == null) Debug.LogWarning($"EquipmentSlot: PlayerEquipment is null on UI Handler. Cannot unequip.");
            }
        }
    }

    // IPointerEnterHandler (for tooltip & highlighting)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip based on whether slot is empty or has an item
        if (uiHandler != null && uiHandler.tooltipManager != null)
        {
            if (equippedItem != null)
            {
                uiHandler.ShowItemTooltip(equippedItem); // Show item tooltip
            }
            else
            {
                // Show empty slot tooltip
                uiHandler.ShowEmptySlotTooltip(GetSlotNameDisplay());
            }

            // Start coroutine for dynamic tooltip update
            if (tooltipUpdateCoroutine != null) StopCoroutine(tooltipUpdateCoroutine);
            tooltipUpdateCoroutine = StartCoroutine(CheckShiftForTooltip());
        }
        
        // Highlight when dragging an item over this slot
        if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<InventoryItemDrag>() != null)
        {
            SetOutlineColor(highlightColor);
        }
    }

    // Coroutine for continuous tooltip update (same as InventorySlot)
    private IEnumerator CheckShiftForTooltip()
    {
        bool wasShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        yield return null; // Yield once for fresh input

        while (true)
        {
            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (isShiftPressed != wasShiftPressed)
            {
                if (uiHandler != null) // Check uiHandler for valid reference
                {
                    if (equippedItem != null)
                    {
                        uiHandler.ShowItemTooltip(equippedItem);
                    }
                    else
                    {
                        uiHandler.ShowEmptySlotTooltip(GetSlotNameDisplay());
                    }
                }
                wasShiftPressed = isShiftPressed;
            }
            yield return null; // Wait for the next frame
        }
    }

    // IPointerMoveHandler (Optional, mostly for consistency if any other logic needed)
    public void OnPointerMove(PointerEventData eventData)
    {
        // The continuous tooltip update is handled by CheckShiftForTooltip coroutine.
        // This method can be used for other logic if needed.
    }

    // IPointerExitHandler (for tooltip & highlighting)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiHandler != null)
        {
            uiHandler.HideItemTooltip();
        }
        SetOutlineColor(normalColor); 

        if (tooltipUpdateCoroutine != null)
        {
            StopCoroutine(tooltipUpdateCoroutine);
            tooltipUpdateCoroutine = null; 
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

    // Helper to get display name for empty slot
    private string GetSlotNameDisplay()
    {
        return slotType.ToString() + " Slot (Empty)";
    }
}
