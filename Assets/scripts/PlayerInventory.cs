using UnityEngine;
using System.Collections.Generic;

// New: A class to represent an item within the inventory, including its quantity
[System.Serializable] // Make it visible in the Inspector
public class InventoryItem
{
    public Item itemData; // Reference to the ScriptableObject Item
    public int quantity;  // Quantity of this item in the stack

    public InventoryItem(Item data, int qty)
    {
        itemData = data;
        quantity = qty;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] public InventoryItem[] items; // Array to hold inventory items with quantities
    public int inventorySize = 20; // Define the size of your inventory

    // New: Reference to the InventoryUIHandler
    public InventoryUIHandler uiHandler;

    // New: Reference to PlayerEquipment for testing equipping
    public PlayerEquipment playerEquipment;

    // New: Starting item for debugging
    public Item startingItem; // Drag your 'Sword' Item ScriptableObject here in Inspector

    void Awake()
    {
        items = new InventoryItem[inventorySize]; // Initialize the array with the defined size
    }

    void Start()
    {
        if (playerEquipment == null)
        {
            playerEquipment = GetComponent<PlayerEquipment>();
            if (playerEquipment == null)
            {
                // Removed: Debug.LogError("PlayerEquipment component not found on the player! Equipping via inventory will not work.");
            }
        }

        if (uiHandler == null)
        {
            uiHandler = FindFirstObjectByType<InventoryUIHandler>(); // Changed from FindObjectOfType
            if (uiHandler == null)
            {
                // Removed: Debug.LogError("InventoryUIHandler not found in scene.");
            }
        }
        // Update the UI immediately after initialization
        if (uiHandler != null)
        {
            uiHandler.UpdateInventoryUI();
        }

        // Add starting item if assigned for debugging
        if (startingItem != null)
        {
            AddItem(startingItem); // Add the sword to the first available slot
        }
    }

    public void AddItem(Item itemToAdd, int quantity = 1)
    {
        // Check for existing stackable items
        if (itemToAdd.isStackable)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].itemData == itemToAdd && items[i].quantity < itemToAdd.maxStackSize)
                {
                    int amountToAdd = Mathf.Min(quantity, itemToAdd.maxStackSize - items[i].quantity);
                    items[i].quantity += amountToAdd;
                    quantity -= amountToAdd;
                    if (uiHandler != null) uiHandler.UpdateInventoryUI();
                    if (quantity <= 0) return; // All items added
                }
            }
        }

        // Add remaining items to new slots if any quantity left or not stackable
        for (int i = 0; i < quantity; i++)
        {
            for (int j = 0; j < items.Length; j++)
            {
                if (items[j] == null)
                {
                    items[j] = new InventoryItem(itemToAdd, 1);
                    if (uiHandler != null) uiHandler.UpdateInventoryUI();
                    return; // Item added, exit
                }
            }
        }
    }

    public void RemoveItem(Item itemToRemove, int quantity = 1)
    {
        int removedCount = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemData == itemToRemove)
            {
                int amountToRemove = Mathf.Min(quantity, items[i].quantity);
                items[i].quantity -= amountToRemove;
                quantity -= amountToRemove;
                removedCount += amountToRemove;

                if (items[i].quantity <= 0)
                {
                    items[i] = null; // Clear slot if stack is empty
                }
                if (uiHandler != null) uiHandler.UpdateInventoryUI();
                if (quantity <= 0) break; // All requested items removed
            }
        }

        if (removedCount == 0 && quantity > 0)
        {
        }
    }

    // New: Method to move an item from a source slot to a destination slot
    public void MoveItem(InventorySlot sourceSlot, InventorySlot dropSlot)
    {
        int sourceIndex = System.Array.IndexOf(uiHandler.slots, sourceSlot);
        int dropIndex = System.Array.IndexOf(uiHandler.slots, dropSlot);

        if (sourceIndex == -1 || dropIndex == -1)
        {
            return;
        }

        InventoryItem sourceInventoryItem = items[sourceIndex];
        InventoryItem dropInventoryItem = items[dropIndex];

        // Case 1: Dropping an item onto an empty slot
        if (sourceInventoryItem != null && dropInventoryItem == null)
        {
            items[dropIndex] = sourceInventoryItem;
            items[sourceIndex] = null;
            // Debug.Log($"Moved {sourceInventoryItem.itemData.itemName} from slot {sourceIndex} to empty slot {dropIndex}.");
        }
        // Case 2: Dropping an item onto a slot with the same stackable item
        else if (sourceInventoryItem != null && dropInventoryItem != null && 
                 sourceInventoryItem.itemData == dropInventoryItem.itemData && 
                 sourceInventoryItem.itemData.isStackable)
        {
            int transferAmount = Mathf.Min(sourceInventoryItem.quantity, 
                                           dropInventoryItem.itemData.maxStackSize - dropInventoryItem.quantity);
            
            if (transferAmount > 0)
            {
                dropInventoryItem.quantity += transferAmount;
                sourceInventoryItem.quantity -= transferAmount;
                // Debug.Log($"Stacked {transferAmount} x {sourceInventoryItem.itemData.itemName} from slot {sourceIndex} to slot {dropIndex}.");

                if (sourceInventoryItem.quantity <= 0)
                {
                    items[sourceIndex] = null; // Clear source if stack is empty
                }
            }
            else
            {
                // If no transfer happened (e.g., target stack is full), swap items
                SwapItems(sourceIndex, dropIndex);
            }
        }
        // Case 3: Dropping an item onto a slot with a different item or non-stackable item
        else if (sourceInventoryItem != null && dropInventoryItem != null)
        {
            SwapItems(sourceIndex, dropIndex);
        }
        else // Both are null (dropping empty on empty), or dragging from an empty slot (should be prevented by InventoryItemDrag)
        {
            // Debug.Log("No action needed for move: both slots empty or invalid drag.");
            return;
        }

        // The UI update needs to be triggered AFTER the data change.
        if (uiHandler != null)
        {
            uiHandler.UpdateInventoryUI(); // Re-enabled: Update UI after moving for consistency
        }
    }

    // Helper method to swap items between two indices
    private void SwapItems(int index1, int index2)
    {
        InventoryItem temp = items[index1];
        items[index2] = items[index1];
        items[index1] = temp;
        // Debug.Log($"Swapped items between slot {index1} and slot {index2}.");
    }

    public bool HasItem(Item itemToCheck, int quantity = 1)
    {
        int count = 0;
        foreach (var inventoryItem in items)
        {
            if (inventoryItem != null && inventoryItem.itemData == itemToCheck)
            {
                count += inventoryItem.quantity;
            }
        }
        return count >= quantity;
    }

    // Helper to get total item count (now counts quantities)
    private int GetItemCount()
    {
        int count = 0;
        foreach (var inventoryItem in items)
        {
            if (inventoryItem != null)
            {
                count += inventoryItem.quantity;
            }
        }
        return count;
    }

    // NEW: Check if there's at least one empty slot in the inventory
    public bool HasFreeSlot()
    {
        foreach (var inventoryItem in items)
        {
            if (inventoryItem == null) return true;
        }
        return false;
    }

    // New: Method to use a consumable item
    public void UseItem(Item itemToUse)
    {
        if (itemToUse == null)
        {
            Debug.LogWarning("Attempted to use a null item.");
            return;
        }

        if (itemToUse.itemType != Item.ItemType.Consumable)
        {
            Debug.LogWarning($"Cannot use {itemToUse.itemName}: It's not a consumable item.");
            return;
        }

        // Check if the item is present in inventory with at least 1 quantity
        if (!HasItem(itemToUse, 1))
        {
            Debug.LogWarning($"Cannot use {itemToUse.itemName}: Not found in inventory or insufficient quantity.");
            return;
        }

        // Reference to player's Stats component
        Stats playerStats = GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats component not found on PlayerInventory's GameObject. Cannot use consumable effects.");
            return;
        }

        // Apply consumable effects
        if (itemToUse.healthRestoreAmount > 0)
        {
            playerStats.currentHealth = Mathf.Min(playerStats.maxHealth, playerStats.currentHealth + itemToUse.healthRestoreAmount);
            Debug.Log($"Used {itemToUse.itemName}: Restored {itemToUse.healthRestoreAmount:F0} Health. Current Health: {playerStats.currentHealth:F0}.");
        }
        if (itemToUse.manaRestoreAmount > 0)
        {
            playerStats.currentMana = Mathf.Min(playerStats.maxMana, playerStats.currentMana + itemToUse.manaRestoreAmount);
            Debug.Log($"Used {itemToUse.itemName}: Restored {itemToUse.manaRestoreAmount:F0} Mana. Current Mana: {playerStats.currentMana:F0}.");
        }

        // Remove item after use
        RemoveItem(itemToUse); // Quantity is 1 by default for RemoveItem
        playerStats.UpdateUI(); // Update UI to reflect health/mana changes

        Debug.Log($"Successfully used {itemToUse.itemName}.");
    }
}

