using UnityEngine;

public class Stats : MonoBehaviour
{
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

    // Attribute Points
    [Header("Attribute Points")]
    public int attributePoints = 0;

    // Core Stats
    [Header("Core Stats")]
    [Range(1, 100)]
    public int level = 1;
    public float currentExp = 0;
    public float maxExp = 100;
    public float attackRange = 2f;
    public float movementSpeed = 5f;

    // Substats
    [Header("Calculated Substats")]
    public float minPhysicalDamage;
    public float maxPhysicalDamage;
    public float dodgePoints;
    public float attackSpeed;
    public float defense;
    public float maxHealth;
    public float currentHealth; // Add this line
    public float healthRecoveryPerSecond;
    public float minMagicalDamage;
    public float maxMagicalDamage;
    public float maxMana;
    public float manaRecoveryPerSecond;
    public float rangedMinPhysicalDamage;
    public float rangedMaxPhysicalDamage;
    public float hitPoints; // Changed from hitRate

    void OnValidate()
    {
        // Called in editor when script is loaded or a value is changed in the inspector
        CalculateStats();
        currentHealth = maxHealth; // Initialize current health
    }

    void Start()
    {
        CalculateStats();
        currentHealth = maxHealth; // Initialize current health
    }

    public void CalculateStats()
    {
        // Strength
        minPhysicalDamage = (strength * 0.8f) + (level * 0.2f);
        maxPhysicalDamage = (strength * 1.2f) + (level * 0.3f);

        // Agility
        dodgePoints = (agility * 0.5f) + (level * 0.1f);
        attackSpeed = 1f + (agility * 0.02f); // Example: 1.0 is base, higher agility means faster attack

        // Constitution
        defense = (constitution * 0.6f) + (level * 0.15f);
        maxHealth = 100f + (constitution * 5f) + (level * 10f);
        healthRecoveryPerSecond = (constitution * 0.1f) + (level * 0.05f);

        // Spirit
        minMagicalDamage = (spirit * 0.7f) + (level * 0.2f);
        maxMagicalDamage = (spirit * 1.3f) + (level * 0.3f);
        maxMana = 50f + (spirit * 3f) + (level * 5f);
        manaRecoveryPerSecond = (spirit * 0.15f) + (level * 0.07f);

        // Accuracy
        rangedMinPhysicalDamage = (accuracy * 0.7f) + (level * 0.2f);
        rangedMaxPhysicalDamage = (accuracy * 1.1f) + (level * 0.3f);
        hitPoints = (accuracy * 0.6f) + (level * 0.1f); // Changed calculation to be point-based

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
        hitPoints = Mathf.Max(0f, hitPoints); // Ensure non-negative
    }

    public void GainExp(float expAmount)
    {
        currentExp += expAmount;
        if (currentExp >= maxExp)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        attributePoints += 1; // Gain 1 attribute point per level

        if (level % 10 == 0) // Bonus points every 10 levels
        {
            attributePoints += 3; 
            Debug.Log("Bonus 3 attribute points!");
        }

        currentExp -= maxExp; // Carry over excess exp
        maxExp = CalculateNextLevelExp(level); // Recalculate maxExp based on new level

        // For now, just recalculate based on new level.
        CalculateStats();
        Debug.Log("Leveled up to: " + level + ", Attribute Points: " + attributePoints);
    }

    float CalculateNextLevelExp(int currentLevel)
    {
        float baseExp = 100f;
        float difficultyMultiplier = 1.0f;

        int segment = (currentLevel - 1) / 10; // 0 for levels 1-10, 1 for 11-20, etc.
        difficultyMultiplier = 1.0f + (segment * 0.5f); // Increase difficulty by 50% per segment

        float expWithinSegment = (currentLevel % 10 == 0) ? 10 : (currentLevel % 10); // Adjust for level 10, 20 etc
        if (expWithinSegment == 0) expWithinSegment = 10; // If level is a multiple of 10, it's the 10th level of the segment
        
        // Minor increase within the segment
        float minorIncrease = 1.0f + (expWithinSegment * 0.05f); // 5% increase per level within segment

        return baseExp * difficultyMultiplier * minorIncrease;
    }

    // Methods to allocate attribute points
    public void AllocateStrength()
    {
        if (attributePoints > 0)
        {
            strength++;
            attributePoints--;
            CalculateStats();
            Debug.Log("Strength increased! Remaining attribute points: " + attributePoints);
        }
    }

    public void AllocateAgility()
    {
        if (attributePoints > 0)
        {
            agility++;
            attributePoints--;
            CalculateStats();
            Debug.Log("Agility increased! Remaining attribute points: " + attributePoints);
        }
    }

    public void AllocateConstitution()
    {
        if (attributePoints > 0)
        {
            constitution++;
            attributePoints--;
            CalculateStats();
            Debug.Log("Constitution increased! Remaining attribute points: " + attributePoints);
        }
    }

    public void AllocateSpirit()
    {
        if (attributePoints > 0)
        {
            spirit++;
            attributePoints--;
            CalculateStats();
            Debug.Log("Spirit increased! Remaining attribute points: " + attributePoints);
        }
    }

    public void AllocateAccuracy()
    {
        if (attributePoints > 0)
        {
            accuracy++;
            attributePoints--;
            CalculateStats();
            Debug.Log("Accuracy increased! Remaining attribute points: " + attributePoints);
        }
    }
}
