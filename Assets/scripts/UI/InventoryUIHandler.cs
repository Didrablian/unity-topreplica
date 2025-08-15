using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for event interfaces
using System.Collections.Generic; // For List

public class InventoryUIHandler : MonoBehaviour
{
    public GameObject inventoryPanel; // The main inventory UI panel
    public Transform itemsParent; // The parent transform for all inventory slots
    public GameObject inventorySlotPrefab; // Assign your InventorySlot prefab here

    public Tooltip tooltipManager; // Assign the Tooltip script here

    // New: Reference to PlayerEquipment for item comparison
    public PlayerEquipment playerEquipment;

    // List to hold references to all InventorySlot scripts
    public InventorySlot[] slots; // Changed from private to public
    public PlayerInventory playerInventory; // Changed from private to public

    void Start()
    {
        // Removed: playerInventory = GetComponentInParent<PlayerInventory>();
        // The playerInventory reference will now be assigned via the Inspector.
        if (playerInventory == null)
        {
            Debug.LogError("InventoryUIHandler: PlayerInventory reference is not assigned! Drag Player GameObject to this field.");
            return;
        }
        else
        {
            Debug.Log("InventoryUIHandler: PlayerInventory reference found.");
        }

        // Get all InventorySlot components from children
        slots = itemsParent.GetComponentsInChildren<InventorySlot>();
        Debug.Log($"InventoryUIHandler: Found {slots.Length} InventorySlot components under {itemsParent.name}.");

        // If no slots are pre-existing, dynamically create them (optional)
        // if (slots.Length == 0 && inventorySlotPrefab != null)
        // {
        //     Debug.LogWarning("No InventorySlot components found. Attempting to generate slots. Make sure your inventory slots have the InventorySlot script attached.");
        //     // Example: Create 20 slots dynamically
        //     for (int i = 0; i < 20; i++)
        //     {
        //         GameObject slotGO = Instantiate(inventorySlotPrefab, itemsParent);
        //         slotGO.name = "InventorySlot_" + i;
        //     }
        //     slots = itemsParent.GetComponentsInChildren<InventorySlot>(); // Re-populate after creation
        // }

        // Initial UI update based on player inventory
        UpdateInventoryUI();
    }

    // Call this whenever the underlying player inventory changes
    public void UpdateInventoryUI()
    {
        Debug.Log("InventoryUIHandler: UpdateInventoryUI called.");
        // Clear all slots first
        foreach (InventorySlot slot in slots)
        {
            slot.ClearSlot();
        }

        // Populate slots with items from player inventory
        for (int i = 0; i < playerInventory.items.Length && i < slots.Length; i++)
        {
            if (playerInventory.items[i] != null)
            {
                slots[i].AddItem(playerInventory.items[i]); // Pass the InventoryItem object
            }
            else
            {
                slots[i].ClearSlot(); // Ensure empty slots are explicitly cleared
            }
        }
    }

    // Called by InventorySlot when an item is dropped
    public void OnDropItem(InventoryItemDrag draggedItem, ISlot sourceSlot, InventorySlot dropSlot)
    {
        if (sourceSlot == null) 
        {
            draggedItem.ResetPosition(); // Reset if source is null (shouldn't happen with InventoryItemDrag logic)
            return;
        }

        Item itemToHandle = sourceSlot.GetItem();
        if (itemToHandle == null) 
        {
            draggedItem.ResetPosition(); // Reset if dragging an empty slot (should be prevented by drag logic)
            return;
        }

        // Case 1: Dropping an item from Inventory to another Inventory slot
        if (sourceSlot is InventorySlot inventorySourceSlot)
        {
            playerInventory.MoveItem(inventorySourceSlot, dropSlot); // This now handles internal data move/swap
            Destroy(draggedItem.gameObject); // NEW: Destroy the dragged clone on successful move/swap/stack
        }
        // Case 2: Dropping an item from Equipment to Inventory slot
        else if (sourceSlot is EquipmentSlot equipmentSourceSlot)
        {
            // Only allow unequip if the item is not null and inventory has space, and item can be placed in inventory
            // (i.e., not a quest item that can't be stored in inventory like this, etc.)
            if (itemToHandle != null && playerInventory.HasFreeSlot())
            {
                // Unequip from source slot and add to inventory
                equipmentSourceSlot.GetEquipmentUIHandler().playerEquipment.UnequipItem(itemToHandle.equipSlot); 
                // PlayerInventory.AddItem is called by UnequipItem internally, which will update InventoryUI
                Destroy(draggedItem.gameObject); // NEW: Destroy the dragged clone on successful unequip
            }
            else
            {
                Debug.LogWarning($"Cannot unequip {itemToHandle.itemName} to inventory: No free slot or item is null/unmovable.");
                draggedItem.ResetPosition(); // Reset position if cannot unequip
            }
        }

        // The UI update for inventory will be triggered after PlayerInventory.MoveItem/AddItem/RemoveItem
        // draggedItem.ResetPosition() is handled by the OnEndDrag for drag failures, or implicitly by UI updates.
        // With explicit Destroy(draggedItem.gameObject) for successes, the below comment is less relevant.
    }

    public void ShowItemTooltip(Item itemToShow)
    {
        if (tooltipManager == null) return; // Exit if no tooltip manager

        string content = $"<b>{itemToShow.itemName}</b>\n<size=11>{itemToShow.description}</size>\n\nItem Type: {itemToShow.itemType}";

        // Get the currently equipped item in the same slot for comparison, if applicable
        Item equippedItem = null;
        if (itemToShow.equipSlot != Item.EquipmentSlotType.None && playerEquipment != null)
        {
            equippedItem = playerEquipment.GetEquippedItemInSlot(itemToShow.equipSlot);
        }

        // Check for Shift key to show comparison
        bool showComparison = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (showComparison && equippedItem != null)
        {
            content += "\n\n--- Comparison (Equipped vs. New) ---";

            // Compare Physical Damage
            content += GetStatComparisonLine("Min Physical Damage", equippedItem.minPhysicalDamageBonus, itemToShow.minPhysicalDamageBonus);
            content += GetStatComparisonLine("Max Physical Damage", equippedItem.maxPhysicalDamageBonus, itemToShow.maxPhysicalDamageBonus);

            // Compare Dodge Points
            content += GetStatComparisonLine("Dodge Points", equippedItem.dodgePointsBonus, itemToShow.dodgePointsBonus);

            // Compare Attack Speed
            content += GetStatComparisonLine("Attack Speed", equippedItem.attackSpeedBonus, itemToShow.attackSpeedBonus, "F2");

            // Compare Defense
            content += GetStatComparisonLine("Defense", equippedItem.defenseBonus, itemToShow.defenseBonus, "F1");

            // Compare Health Bonus
            content += GetStatComparisonLine("Health Bonus", equippedItem.healthBonus, itemToShow.healthBonus, "F0");

            // Compare Health Recovery
            content += GetStatComparisonLine("Health Regen/s", equippedItem.healthRecoveryPerSecondBonus, itemToShow.healthRecoveryPerSecondBonus, "F2");

            // Compare Mana Bonus
            content += GetStatComparisonLine("Mana Bonus", equippedItem.manaBonus, itemToShow.manaBonus, "F0");

            // Compare Mana Recovery
            content += GetStatComparisonLine("Mana Regen/s", equippedItem.manaRecoveryPerSecondBonus, itemToShow.manaRecoveryPerSecondBonus, "F2");

            // Compare Ranged Physical Damage
            content += GetStatComparisonLine("Ranged Min Physical Damage", equippedItem.rangedMinPhysicalDamageBonus, itemToShow.rangedMinPhysicalDamageBonus);
            content += GetStatComparisonLine("Ranged Max Physical Damage", equippedItem.rangedMaxPhysicalDamageBonus, itemToShow.rangedMaxPhysicalDamageBonus);

            // Compare Hit Points
            content += GetStatComparisonLine("Hit Points", equippedItem.hitPointsBonus, itemToShow.hitPointsBonus, "F1");

            // Add other comparisons as needed based on your Item properties
        }
        else // No comparison or no equipped item, show regular details
        {
            // Add specific details based on item type
            if (itemToShow.itemType == Item.ItemType.Weapon)
            {
                content += $"\nDamage: {itemToShow.damageBonus:F1}";
                if (itemToShow.attackSpeedBonus > 0) content += $"\nAttack Speed: {itemToShow.attackSpeedBonus:F2}";
            }
            else if (itemToShow.itemType == Item.ItemType.Armor)
            {
                content += $"\nDefense: {itemToShow.defenseBonus:F1}";
                if (itemToShow.healthBonus > 0) content += $"\nHealth Bonus: {itemToShow.healthBonus:F0}";
                if (itemToShow.manaBonus > 0) content += $"\nMana Bonus: {itemToShow.manaBonus:F0}";
            }
            else if (itemToShow.itemType == Item.ItemType.Consumable)
            {
                if (itemToShow.healthRestoreAmount > 0) content += $"\nRestores Health: {itemToShow.healthRestoreAmount:F0}";
                if (itemToShow.manaRestoreAmount > 0) content += $"\nRestores Mana: {itemToShow.manaRestoreAmount:F0}";
            }

            // Add general bonuses if any
            if (itemToShow.minPhysicalDamageBonus > 0) content += $"\n+ {itemToShow.minPhysicalDamageBonus:F1} Min Physical Damage";
            if (itemToShow.maxPhysicalDamageBonus > 0) content += $"\n+ {itemToShow.maxPhysicalDamageBonus:F1} Max Physical Damage";
            if (itemToShow.dodgePointsBonus > 0) content += $"\n+ {itemToShow.dodgePointsBonus:F1} Dodge Points";
            if (itemToShow.healthRecoveryPerSecondBonus > 0) content += $"\n+ {itemToShow.healthRecoveryPerSecondBonus:F2} Health Regen/s";
            if (itemToShow.manaRecoveryPerSecondBonus > 0) content += $"\n+ {itemToShow.manaRecoveryPerSecondBonus:F2} Mana Regen/s";
            // Add other bonuses as needed
        }

        content += $"\n\nValue: {itemToShow.value} Gold";
        tooltipManager.ShowTooltip(content);
    }

    // Helper method for generating comparison lines
    private string GetStatComparisonLine(string statName, float equippedValue, float newValue, string format = "F1")
    {
        float difference = newValue - equippedValue;
        string colorTag = "<color=grey>"; // Neutral
        if (difference > 0) colorTag = "<color=green>";
        else if (difference < 0) colorTag = "<color=red>";

        if (difference == 0) // No change, just show the stat name without +/- or color
        {
            return $"\n{statName}: {newValue.ToString(format)}";
        }
        else
        {
            return $"\n{statName}: {newValue.ToString(format)} ({colorTag}{(difference > 0 ? "+" : "")}{difference.ToString(format)}</color>)";
        }
    }

    public void HideItemTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }
}
