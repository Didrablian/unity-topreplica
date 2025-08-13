using UnityEngine;
using TMPro; // Add this line for TextMeshPro

public class EnemyStats : MonoBehaviour
{
    public MonsterData monsterData; // Reference to the ScriptableObject data

    // UI Text References
    [Header("UI Text References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;

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
    public float currentMana; // Corrected: Removed extra 'float'
    public float manaRecoveryPerSecond;
    public float rangedMinPhysicalDamage;
    public float rangedMaxPhysicalDamage;
    public float hitPoints;

    // Add reference to EnemyAI
    private EnemyAI enemyAI;

    private float respawnTimer; // Timer for respawn
    private bool isDead = false;

    void OnValidate()
    {
        if (monsterData != null)
        {
            InitializeFromMonsterData();
            CalculateStats();
            currentHealth = maxHealth;
            if (currentMana == 0) currentMana = maxMana; // Initialize current mana if not set, as it's part of EnemyStats
            UpdateUI(); // Update UI in editor
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
        if (currentMana == 0) currentMana = maxMana; // Initialize current mana if not set
        UpdateUI(); // Update UI on game start

        // Get EnemyAI component
        enemyAI = GetComponent<EnemyAI>();
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
        // Ensure mana is initialized from MonsterData if available and relevant
        maxMana = monsterData.GetMaxMana();
        if (currentMana == 0) currentMana = maxMana;
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

        // Update UI after stats calculation
        UpdateUI();
    }

    // New method to update UI Text elements for enemy
    public void UpdateUI()
    {
        if (nameText != null) nameText.text = monsterData.monsterName; // Get name from MonsterData
        if (levelText != null) levelText.text = "Level: " + level.ToString();
        if (healthText != null) healthText.text = "Health: " + currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0"); // Format to 0 decimal places
        // If you decide to display mana, you'd add a manaText here
        // if (manaText != null) manaText.text = "Mana: " + currentMana.ToString("F0") + "/" + maxMana.ToString("F0");
    }

    // Method to apply damage to the enemy
    public void TakeDamage(float damageAmount, Transform attacker = null) // Add attacker parameter
    {
        if (isDead) return; // Don't take damage if already dead

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go below 0

        UpdateUI(); // Update the UI after taking damage

        if (currentHealth <= 0)
        {
            HandleDeath();
        }
        // If there's an attacker and EnemyAI component, take aggro
        else if (attacker != null && enemyAI != null)
        {
            enemyAI.TakeAggro(attacker);
        }
    }

    void HandleDeath()
    {
        if (isDead) return; // Prevent double death handling

        isDead = true;
        Debug.Log(monsterData.monsterName + " has been defeated! Starting respawn timer.");

        // Disable visual and interactive components
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null) mesh.enabled = false; // Disable renderer

        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null) navAgent.enabled = false;

        if (enemyAI != null) enemyAI.enabled = false; // Disable AI

        // Hide UI elements
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (levelText != null) levelText.gameObject.SetActive(false);
        if (healthText != null) healthText.gameObject.SetActive(false);

        respawnTimer = monsterData.respawnTime;

        // Drop loot
        DropLoot();
    }

    void DropLoot()
    {
        if (monsterData.worldItemPrefab == null)
        {
            Debug.LogWarning($"Monster {monsterData.monsterName} has no WorldItemPrefab assigned for loot drops.");
            return;
        }

        foreach (var itemDrop in monsterData.possibleDrops)
        {
            if (itemDrop.item == null)
            {
                Debug.LogWarning($"Monster {monsterData.monsterName} has a null Item in its possibleDrops list.");
                continue;
            }

            float randomChance = Random.value; // Generates a random float between 0.0 and 1.0
            if (randomChance <= itemDrop.dropChance)
            {
                int quantity = Random.Range(itemDrop.minQuantity, itemDrop.maxQuantity + 1); // +1 because max is exclusive
                if (quantity <= 0) continue;

                GameObject droppedItemGO = Instantiate(monsterData.worldItemPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity); // Slightly above ground
                WorldItem worldItem = droppedItemGO.GetComponent<WorldItem>();

                if (worldItem != null)
                {
                    worldItem.itemData = itemDrop.item; // Assign the Item ScriptableObject
                    // For now, WorldItem doesn't directly handle quantity, inventory will later.
                    Debug.Log($"Dropped {quantity} x {itemDrop.item.itemName} from {monsterData.monsterName}.");
                }
                else
                {
                    Debug.LogError($"WorldItemPrefab for {monsterData.monsterName} is missing WorldItem component.");
                }
            }
        }
    }

    void Update()
    {
        if (isDead)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0)
            {
                RespawnEnemy();
            }
        }
    }

    void RespawnEnemy()
    {
        isDead = false;
        currentHealth = maxHealth; // Restore health
        currentMana = maxMana; // Restore mana

        // Re-enable visual and interactive components
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null) mesh.enabled = true; // Re-enable renderer

        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null) 
        {
            navAgent.enabled = true;
            navAgent.Warp(enemyAI.initialPosition); // Move back to initial spawn point
            enemyAI.currentState = EnemyAI.EnemyState.Wandering; // Reset AI state
        }

        if (enemyAI != null) 
        {
            enemyAI.enabled = true;
            // Reset any specific AI state needed
            enemyAI.currentState = EnemyAI.EnemyState.Wandering;
        }

        // Show UI elements
        if (nameText != null) nameText.gameObject.SetActive(true);
        if (levelText != null) levelText.gameObject.SetActive(true);
        if (healthText != null) healthText.gameObject.SetActive(true);

        UpdateUI(); // Update UI after respawn
        Debug.Log(monsterData.monsterName + " has respawned!");
    }
}
