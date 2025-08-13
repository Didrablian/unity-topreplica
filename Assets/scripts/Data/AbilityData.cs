using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAbilityData", menuName = "ScriptableObjects/Ability Data", order = 2)]
public class AbilityData : ScriptableObject
{
    [Header("Basic Information")]
    public string abilityName = "New Ability";
    [TextArea(3, 5)]
    public string description = "Ability description here.";
    public Sprite icon; // UI Icon for the ability

    [Header("Core Mechanics")]
    public float cooldown = 5f; // Cooldown duration in seconds
    public float manaCost = 10f; // Mana cost of the ability
    public float castTime = 0f; // Time to cast the ability

    // New: Enum for stats that abilities can scale with
    public enum ScalingStatType
    {
        Strength, Agility, Constitution, Spirit, Accuracy, // Main attributes
        MinPhysicalDamage, MaxPhysicalDamage, DodgePoints, AttackSpeed, Defense, MaxHealth, HealthRecoveryPerSecond, MinMagicalDamage, MaxMagicalDamage, MaxMana, ManaRecoveryPerSecond, RangedMinPhysicalDamage, RangedMaxPhysicalDamage, HitPoints // Substats
    }

    // New: Enum for how the scaling factor is applied
    public enum ScalingOperationType
    {
        Additive, // Additive scaling (e.g., base + (stat * factor))
        Multiplicative // Multiplicative scaling (e.g., base * (stat * factor))
    }

    // New: Serializable class to define individual scaling entries
    [System.Serializable]
    public class AbilityScaling
    {
        public ScalingStatType statType; // Which stat this entry scales with
        public float scaleFactor = 1.0f; // The factor by which the stat contributes
        public ScalingOperationType operationType = ScalingOperationType.Additive; // How the scaling is applied
    }

    public enum AbilityType { Damage, Heal, Buff, Debuff, Movement, Utility, Summon }
    [Header("Ability Type")]
    public AbilityType abilityType;

    public enum TargetType { Self, SingleTarget, AreaOfEffect, PointTarget, NoTarget }
    [Header("Targeting")]
    public TargetType targetType;
    public float range = 10f; // Range of the ability
    public float areaRadius = 5f; // Radius for AoE abilities

    [Header("Damage / Healing Properties")]
    public DamageType damageType;
    public enum DamageType { Physical, Magical, Pure }
    
    [Header("Scaling for Damage/Healing")]
    public float baseDamageOrHealing = 0f; // Base value before scaling
    public List<AbilityScaling> damageScalings = new List<AbilityScaling>();
    public List<AbilityScaling> healingScalings = new List<AbilityScaling>();

    [Header("Buff / Debuff Properties")]
    public float duration = 5f; // Duration of buff/debuff
    public float effectMagnitude = 0.1f; // Magnitude of the effect (e.g., 0.1 for 10%)
    public StatAffected statAffected;
    public enum StatAffected { Strength, Agility, Constitution, Spirit, Accuracy, MovementSpeed, AttackSpeed, Defense, HealthRecovery, ManaRecovery }

    [Header("Movement Ability Properties")]
    public float moveDistance = 5f; // For movement abilities like dashes

    [Header("Summon Ability Properties")]
    public GameObject summonPrefab; // Prefab to summon
    public float summonDuration = 30f; // Duration the summoned entity lasts
    public int numberOfSummons = 1; // Number of entities to summon
}
