using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<Item> items = new List<Item>(); // List to hold all items in inventory

    // New: Reference to PlayerEquipment for testing equipping
    public PlayerEquipment playerEquipment;

    // Optional: for stacking items, you might use a more complex structure like:
    // public Dictionary<Item, int> itemQuantities = new Dictionary<Item, int>();

    void Start()
    {
        Debug.Log("PlayerInventory initialized.");

        if (playerEquipment == null)
        {
            playerEquipment = GetComponent<PlayerEquipment>();
            if (playerEquipment == null)
            {
                Debug.LogError("PlayerEquipment component not found on the player! Equipping via inventory will not work.");
            }
        }
    }

    void Update()
    {
        // Debugging: Press 'Q' to try equipping the first item in inventory
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (playerEquipment != null)
            {
                if (items.Count > 0)
                {
                    Item itemToEquip = items[0]; // Get the first item
                    playerEquipment.EquipItem(itemToEquip);
                }
                else
                {
                    // Removed: Debug.Log("Inventory is empty. Cannot equip any item.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerEquipment reference is null. Cannot test equipping.");
            }
        }

        // Removed: LogAllItems(); // Keep logging all items for constant debug visibility
    }

    public void AddItem(Item itemToAdd, int quantity = 1)
    {
        // For now, just add the item to the list. Stacking logic can be added later.
        for (int i = 0; i < quantity; i++)
        {
            items.Add(itemToAdd);
        }
        Debug.Log($"Added {quantity} x {itemToAdd.itemName} to inventory. Total items: {items.Count}");
        // You would typically update UI here if it existed.
    }

    public void RemoveItem(Item itemToRemove, int quantity = 1)
    {
        int removedCount = 0;
        for (int i = 0; i < quantity; i++)
        {
            if (items.Remove(itemToRemove))
            {
                removedCount++;
            }
            else
            {
                Debug.LogWarning($"Attempted to remove {itemToRemove.itemName} but not enough found in inventory.");
                break;
            }
        }
        if (removedCount > 0)
        {
            Debug.Log($"Removed {removedCount} x {itemToRemove.itemName} from inventory. Total items: {items.Count}");
        }
        // You would typically update UI here if it existed.
    }

    public bool HasItem(Item itemToCheck, int quantity = 1)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item == itemToCheck)
            {
                count++;
            }
        }
        return count >= quantity;
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

        if (!HasItem(itemToUse, 1))
        {
            Debug.LogWarning($"Cannot use {itemToUse.itemName}: Not found in inventory.");
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
        LogAllItems(); // Update inventory debug log

        Debug.Log($"Successfully used {itemToUse.itemName}.");
    }

    // For debug logging all items currently in inventory
    public void LogAllItems()
    {
        Debug.Log("--- Current Inventory ---");
        if (items.Count == 0)
        {
            Debug.Log("Inventory is empty.");
            return;
        }
        foreach (var item in items)
        {
            Debug.Log($"- {item.itemName}");
        }
        Debug.Log("-------------------------");
    }
}
