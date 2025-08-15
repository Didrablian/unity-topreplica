using UnityEngine;
using System.Collections.Generic;

public class PlayerEquipment : MonoBehaviour
{
    // References to currently equipped items by slot
    [Header("Equipped Items")]
    public Item equippedHead;      
    public Item equippedChest;     
    public Item equippedLegs;      
    public Item equippedFeet;      
    public Item equippedHands;     
    public Item equippedMainHand;  
    public Item equippedOffHand;   
    public Item equippedTwoHanded; // If a two-handed weapon, main and off-hand should be null
    public Item equippedRing1;     
    public Item equippedRing2; // NEW: Ring2 slot
    public Item equippedNecklace;  
    public Item equippedFairy; // NEW: Fairy slot

    // Reference to EquipmentUIHandler
    public EquipmentUIHandler uiHandler; // NEW: Assign this in Inspector

    // References to player components
    private Stats playerStats;
    private PlayerInventory playerInventory;

    void Start()
    {
        playerStats = GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats component not found on the player! PlayerEquipment requires Stats.");
            enabled = false;
            return;
        }

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory component not found on the player! PlayerEquipment requires PlayerInventory.");
            enabled = false;
            return;
        }

        // Apply bonuses from any items initially assigned in the inspector
        ApplyAllEquippedBonuses();
    }

    // Method to equip an item
    public void EquipItem(Item itemToEquip)
    {
        if (itemToEquip == null) 
        {
            Debug.LogWarning("Attempted to equip a null item.");
            return;
        }
        if (itemToEquip.itemType != Item.ItemType.Weapon && itemToEquip.itemType != Item.ItemType.Armor) 
        {
            Debug.LogWarning($"Cannot equip {itemToEquip.itemName}: It's not a weapon or armor.");
            return;
        }

        // First, check if the player has the item in inventory
        if (!playerInventory.HasItem(itemToEquip))
        {
            Debug.LogWarning($"Cannot equip {itemToEquip.itemName}: Not found in inventory.");
            return;
        }

        // Get the slot for the item
        Item.EquipmentSlotType targetSlot = itemToEquip.equipSlot;
        if (targetSlot == Item.EquipmentSlotType.None) 
        {
            Debug.LogWarning($"Cannot equip {itemToEquip.itemName}: No equipment slot defined.");
            return;
        }

        // Handle unequipping current item in slot (if any)
        Item previouslyEquippedItem = GetEquippedItemInSlot(targetSlot);
        if (previouslyEquippedItem != null)
        {
            UnequipItem(targetSlot); // Unequip current item to make space
        }

        // Remove item from inventory, then equip it
        playerInventory.RemoveItem(itemToEquip); // Remove from inventory first
        SetEquippedItemInSlot(targetSlot, itemToEquip);
        ApplyAllEquippedBonuses(); // Re-calculate and apply all bonuses

        Debug.Log($"Equipped {itemToEquip.itemName} in {targetSlot} slot.");
        if (uiHandler != null) uiHandler.UpdateEquipmentUI(); // Update UI after equip
    }

    // Method to unequip an item
    public void UnequipItem(Item.EquipmentSlotType slotToUnequip)
    {
        Item itemToUnequip = GetEquippedItemInSlot(slotToUnequip);
        if (itemToUnequip == null)
        {
            Debug.LogWarning($"No item equipped in {slotToUnequip} slot to unequip.");
            return;
        }

        // Add item back to inventory
        playerInventory.AddItem(itemToUnequip);
        SetEquippedItemInSlot(slotToUnequip, null);
        ApplyAllEquippedBonuses(); // Re-calculate and apply all bonuses

        Debug.Log($"Unequipped {itemToUnequip.itemName} from {slotToUnequip} slot.");
        if (uiHandler != null) uiHandler.UpdateEquipmentUI(); // Update UI after unequip
    }

    // NEW: Validation method to check if an item can be equipped into a specific slot
    public bool CanEquipItem(Item item, Item.EquipmentSlotType targetSlotType)
    {
        if (item == null || targetSlotType == Item.EquipmentSlotType.None) return false;

        // Check if the item's equipSlot matches the target slot type
        if (item.equipSlot != targetSlotType) return false;

        // Additional checks can be added here:
        // - Player level requirements
        // - Class restrictions
        // - Stat requirements (e.g., requires X strength)

        return true;
    }

    // NEW: Validation method to check if two items can be swapped (or one can replace another)
    public bool CanSwapItems(Item itemToPlace, Item.EquipmentSlotType targetSlotType)
    {
        // Simplest check: itemToPlace must be equippable in the target slot.
        // More complex logic can involve checking if the item currently in targetSlot
        // can be put into the source slot (if it's a two-way swap).

        return CanEquipItem(itemToPlace, targetSlotType);
    }

    // Helper to get item in a specific slot (Changed to public)
    public Item GetEquippedItemInSlot(Item.EquipmentSlotType slot)
    {
        return slot switch
        {
            Item.EquipmentSlotType.Head => equippedHead,
            Item.EquipmentSlotType.Chest => equippedChest,
            Item.EquipmentSlotType.Legs => equippedLegs,
            Item.EquipmentSlotType.Feet => equippedFeet,
            Item.EquipmentSlotType.Hands => equippedHands,
            Item.EquipmentSlotType.MainHand => equippedMainHand,
            Item.EquipmentSlotType.OffHand => equippedOffHand,
            Item.EquipmentSlotType.TwoHanded => equippedTwoHanded,
            Item.EquipmentSlotType.Ring1 => equippedRing1, // Renamed from Ring
            Item.EquipmentSlotType.Ring2 => equippedRing2, // NEW: Ring2 slot
            Item.EquipmentSlotType.Necklace => equippedNecklace,
            Item.EquipmentSlotType.Fairy => equippedFairy, // NEW: Fairy slot
            _ => null,
        };
    }

    // Helper to set item in a specific slot
    private void SetEquippedItemInSlot(Item.EquipmentSlotType slot, Item item)
    {
        switch (slot)
        {
            case Item.EquipmentSlotType.Head: equippedHead = item; break;
            case Item.EquipmentSlotType.Chest: equippedChest = item; break;
            case Item.EquipmentSlotType.Legs: equippedLegs = item; break;
            case Item.EquipmentSlotType.Feet: equippedFeet = item; break;
            case Item.EquipmentSlotType.Hands: equippedHands = item; break;
            case Item.EquipmentSlotType.MainHand: equippedMainHand = item; break;
            case Item.EquipmentSlotType.OffHand: equippedOffHand = item; break;
            case Item.EquipmentSlotType.TwoHanded: equippedTwoHanded = item; break;
            case Item.EquipmentSlotType.Ring1: equippedRing1 = item; break; // Renamed from Ring
            case Item.EquipmentSlotType.Ring2: equippedRing2 = item; break; // NEW: Ring2 slot
            case Item.EquipmentSlotType.Necklace: equippedNecklace = item; break;
            case Item.EquipmentSlotType.Fairy: equippedFairy = item; break; // NEW: Fairy slot
            // Default case intentionally left out for explicit handling.
        }
        // Handle two-handed weapon: if equipped, clear main/off-hand.
        if (slot == Item.EquipmentSlotType.TwoHanded && item != null)
        {
            equippedMainHand = null;
            equippedOffHand = null;
        }
        // If equipping one-handed, clear two-handed slot.
        else if ((slot == Item.EquipmentSlotType.MainHand || slot == Item.EquipmentSlotType.OffHand) && item != null)
        {
            equippedTwoHanded = null;
        }
    }

    // Applies all bonuses from currently equipped items to player stats
    void ApplyAllEquippedBonuses()
    {
        if (playerStats == null) return;

        // First, reset all temporary bonuses applied by equipment
        playerStats.ResetEquipmentBonuses(); // This method will be added to Stats.cs

        // Then, apply bonuses from currently equipped items
        ApplyBonusesFromItem(equippedHead);
        ApplyBonusesFromItem(equippedChest);
        ApplyBonusesFromItem(equippedLegs);
        ApplyBonusesFromItem(equippedFeet);
        ApplyBonusesFromItem(equippedHands);
        ApplyBonusesFromItem(equippedMainHand);
        ApplyBonusesFromItem(equippedOffHand);
        ApplyBonusesFromItem(equippedTwoHanded);
        ApplyBonusesFromItem(equippedRing1);
        ApplyBonusesFromItem(equippedRing2); // NEW: Ring2 slot
        ApplyBonusesFromItem(equippedNecklace);
        ApplyBonusesFromItem(equippedFairy); // NEW: Fairy slot

        playerStats.CalculateStats(); // Recalculate player stats after applying bonuses
        playerStats.UpdateUI(); // Update UI to reflect new stats
    }

    // Helper to apply bonuses of a single item
    private void ApplyBonusesFromItem(Item item)
    {
        if (item == null) return;

        // Apply bonuses to temporary stats in playerStats
        // (These will be factored into CalculateStats() in Stats.cs)
        playerStats.tempMinPhysicalDamageBonus += item.minPhysicalDamageBonus;
        playerStats.tempMaxPhysicalDamageBonus += item.maxPhysicalDamageBonus;
        playerStats.tempDodgePointsBonus += item.dodgePointsBonus;
        playerStats.tempAttackSpeedBonus += item.attackSpeedBonus;
        playerStats.tempDefenseBonus += item.defenseBonus;
        playerStats.tempMaxHealthBonus += item.healthBonus; // Using healthBonus for maxHealthBonus
        playerStats.tempHealthRecoveryPerSecondBonus += item.healthRecoveryPerSecondBonus;
        playerStats.tempMinMagicalDamageBonus += item.minMagicalDamageBonus;
        playerStats.tempMaxMagicalDamageBonus += item.maxMagicalDamageBonus;
        playerStats.tempMaxManaBonus += item.manaBonus; // Using manaBonus for maxManaBonus
        playerStats.tempManaRecoveryPerSecondBonus += item.manaRecoveryPerSecondBonus;
        playerStats.tempRangedMinPhysicalDamageBonus += item.rangedMinPhysicalDamageBonus;
        playerStats.tempRangedMaxPhysicalDamageBonus += item.rangedMaxPhysicalDamageBonus;
        playerStats.tempHitPointsBonus += item.hitPointsBonus;

        // Also apply main attribute bonuses if you decide to have them on items
        // playerStats.tempStrengthBonus += item.strengthBonus;
        // ... etc
    }
}
