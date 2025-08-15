using UnityEngine;
using System.Collections.Generic;
using TMPro; // For Tooltip

public class EquipmentUIHandler : MonoBehaviour
{
    public Transform slotsParent; // The parent transform for all equipment slots
    public Tooltip tooltipManager; // Assign the Tooltip script here

    // Reference to PlayerEquipment
    public PlayerEquipment playerEquipment;

    // List to hold references to all EquipmentSlot scripts
    public EquipmentSlot[] slots;

    void Start()
    {
        // NEW: Ensure playerEquipment is assigned, otherwise try to find it.
        if (playerEquipment == null)
        {
            playerEquipment = GetComponentInParent<PlayerEquipment>(); // Try finding in parent
            if (playerEquipment == null)
            {
                // As a last resort, try finding it anywhere in the scene, though assigning manually is best.
                playerEquipment = FindFirstObjectByType<PlayerEquipment>(); // Changed from FindObjectOfType
                if (playerEquipment == null)
                {
                    Debug.LogError("EquipmentUIHandler: PlayerEquipment reference is NOT assigned and could not be found in scene. Drag Player GameObject to this field.");
                    enabled = false;
                    return;
                }
            }
            else
            {
                Debug.Log("EquipmentUIHandler: PlayerEquipment reference found via GetComponentInParent.");
            }
        }

        // Get all EquipmentSlot components from children
        slots = slotsParent.GetComponentsInChildren<EquipmentSlot>();
        Debug.Log($"EquipmentUIHandler: Found {slots.Length} EquipmentSlot components under {slotsParent.name}.");

        // Initial UI update based on player equipment
        UpdateEquipmentUI();
    }

    // Call this whenever the underlying player equipment changes
    public void UpdateEquipmentUI()
    {
        Debug.Log("EquipmentUIHandler: UpdateEquipmentUI called.");

        // Clear all slots first
        foreach (EquipmentSlot slot in slots)
        {
            slot.ClearSlot();
        }

        // Populate slots with equipped items from PlayerEquipment
        foreach (EquipmentSlot eslot in slots)
        {
            if (eslot == null) // NEW: Safeguard against null slots
            {
                Debug.LogWarning("EquipmentUIHandler: A null EquipmentSlot was found in the slots array. Skipping.");
                continue;
            }

            Item equippedItem = playerEquipment.GetEquippedItemInSlot(eslot.slotType);
            if (equippedItem != null)
            {
                eslot.EquipItemUI(equippedItem);
                Debug.Log($"EquipmentUIHandler: Populating {eslot.slotType} slot with {equippedItem.itemName}.");
            }
            else
            {
                eslot.ClearSlot();
                Debug.Log($"EquipmentUIHandler: {eslot.slotType} slot is empty.");
            }
        }
    }

    // Called by EquipmentSlot when an item is dropped onto it (from inventory or another equipment slot)
    public void OnDropItem(InventoryItemDrag draggedItem, ISlot sourceSlot, EquipmentSlot dropSlot)
    {
        if (sourceSlot == null) 
        {
            draggedItem.ResetPosition(); // Reset if source is null (shouldn't happen with current drag logic)
            return;
        }

        Item itemToHandle = sourceSlot.GetItem();
        if (itemToHandle == null) 
        {
            draggedItem.ResetPosition(); // Reset if dragging an empty slot (should be prevented by drag logic)
            return;
        }

        // Case 1: Dragging from an Inventory Slot to an Equipment Slot
        if (sourceSlot is InventorySlot inventorySourceSlot)
        {
            // Check if the item can be equipped into this specific slot type
            if (playerEquipment.CanEquipItem(itemToHandle, dropSlot.slotType))
            {
                // If there's an item in the drop slot, try to unequip it to inventory
                Item itemInDropSlot = dropSlot.GetItem();
                if (itemInDropSlot != null)
                {
                    // Check if inventory has space for the currently equipped item
                    if (inventorySourceSlot.GetInventoryUIHandler().playerInventory.HasFreeSlot())
                    {
                        playerEquipment.UnequipItem(dropSlot.slotType); // Unequips current item to inventory
                    }
                    else
                    {
                        Debug.LogWarning($"Inventory is full. Cannot unequip {itemInDropSlot.itemName} to make space for {itemToHandle.itemName}.");
                        draggedItem.ResetPosition(); // Reset if inventory is full
                        return;
                    }
                }

                // Remove from inventory and equip the new item
                inventorySourceSlot.GetInventoryUIHandler().playerInventory.RemoveItem(itemToHandle, 1); 
                playerEquipment.EquipItem(itemToHandle); 
                Destroy(draggedItem.gameObject); // NEW: Destroy the dragged clone on successful equip
            }
            else
            {
                Debug.LogWarning($"Cannot equip {itemToHandle.itemName} into {dropSlot.slotType} slot.");
                draggedItem.ResetPosition(); // Reset position if cannot equip
            }
        }
        // Case 2: Dragging from one Equipment Slot to another Equipment Slot
        else if (sourceSlot is EquipmentSlot equipmentSourceSlot)
        {
            Item itemInDropSlot = dropSlot.GetItem();

            // If dropping on itself, or if source and target are the same slot type (e.g., Ring1 to Ring1) and have same item (shouldn't be allowed)
            if (sourceSlot == (UnityEngine.Object)dropSlot || (itemInDropSlot != null && itemInDropSlot == (UnityEngine.Object)itemToHandle)) // Added explicit cast to UnityEngine.Object
            {
                draggedItem.ResetPosition();
                return;
            }

            // If dropping on an empty compatible slot
            if (itemInDropSlot == null)
            {
                if (playerEquipment.CanEquipItem(itemToHandle, dropSlot.slotType))
                {
                    playerEquipment.UnequipItem(equipmentSourceSlot.slotType); // Remove from source
                    playerEquipment.EquipItem(itemToHandle); // Equip to target
                    Destroy(draggedItem.gameObject); // NEW: Destroy the dragged clone on successful move
                }
                else
                {
                    Debug.LogWarning($"Cannot move {itemToHandle.itemName} into empty {dropSlot.slotType} slot.");
                    draggedItem.ResetPosition();
                }
            }
            // If dropping on an occupied slot (attempt to swap)
            else 
            {
                // Check if items are swappable (both fit each other's slots)
                bool sourceCanTakeDropItem = playerEquipment.CanEquipItem(itemInDropSlot, equipmentSourceSlot.slotType); // Can item in drop slot go to source slot?
                bool dropCanTakeSourceItem = playerEquipment.CanEquipItem(itemToHandle, dropSlot.slotType); // Can item from source slot go to drop slot?

                if (sourceCanTakeDropItem && dropCanTakeSourceItem)
                {
                    // Perform actual swap: unequip both, then equip each into the other's slot
                    playerEquipment.UnequipItem(equipmentSourceSlot.slotType); // Unequip source item
                    playerEquipment.UnequipItem(dropSlot.slotType); // Unequip target item

                    // Equip them in swapped positions
                    playerEquipment.EquipItem(itemInDropSlot); // Equip target item to source slot
                    playerEquipment.EquipItem(itemToHandle); // Equip source item to target slot
                    Destroy(draggedItem.gameObject); // NEW: Destroy the dragged clone on successful swap
                }
                else
                {
                    Debug.LogWarning($"Cannot swap {itemToHandle.itemName} and {itemInDropSlot.itemName} between slots.");
                    draggedItem.ResetPosition();
                }
            }
        }
        // Case 3: Dragging from Equipment Slot to Inventory Slot (handled by InventoryUIHandler.OnDropItem)
        // The dragged item is handled by InventoryUIHandler in this case, no action here.

        // Note: draggedItem.ResetPosition() is not explicitly called here for successful moves/swaps,
        // because PlayerEquipment.EquipItem/UnequipItem will trigger UpdateEquipmentUI,
        // which will then clear and repopulate slots, effectively resetting the visual item.
        // With explicit Destroy(draggedItem.gameObject), this comment is less relevant for successes.
    }

    // Tooltip for empty slots
    public void ShowEmptySlotTooltip(string slotName)
    {
        if (tooltipManager != null)
        {
            string content = $"<b>{slotName}</b>\n<size=11>This slot is empty.</size>";
            tooltipManager.ShowTooltip(content);
        }
    }

    // NEW: ShowItemTooltip for equipped items (similar to InventoryUIHandler's)
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
            content += GetStatComparisonLine("Ranged Max Physical Damage", equippedItem.rangedMaxPhysicalDamageBonus, itemToShow.rangedMaxPhysicalDamageBonus); // Corrected property name

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

    public void HideItemTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }

    // Helper method for generating comparison lines (Copied from InventoryUIHandler)
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
}
