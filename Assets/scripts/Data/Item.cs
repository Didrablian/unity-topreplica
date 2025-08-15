using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "ScriptableObjects/Item", order = 0)]
public class Item : ScriptableObject
{
    [Header("Basic Item Information")]
    public string itemName = "New Item";
    [TextArea(3, 5)]
    public string description = "Item description here.";
    public Sprite icon; // UI Icon for the item
    public int value = 1; // Gold value or general worth

    public enum ItemType { Generic, Weapon, Armor, Consumable, QuestItem, Material }
    [Header("Item Type")]
    public ItemType itemType = ItemType.Generic;

    public enum EquipmentSlotType { None, Head, Chest, Legs, Feet, Hands, MainHand, OffHand, TwoHanded, Ring1, Ring2, Necklace, Fairy }
    [Header("Equipment Properties")]
    public EquipmentSlotType equipSlot = EquipmentSlotType.None;
    public float damageBonus = 0f; // For weapons
    public float defenseBonus = 0f; // For armor
    public float attackSpeedBonus = 0f; // For weapons or certain items
    public float healthBonus = 0f; // For armor or accessories
    public float manaBonus = 0f; // For armor or accessories

    // New: Additional Substat Bonuses from Stats.cs
    public float minPhysicalDamageBonus = 0f;
    public float maxPhysicalDamageBonus = 0f;
    public float dodgePointsBonus = 0f;
    public float healthRecoveryPerSecondBonus = 0f;
    public float minMagicalDamageBonus = 0f;
    public float maxMagicalDamageBonus = 0f;
    public float manaRecoveryPerSecondBonus = 0f;
    public float rangedMinPhysicalDamageBonus = 0f;
    public float rangedMaxPhysicalDamageBonus = 0f;
    public float hitPointsBonus = 0f;

    [Header("Consumable Properties")]
    public float healthRestoreAmount = 0f;
    public float manaRestoreAmount = 0f;
    public float duration = 0f; // For temporary buffs from consumables
    // Add more specific effects for consumables if needed (e.g., stat buffs, debuff cleanse)

    [Header("Stacking Properties")]
    public bool isStackable = true;
    public int maxStackSize = 99;

    // You can add more properties here as your game evolves
    // For example:
    // public GameObject itemPrefab; // For visual representation when dropped in world
    // public Rarity rarity; // Enum for item rarity
    // public List<StatModifier> statModifiers; // For more complex stat changes
}

