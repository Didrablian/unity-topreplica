using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "ScriptableObjects/Monster Data", order = 1)]
public class MonsterData : ScriptableObject
{
    [Header("Monster Identification")]
    public string monsterName = "New Monster";

    // Main Attributes
    [Header("Main Attributes")]
    [Range(1, 100)]
    public int strength = 10;
    [Range(1, 100)]
    public int agility = 10;
    [Range(1, 100)]
    public int constitution = 10;
    [Range(1, 100)]
    public int spirit = 10;
    [Range(1, 100)]
    public int accuracy = 10;

    // Core Stats
    [Header("Core Stats")]
    [Range(1, 100)]
    public int level = 1;
    public float attackRange = 2f;
    public float movementSpeed = 5f;

    // Enemy Specific Radii
    [Header("Enemy Radii")]
    public float hoveringRadius = 5f;
    public float chaseRadius = 10f;

    // Substats (calculated from attributes, not directly set in data table)
    // These would typically be calculated by the EnemyStats script that uses this data
    // public float minPhysicalDamage;
    // public float maxPhysicalDamage;
    // public float dodgePoints;
    // public float attackSpeed;
    // public float float defense;
    // public float maxHealth;
    // public float healthRecoveryPerSecond;
    // public float minMagicalDamage;
    // public float maxMagicalDamage;
    // public float maxMana;
    // public float manaRecoveryPerSecond;
    // public float rangedMinPhysicalDamage;
    // public float rangedMaxPhysicalDamage;
    // public float hitPoints;

    // Note: Substats are typically calculated by the runtime script (e.g., EnemyStats)
    // based on the raw attributes provided in this data table.
    // We are not adding the CalculateStats() method here, as this is purely for data definition.

    public float GetMinPhysicalDamage() { return (strength * 0.8f) + (level * 0.2f); }
    public float GetMaxPhysicalDamage() { return (strength * 1.2f) + (level * 0.3f); }
    public float GetDodgePoints() { return (agility * 0.5f) + (level * 0.1f); }
    public float GetAttackSpeed() { return 1f + (agility * 0.02f); }
    public float GetDefense() { return (constitution * 0.6f) + (level * 0.15f); }
    public float GetMaxHealth() { return 100f + (constitution * 5f) + (level * 10f); }
    public float GetHealthRecoveryPerSecond() { return (constitution * 0.1f) + (level * 0.05f); }
    public float GetMinMagicalDamage() { return (spirit * 0.7f) + (level * 0.2f); }
    public float GetMaxMagicalDamage() { return (spirit * 1.3f) + (level * 0.3f); }
    public float GetMaxMana() { return 50f + (spirit * 3f) + (level * 5f); }
    public float GetManaRecoveryPerSecond() { return (spirit * 0.15f) + (level * 0.07f); }
    public float GetRangedMinPhysicalDamage() { return (accuracy * 0.7f) + (level * 0.2f); }
    public float GetRangedMaxPhysicalDamage() { return (accuracy * 1.1f) + (level * 0.3f); }
    public float GetHitPoints() { return (accuracy * 0.6f) + (level * 0.1f); }
}
