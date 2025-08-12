using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public MonsterData monsterData; // Reference to the ScriptableObject data

    // Main Attributes - These will be read from MonsterData
    [Header("Main Attributes")]
    [Range(1, 100)]
    public int strength; // Removed default value
    [Range(1, 100)]
    public int agility; // Removed default value
    [Range(1, 100)]
    public int constitution; // Removed default value
    [Range(1, 100)]
    public int spirit; // Removed default value
    [Range(1, 100)]
    public int accuracy; // Removed default value

    // Core Stats - These will be read from MonsterData
    [Header("Core Stats")]
    [Range(1, 100)]
    public int level;
    public float attackRange;
    public float movementSpeed;

    // Enemy Specific Radii - These will be read from MonsterData
    [Header("Enemy Radii")]
    public float hoveringRadius;
    public float chaseRadius;

    // Substats
    [Header("Calculated Substats")]
    public float minPhysicalDamage;
    public float maxPhysicalDamage;
    public float dodgePoints;
    public float attackSpeed;
    public float defense;
    public float maxHealth;
    public float currentHealth;
    public float healthRecoveryPerSecond;
    public float minMagicalDamage;
    public float maxMagicalDamage;
    public float maxMana;
    public float manaRecoveryPerSecond;
    public float rangedMinPhysicalDamage;
    public float rangedMaxPhysicalDamage;
    public float hitPoints;

    void OnValidate()
    {
        if (monsterData != null)
        {
            InitializeFromMonsterData();
            CalculateStats();
            currentHealth = maxHealth;
        }
    }

    void Start()
    {
        if (monsterData != null)
        {
            InitializeFromMonsterData();
        }
        CalculateStats();
        currentHealth = maxHealth;
    }

    void InitializeFromMonsterData()
    {
        strength = monsterData.strength;
        agility = monsterData.agility;
        constitution = monsterData.constitution;
        spirit = monsterData.spirit;
        accuracy = monsterData.accuracy;

        level = monsterData.level;
        attackRange = monsterData.attackRange;
        movementSpeed = monsterData.movementSpeed;

        hoveringRadius = monsterData.hoveringRadius;
        chaseRadius = monsterData.chaseRadius;
    }

    public void CalculateStats()
    {
        // Strength
        minPhysicalDamage = monsterData.GetMinPhysicalDamage();
        maxPhysicalDamage = monsterData.GetMaxPhysicalDamage();

        // Agility
        dodgePoints = monsterData.GetDodgePoints();
        attackSpeed = monsterData.GetAttackSpeed();

        // Constitution
        defense = monsterData.GetDefense();
        maxHealth = monsterData.GetMaxHealth();
        healthRecoveryPerSecond = monsterData.GetHealthRecoveryPerSecond();

        // Spirit
        minMagicalDamage = monsterData.GetMinMagicalDamage();
        maxMagicalDamage = monsterData.GetMaxMagicalDamage();
        maxMana = monsterData.GetMaxMana();
        manaRecoveryPerSecond = monsterData.GetManaRecoveryPerSecond();

        // Accuracy
        rangedMinPhysicalDamage = monsterData.GetRangedMinPhysicalDamage();
        rangedMaxPhysicalDamage = monsterData.GetRangedMaxPhysicalDamage();
        hitPoints = monsterData.GetHitPoints();

        // Ensure minimum values
        minPhysicalDamage = Mathf.Max(1f, minPhysicalDamage);
        maxPhysicalDamage = Mathf.Max(1f, maxPhysicalDamage);
        dodgePoints = Mathf.Max(0f, dodgePoints);
        attackSpeed = Mathf.Max(0.5f, attackSpeed);
        defense = Mathf.Max(0f, defense);
        maxHealth = Mathf.Max(1f, maxHealth);
        healthRecoveryPerSecond = Mathf.Max(0f, healthRecoveryPerSecond);
        minMagicalDamage = Mathf.Max(1f, minMagicalDamage);
        maxMagicalDamage = Mathf.Max(1f, maxMagicalDamage);
        maxMana = Mathf.Max(1f, maxMana);
        manaRecoveryPerSecond = Mathf.Max(0f, manaRecoveryPerSecond);
        rangedMinPhysicalDamage = Mathf.Max(1f, rangedMinPhysicalDamage);
        rangedMaxPhysicalDamage = Mathf.Max(1f, rangedMaxPhysicalDamage);
        hitPoints = Mathf.Max(0f, hitPoints);
    }
}
