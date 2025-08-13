using UnityEngine;
using TMPro; // Add this line for TextMeshPro
using UnityEngine.UI; // Add this line

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
    public float currentMana; // Add this line
    public float manaRecoveryPerSecond;
    public float rangedMinPhysicalDamage;
    public float rangedMaxPhysicalDamage;
    public float hitPoints; // Changed from hitRate

    // New: Temporary bonuses from equipped items
    [Header("Equipment Bonuses (Temporary)")]
    public float tempMinPhysicalDamageBonus = 0f;
    public float tempMaxPhysicalDamageBonus = 0f;
    public float tempDodgePointsBonus = 0f;
    public float tempAttackSpeedBonus = 0f;
    public float tempDefenseBonus = 0f;
    public float tempMaxHealthBonus = 0f;
    public float tempHealthRecoveryPerSecondBonus = 0f;
    public float tempMinMagicalDamageBonus = 0f;
    public float tempMaxMagicalDamageBonus = 0f;
    public float tempMaxManaBonus = 0f;
    public float tempManaRecoveryPerSecondBonus = 0f;
    public float tempRangedMinPhysicalDamageBonus = 0f;
    public float tempRangedMaxPhysicalDamageBonus = 0f;
    public float tempHitPointsBonus = 0f;
    // Add temporary bonuses for main attributes if items can boost them
    // public int tempStrengthBonus = 0;
    // ... etc.

    // UI Text References
    [Header("UI Text References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI classText; // New
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI attributePointsText; // New
    public TextMeshProUGUI strengthText; // New
    public TextMeshProUGUI agilityText; // New
    public TextMeshProUGUI constitutionText; // New
    public TextMeshProUGUI spiritText; // New
    public TextMeshProUGUI accuracyText; // New
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;

    // Button References
    [Header("Attribute Buttons")]
    public Button strengthAllocateButton; // New
    public Button strengthDeallocateButton; // New
    public Button agilityAllocateButton; // New
    public Button agilityDeallocateButton; // New
    public Button constitutionAllocateButton; // New
    public Button constitutionDeallocateButton; // New
    public Button spiritAllocateButton; // New
    public Button spiritDeallocateButton; // New
    public Button accuracyAllocateButton; // New
    public Button accuracyDeallocateButton; // New

    // UI Panel and Input Control Reference
    [Header("UI Panel & Input Control")]
    public GameObject statsPanel; // Assign your UI Stats Panel here
    public TopDownControls topDownControls; // Changed to public - Assign the player's TopDownControls script here!

    // Tooltip Reference
    [Header("Tooltip")]
    public Tooltip tooltipManager; // Assign your Tooltip script instance here

    // Added for attacking
    public Transform currentTarget; // The current target the player is attacking

    void OnValidate()
    {
        // Called in editor when script is loaded or a value is changed in the inspector
        CalculateStats();
        // Removed: if (currentHealth == 0) currentHealth = maxHealth; // Initialize current health only if not set
        // Removed: if (currentMana == 0) currentMana = maxMana; // Initialize current mana only if not set
        // Removed: UpdateUI(); // Update UI in editor (input toggle should only happen at runtime)
    }

    void Start()
    {
        CalculateStats();
        if (currentHealth == 0) currentHealth = maxHealth; // Initialize current health only if not set
        if (currentMana == 0) currentMana = maxMana; // Initialize current mana only if not set
        UpdateUI(); // Update UI on game start

        // Removed: topDownControls = FindObjectOfType<TopDownControls>();
        // This reference will now be assigned via the Inspector.

        // Attach button listeners
        strengthAllocateButton?.onClick.AddListener(AllocateStrength);
        strengthDeallocateButton?.onClick.AddListener(DeallocateStrength);
        agilityAllocateButton?.onClick.AddListener(AllocateAgility);
        agilityDeallocateButton?.onClick.AddListener(DeallocateAgility);
        constitutionAllocateButton?.onClick.AddListener(AllocateConstitution);
        constitutionDeallocateButton?.onClick.AddListener(DeallocateConstitution);
        spiritAllocateButton?.onClick.AddListener(AllocateSpirit);
        spiritDeallocateButton?.onClick.AddListener(DeallocateSpirit);
        accuracyAllocateButton?.onClick.AddListener(AllocateAccuracy);
        accuracyDeallocateButton?.onClick.AddListener(DeallocateAccuracy);
    }

    public void CalculateStats()
    {
        // Reset calculated stats (important for equipment bonuses to re-apply correctly)
        // Only reset the calculated stats that are influenced by attributes and equipment.

        // Strength
        minPhysicalDamage = (strength * 0.8f) + (level * 0.2f) + tempMinPhysicalDamageBonus;
        maxPhysicalDamage = (strength * 1.2f) + (level * 0.3f) + tempMaxPhysicalDamageBonus;

        // Agility
        dodgePoints = (agility * 0.5f) + (level * 0.1f) + tempDodgePointsBonus;
        attackSpeed = 1f + (agility * 0.02f) + tempAttackSpeedBonus; // Example: 1.0 is base, higher agility means faster attack

        // Constitution
        defense = (constitution * 0.6f) + (level * 0.15f) + tempDefenseBonus;
        maxHealth = 100f + (constitution * 5f) + (level * 10f) + tempMaxHealthBonus;
        healthRecoveryPerSecond = (constitution * 0.1f) + (level * 0.05f) + tempHealthRecoveryPerSecondBonus;

        // Spirit
        minMagicalDamage = (spirit * 0.7f) + (level * 0.2f) + tempMinMagicalDamageBonus;
        maxMagicalDamage = (spirit * 1.3f) + (level * 0.3f) + tempMaxMagicalDamageBonus;
        maxMana = 50f + (spirit * 3f) + (level * 5f) + tempMaxManaBonus;
        manaRecoveryPerSecond = (spirit * 0.15f) + (level * 0.07f) + tempManaRecoveryPerSecondBonus;

        // Accuracy
        rangedMinPhysicalDamage = (accuracy * 0.7f) + (level * 0.2f) + tempRangedMinPhysicalDamageBonus;
        rangedMaxPhysicalDamage = (accuracy * 1.1f) + (level * 0.3f) + tempRangedMaxPhysicalDamageBonus;
        hitPoints = (accuracy * 0.6f) + (level * 0.1f) + tempHitPointsBonus; // Changed calculation to be point-based

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

        // Update UI after stats calculation
        UpdateUI();
    }

    // New: Method to reset all temporary equipment bonuses
    public void ResetEquipmentBonuses()
    {
        tempMinPhysicalDamageBonus = 0f;
        tempMaxPhysicalDamageBonus = 0f;
        tempDodgePointsBonus = 0f;
        tempAttackSpeedBonus = 0f;
        tempDefenseBonus = 0f;
        tempMaxHealthBonus = 0f;
        tempHealthRecoveryPerSecondBonus = 0f;
        tempMinMagicalDamageBonus = 0f;
        tempMaxMagicalDamageBonus = 0f;
        tempMaxManaBonus = 0f;
        tempManaRecoveryPerSecondBonus = 0f;
        tempRangedMinPhysicalDamageBonus = 0f;
        tempRangedMaxPhysicalDamageBonus = 0f;
        tempHitPointsBonus = 0f;

        // If you add temp main attribute bonuses, reset them here too
        // tempStrengthBonus = 0;
    }

    public void GainExp(float expAmount)
    {
        currentExp += expAmount;
        if (currentExp >= maxExp)
        {
            LevelUp();
        }
        UpdateUI(); // Update UI after experience gain (if level doesn't change)
    }

    void LevelUp()
    {
        level++;
        attributePoints += 1; // Gain 1 attribute point per level

        if (level % 10 == 0) // Bonus points every 10 levels
        {
            attributePoints += 3; 
        }

        currentExp -= maxExp; // Carry over excess exp
        maxExp = CalculateNextLevelExp(level); // Recalculate maxExp based on new level

        // For now, just recalculate based on new level.
        CalculateStats(); // Recalculate stats based on new level, which will also call UpdateUI
        // You might also want to restore health/mana on level up
        currentHealth = maxHealth;
        currentMana = maxMana;
        UpdateUI(); // Ensure UI is updated after level up and health/mana restoration
    }

    void Update()
    {
        RegenerateHealthAndMana(); // Add this line to call regeneration every frame
    }

    void RegenerateHealthAndMana()
    {
        // Health regeneration
        if (currentHealth < maxHealth)
        {
            currentHealth += healthRecoveryPerSecond * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth); // Clamp to max health
        }

        // Mana regeneration
        if (currentMana < maxMana)
        {
            currentMana += manaRecoveryPerSecond * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana); // Clamp to max mana
        }

        // Only update UI if regeneration actually occurred to avoid unnecessary calls
        if (healthRecoveryPerSecond > 0 || manaRecoveryPerSecond > 0)
        {
            UpdateUI();
        }
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
            CalculateStats(); // Calls UpdateUI internally
        }
    }

    public void DeallocateStrength()
    {
        // Ensure strength doesn't go below its base value (e.g., 5)
        if (strength > 5) 
        {
            strength--;
            attributePoints++;
            CalculateStats();
        }
    }

    public void AllocateAgility()
    {
        if (attributePoints > 0)
        {
            agility++;
            attributePoints--;
            CalculateStats(); // Calls UpdateUI internally
        }
    }

    public void DeallocateAgility()
    {
        // Ensure agility doesn't go below its base value (e.g., 5)
        if (agility > 5) 
        {
            agility--;
            attributePoints++;
            CalculateStats();
        }
    }

    public void AllocateConstitution()
    {
        if (attributePoints > 0)
        {
            constitution++;
            attributePoints--;
            CalculateStats(); // Calls UpdateUI internally
        }
    }

    public void DeallocateConstitution()
    {
        // Ensure constitution doesn't go below its base value (e.g., 5)
        if (constitution > 5) 
        {
            constitution--;
            attributePoints++;
            CalculateStats();
        }
    }

    public void AllocateSpirit()
    {
        if (attributePoints > 0)
        {
            spirit++;
            attributePoints--;
            CalculateStats(); // Calls UpdateUI internally
        }
    }

    public void DeallocateSpirit()
    {
        // Ensure spirit doesn't go below its base value (e.g., 5)
        if (spirit > 5) 
        {
            spirit--;
            attributePoints++;
            CalculateStats();
        }
    }

    public void AllocateAccuracy()
    {
        if (attributePoints > 0)
        {
            accuracy++;
            attributePoints--;
            CalculateStats(); // Calls UpdateUI internally
        }
    }

    public void DeallocateAccuracy()
    {
        // Ensure accuracy doesn't go below its base value (e.g., 5)
        if (accuracy > 5) 
        {
            accuracy--;
            attributePoints++;
            CalculateStats();
        }
    }

    // New method to update UI Text elements
    public void UpdateUI()
    {
        if (nameText != null) nameText.text = "Didrablian"; // Name is constant as per request
        if (classText != null) classText.text = "Class: Warrior"; // Example Class, you might want to make this dynamic
        if (levelText != null) levelText.text = "Level: " + level.ToString();
        if (attributePointsText != null) attributePointsText.text = "Attribute Points: " + attributePoints.ToString();
        if (strengthText != null) strengthText.text = "Strength: " + strength.ToString();
        if (agilityText != null) agilityText.text = "Agility: " + agility.ToString();
        if (constitutionText != null) constitutionText.text = "Constitution: " + constitution.ToString();
        if (spiritText != null) spiritText.text = "Spirit: " + spirit.ToString();
        if (accuracyText != null) accuracyText.text = "Accuracy: " + accuracy.ToString();
        if (healthText != null) healthText.text = "Health: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0"); // Format to 0 decimal places
        if (manaText != null) manaText.text = "Mana: " + currentMana.ToString("F0") + "/" + maxMana.ToString("F0"); // Format to 0 decimal places

        // Manage game input based on stats panel visibility
        if (topDownControls != null && statsPanel != null)
        {
            topDownControls.SetInputEnabled(!statsPanel.activeInHierarchy);
        }
        else
        {
        }
    }

    // Tooltip Methods
    public void ShowStrengthTooltip()
    {
        if (tooltipManager != null)
        {
            string content = $"Strength increases physical damage. \nMin Physical Damage: {minPhysicalDamage:F1}\nMax Physical Damage: {maxPhysicalDamage:F1}";
            tooltipManager.ShowTooltip(content);
        }
    }

    public void HideStrengthTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }

    public void ShowAgilityTooltip()
    {
        if (tooltipManager != null)
        {
            string content = $"Agility increases dodge and attack speed. \nDodge Points: {dodgePoints:F1}\nAttack Speed: {attackSpeed:F2} attacks/sec";
            tooltipManager.ShowTooltip(content);
        }
    }

    public void HideAgilityTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }

    public void ShowConstitutionTooltip()
    {
        if (tooltipManager != null)
        {
            string content = $"Constitution increases health and defense. \nMax Health: {maxHealth:F0}\nDefense: {defense:F2}\nHealth Recovery: {healthRecoveryPerSecond:F2}/sec";
            tooltipManager.ShowTooltip(content);
        }
    }

    public void HideConstitutionTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }

    public void ShowSpiritTooltip()
    {
        if (tooltipManager != null)
        {
            string content = $"Spirit increases magical damage and mana. \nMin Magical Damage: {minMagicalDamage:F1}\nMax Magical Damage: {maxMagicalDamage:F1}\nMax Mana: {maxMana:F0}\nMana Recovery: {manaRecoveryPerSecond:F2}/sec";
            tooltipManager.ShowTooltip(content);
        }
    }

    public void HideSpiritTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }

    public void ShowAccuracyTooltip()
    {
        if (tooltipManager != null)
        {
            string content = $"Accuracy increases ranged physical damage and hit points. \nRanged Min Physical Damage: {rangedMinPhysicalDamage:F1}\nRanged Max Physical Damage: {rangedMaxPhysicalDamage:F1}\nHit Points: {hitPoints:F1}";
            tooltipManager.ShowTooltip(content);
        }
    }

    public void HideAccuracyTooltip()
    {
        if (tooltipManager != null) tooltipManager.HideTooltip();
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go below 0
        UpdateUI();
        if (currentHealth <= 0)
        {
            Debug.Log("Player has been defeated!");
            // Optionally, handle player death (e.g., game over screen, respawn)
        }
    }

    // New method for player to attack a target
    public void AttackTarget()
    {
        if (currentTarget != null)
        {
            EnemyStats enemyStats = currentTarget.GetComponent<EnemyStats>();
            if (enemyStats != null)
            {
                // Calculate player's damage (e.g., using minPhysicalDamage, maxPhysicalDamage)
                float damageToDeal = Random.Range(minPhysicalDamage, maxPhysicalDamage); // Example damage calculation
                enemyStats.TakeDamage(damageToDeal, this.transform); // Pass player's transform as attacker
                Debug.Log($"Player attacked {currentTarget.name} for {damageToDeal:F0} damage.");
            }
            else
            {
                Debug.LogWarning($"Target {currentTarget.name} does not have EnemyStats component.");
            }
        }
        else
        {
            Debug.Log("No target to attack.");
        }
    }

}
