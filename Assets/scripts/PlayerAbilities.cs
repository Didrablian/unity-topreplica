using UnityEngine;
using System.Collections.Generic;

public class PlayerAbilities : MonoBehaviour
{
    [Header("Ability Keybinds")]
    public AbilityData ability1;
    public AbilityData ability2;
    public AbilityData ability3;
    public AbilityData ability4;

    [Header("Ability Visuals")]
    public GameObject floatingDamageNumberPrefab; // Assign the FloatingDamageNumberPrefab here

    // Reference to player Stats script (for mana, etc.)
    private Stats playerStats;

    // Cooldown tracking for each ability slot
    private float ability1CooldownTimer = 0f;
    private float ability2CooldownTimer = 0f;
    private float ability3CooldownTimer = 0f;
    private float ability4CooldownTimer = 0f;

    // Reference to TopDownControls
    private TopDownControls topDownControls;

    void Start()
    {
        playerStats = GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats component not found on the player! PlayerAbilities requires Stats.");
            enabled = false; // Disable this script if Stats is not found
            return;
        }

        topDownControls = GetComponent<TopDownControls>();
        if (topDownControls == null)
        {
            Debug.LogError("TopDownControls component not found on the player! PlayerAbilities requires TopDownControls for input and targeting.");
            enabled = false; // Disable this script if TopDownControls is not found
            return;
        }
    }

    void Update()
    {
        // Update cooldown timers
        ability1CooldownTimer = Mathf.Max(0, ability1CooldownTimer - Time.deltaTime);
        ability2CooldownTimer = Mathf.Max(0, ability2CooldownTimer - Time.deltaTime);
        ability3CooldownTimer = Mathf.Max(0, ability3CooldownTimer - Time.deltaTime);
        ability4CooldownTimer = Mathf.Max(0, ability4CooldownTimer - Time.deltaTime);

        // Only allow ability casting if input is enabled via TopDownControls
        if (!topDownControls.IsInputEnabled) return;

        if (Input.GetKey(KeyCode.Alpha1)) // Use GetKey for hold-to-cast
        {
            CastAbility(ability1, ref ability1CooldownTimer);
        }
        if (Input.GetKey(KeyCode.Alpha2)) // Use GetKey for hold-to-cast
        {
            CastAbility(ability2, ref ability2CooldownTimer);
        }
        if (Input.GetKey(KeyCode.Alpha3)) // Use GetKey for hold-to-cast
        {
            CastAbility(ability3, ref ability3CooldownTimer);
        }
        if (Input.GetKey(KeyCode.Alpha4)) // Use GetKey for hold-to-cast
        {
            CastAbility(ability4, ref ability4CooldownTimer);
        }
    }

    void CastAbility(AbilityData ability, ref float cooldownTimer)
    {
        if (ability == null)
        {
            Debug.LogWarning("No ability assigned to this slot.");
            return;
        }

        if (cooldownTimer > 0)
        {
            Debug.Log($"{ability.abilityName} is on cooldown. Remaining: {cooldownTimer:F1}s");
            return;
        }

        if (playerStats.currentMana < ability.manaCost)
        {
            Debug.Log($"Not enough mana for {ability.abilityName}. Needs {ability.manaCost}, has {playerStats.currentMana}");
            return;
        }

        // Determine target based on ability type and current selection
        Transform targetTransform = null;
        Vector3? targetPoint = null; // Nullable Vector3 for point targets

        // Smart cast logic for target types that need one
        if (ability.targetType == AbilityData.TargetType.SingleTarget || ability.targetType == AbilityData.TargetType.AreaOfEffect || ability.targetType == AbilityData.TargetType.PointTarget)
        {
            // Try to use currently selected target first
            if (playerStats.currentTarget != null)
            {
                targetTransform = playerStats.currentTarget;
                Debug.Log($"Casting {ability.abilityName}: Using playerStats.currentTarget ({targetTransform.name}).");
            }
            else // No current target, attempt smart cast
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (ability.targetType == AbilityData.TargetType.SingleTarget || ability.targetType == AbilityData.TargetType.AreaOfEffect)
                    {
                        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                        {
                            // Only use this enemy if it's within the ability's range
                            if (Vector3.Distance(transform.position, hit.transform.position) <= ability.range)
                            {
                                targetTransform = hit.transform;
                                Debug.Log($"Casting {ability.abilityName}: Smart casting to enemy {targetTransform.name} under cursor.");
                            }
                            else
                            {
                                Debug.LogWarning($"Smart cast target {hit.transform.name} is out of {ability.abilityName} range ({ability.range} units).");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Smart cast hit non-enemy object: {hit.collider.gameObject.name}.");
                        }
                    }
                    else if (ability.targetType == AbilityData.TargetType.PointTarget)
                    {
                        // For point target, we just need the hit point on the ground
                        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                        {
                            targetPoint = hit.point;
                            Debug.Log($"Casting {ability.abilityName}: Smart casting to point {targetPoint} on ground.");
                        }
                        else
                        {
                            Debug.LogWarning($"Point target ability: Raycast hit non-ground object {hit.collider.gameObject.name}.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Smart cast: Raycast did not hit anything.");
                }
            }

            // NEW: Universal range check after target acquisition
            if (targetTransform != null && Vector3.Distance(transform.position, targetTransform.position) > ability.range)
            {
                Debug.LogWarning($"Casting {ability.abilityName}: Target {targetTransform.name} is out of range ({ability.range} units).");
                return; // Cancel cast if target is out of range
            }
            // For PointTarget abilities, also check range for the targetPoint
            if (targetPoint.HasValue && Vector3.Distance(transform.position, targetPoint.Value) > ability.range)
            {
                Debug.LogWarning($"Casting {ability.abilityName}: Target point {targetPoint.Value} is out of range ({ability.range} units).");
                return; // Cancel cast if point is out of range
            }

            // If after all targeting attempts, we still don't have a target, cancel cast
            if (targetTransform == null && targetPoint == null && ability.targetType != AbilityData.TargetType.Self && ability.targetType != AbilityData.TargetType.NoTarget)
            {
                Debug.LogWarning($"Casting {ability.abilityName}: No valid target found.");
                return; // Cancel cast if target is required but not found
            }
        }

        // All checks passed, cast the ability
        playerStats.currentMana -= ability.manaCost; // Deduct mana
        cooldownTimer = ability.cooldown; // Start cooldown ONLY here, after all checks pass

        Debug.Log($"Casting {ability.abilityName}! Mana remaining: {playerStats.currentMana}");
        playerStats.UpdateUI(); // Update UI to reflect mana change

        // Execute ability effect based on type and targeting
        ExecuteAbility(ability, targetTransform, targetPoint); // Pass target info to execute
    }

    // New method to calculate the final scaled value (damage or healing)
    private float CalculateScaledValue(List<AbilityData.AbilityScaling> scalings, float baseValue)
    {
        float totalScalingContribution = 0f;
        if (playerStats == null) return baseValue; // Return base if no stats

        foreach (var scaling in scalings)
        {
            float statValue = GetPlayerStatValue(scaling.statType);
            float contribution = 0f;

            if (scaling.operationType == AbilityData.ScalingOperationType.Additive)
            {
                contribution = statValue * scaling.scaleFactor;
            }
            else if (scaling.operationType == AbilityData.ScalingOperationType.Multiplicative)
            {
                // For multiplicative, the contribution is the stat value itself multiplied by the factor
                // This means if a stat is 100 and factor is 1.5, it contributes 150 to the total.
                contribution = statValue * scaling.scaleFactor;
            }
            totalScalingContribution += contribution;
        }
        return baseValue + totalScalingContribution;
    }

    // Helper method to get the current value of a player stat
    private float GetPlayerStatValue(AbilityData.ScalingStatType statType)
    {
        // This large switch statement maps ScalingStatType to actual stat values from playerStats.
        // It is crucial that this mapping is correct and covers all defined ScalingStatTypes.
        switch (statType)
        {
            case AbilityData.ScalingStatType.Strength: return playerStats.strength;
            case AbilityData.ScalingStatType.Agility: return playerStats.agility;
            case AbilityData.ScalingStatType.Constitution: return playerStats.constitution;
            case AbilityData.ScalingStatType.Spirit: return playerStats.spirit;
            case AbilityData.ScalingStatType.Accuracy: return playerStats.accuracy;

            case AbilityData.ScalingStatType.MinPhysicalDamage: return playerStats.minPhysicalDamage;
            case AbilityData.ScalingStatType.MaxPhysicalDamage: return playerStats.maxPhysicalDamage;
            case AbilityData.ScalingStatType.DodgePoints: return playerStats.dodgePoints;
            case AbilityData.ScalingStatType.AttackSpeed: return playerStats.attackSpeed;
            case AbilityData.ScalingStatType.Defense: return playerStats.defense;
            case AbilityData.ScalingStatType.MaxHealth: return playerStats.maxHealth;
            case AbilityData.ScalingStatType.HealthRecoveryPerSecond: return playerStats.healthRecoveryPerSecond;
            case AbilityData.ScalingStatType.MinMagicalDamage: return playerStats.minMagicalDamage;
            case AbilityData.ScalingStatType.MaxMagicalDamage: return playerStats.maxMagicalDamage;
            case AbilityData.ScalingStatType.MaxMana: return playerStats.maxMana;
            case AbilityData.ScalingStatType.ManaRecoveryPerSecond: return playerStats.manaRecoveryPerSecond;
            case AbilityData.ScalingStatType.RangedMinPhysicalDamage: return playerStats.rangedMinPhysicalDamage;
            case AbilityData.ScalingStatType.RangedMaxPhysicalDamage: return playerStats.rangedMaxPhysicalDamage;
            case AbilityData.ScalingStatType.HitPoints: return playerStats.hitPoints;

            default: return 0f; // Should not happen if enum is handled completely
        }
    }

    // Main method to execute the ability's effect
    void ExecuteAbility(AbilityData ability, Transform targetTransform = null, Vector3? targetPoint = null) // Add target parameters
    {
        Debug.Log($"Executing ability: {ability.abilityName} ({ability.abilityType})");

        switch (ability.abilityType)
        {
            case AbilityData.AbilityType.Damage:
                HandleDamageAbility(ability, targetTransform, targetPoint); // Pass target info
                break;
            case AbilityData.AbilityType.Heal:
                HandleHealAbility(ability, targetTransform); // Pass target info
                break;
            // Add cases for other ability types here
            case AbilityData.AbilityType.Buff:
                Debug.Log($"Ability Type: Buff - Not yet implemented: {ability.abilityName}");
                break;
            case AbilityData.AbilityType.Debuff:
                Debug.Log($"Ability Type: Debuff - Not yet implemented: {ability.abilityName}");
                break;
            case AbilityData.AbilityType.Movement:
                Debug.Log($"Ability Type: Movement - Not yet implemented: {ability.abilityName}");
                break;
            case AbilityData.AbilityType.Utility:
                Debug.Log($"Ability Type: Utility - Not yet implemented: {ability.abilityName}");
                break;
            case AbilityData.AbilityType.Summon:
                Debug.Log($"Ability Type: Summon - Not yet implemented: {ability.abilityName}");
                break;
        }
    }

    // Handles damage-type abilities
    void HandleDamageAbility(AbilityData ability, Transform targetTransform = null, Vector3? targetPoint = null) // Add target parameters
    {
        float finalDamage = CalculateScaledValue(ability.damageScalings, ability.baseDamageOrHealing);
        Debug.Log($"Calculated final damage for {ability.abilityName}: {finalDamage:F0}");

        // Targeting logic for damage abilities
        switch (ability.targetType)
        {
            case AbilityData.TargetType.SingleTarget:
                // Use the passed targetTransform (from playerStats.currentTarget or smart cast)
                if (ability.summonPrefab != null) // If there's a projectile prefab assigned
                {
                    if (targetTransform != null)
                    {
                        // Instantiate projectile from player position
                        GameObject projectileGO = Instantiate(ability.summonPrefab, transform.position + Vector3.up * 1f, Quaternion.identity); // Slightly above player
                        AbilityProjectile projectile = projectileGO.GetComponent<AbilityProjectile>();
                        if (projectile != null)
                        {
                            projectile.Initialize(targetTransform, finalDamage, ability.damageType, this.transform, floatingDamageNumberPrefab);
                        }
                        else
                        {
                            Debug.LogError($"Projectile prefab for {ability.abilityName} is missing AbilityProjectile component.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Single target ability with projectile: No target found for smart cast.");
                    }
                }
                else // No projectile prefab, apply damage directly (e.g., instant hit spell)
                {
                    if (targetTransform != null)
                    {
                        EnemyStats enemyStats = targetTransform.GetComponent<EnemyStats>();
                        if (enemyStats != null)
                        {
                            ApplyDamageToTarget(enemyStats, finalDamage, ability.damageType);
                        }
                        else
                        {
                            Debug.LogWarning("Single target ability: Target does not have EnemyStats component.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Single target ability: No target found for smart cast.");
                    }
                }
                break;
            case AbilityData.TargetType.AreaOfEffect:
                // For AoE, if a targetPoint is provided (from smart cast for point AoE), use it.
                // Otherwise, default to player's position (self-centered AoE).
                Vector3 aoeCenter = targetPoint ?? transform.position; // Use targetPoint if available, else player position
                HandleAreaOfEffectDamage(ability, finalDamage, aoeCenter);
                break;
            case AbilityData.TargetType.PointTarget:
                if (targetPoint.HasValue)
                {
                    Debug.Log($"PointTarget ability {ability.abilityName} cast at: {targetPoint.Value}");
                    // Here you would instantiate a ground effect, summon, etc.
                    // For now, just a log.
                }
                else
                {
                    Debug.LogWarning($"PointTarget ability: No valid point found for {ability.abilityName}.");
                }
                break;
            case AbilityData.TargetType.Self:
                Debug.LogWarning("Damage ability with Self target type. This is unusual. Damage player?");
                // You could potentially damage the player here, but typically not for damage abilities.
                break;
            case AbilityData.TargetType.NoTarget:
                Debug.LogWarning("Damage ability with NoTarget. This is unusual. Consider AoE if intended for area.");
                // For abilities that just happen without a target (e.g., toggle an aura)
                break;
        }
    }

    // Handles healing-type abilities
    void HandleHealAbility(AbilityData ability, Transform targetTransform = null) // Add target parameter
    {
        float finalHealing = CalculateScaledValue(ability.healingScalings, ability.baseDamageOrHealing);
        Debug.Log($"Calculated final healing for {ability.abilityName}: {finalHealing:F0}");

        // Healing abilities typically target self or allies.
        switch (ability.targetType)
        {
            case AbilityData.TargetType.Self:
                playerStats.currentHealth = Mathf.Min(playerStats.maxHealth, playerStats.currentHealth + finalHealing);
                playerStats.UpdateUI();
                Debug.Log($"Player healed for {finalHealing:F0}. Current Health: {playerStats.currentHealth:F0}");
                // Optionally show floating heal number over player
                break;
            case AbilityData.TargetType.SingleTarget:
                if (targetTransform != null)
                {
                    // Assuming this target is an ally with a Stats component
                    Stats targetStats = targetTransform.GetComponent<Stats>();
                    if (targetStats != null)
                    {
                        targetStats.currentHealth = Mathf.Min(targetStats.maxHealth, targetStats.currentHealth + finalHealing);
                        targetStats.UpdateUI(); // Update target's UI if it has one
                        Debug.Log($"Target {targetTransform.name} healed for {finalHealing:F0}. Current Health: {targetStats.currentHealth:F0}");
                        // Optionally show floating heal number over target
                    }
                    else
                    {
                        Debug.LogWarning($"Single target healing ability: Target {targetTransform.name} does not have Stats component.");
                    }
                }
                else
                {
                    Debug.LogWarning("Single target healing ability: No target found for smart cast.");
                }
                break;
            case AbilityData.TargetType.AreaOfEffect:
                // Implement AoE healing for allies
                Debug.LogWarning("AreaOfEffect healing for allies not implemented yet.");
                break;
            default:
                Debug.LogWarning($"Healing ability with unsupported target type: {ability.targetType}");
                break;
        }
    }

    // Applies damage to a target considering damage type
    void ApplyDamageToTarget(EnemyStats targetEnemy, float damageAmount, AbilityData.DamageType damageType)
    {
        float finalDamage = damageAmount;
        if (damageType == AbilityData.DamageType.Physical || damageType == AbilityData.DamageType.Magical)
        {
            // Subtract defense for Physical/Magical damage
            finalDamage = Mathf.Max(0, damageAmount - targetEnemy.defense); // Simple defense subtraction
            Debug.Log($"Damage type: {damageType}. Original: {damageAmount:F0}, After Defense: {finalDamage:F0}");
        }
        else if (damageType == AbilityData.DamageType.Pure)
        {
            // Pure damage ignores defense
            Debug.Log($"Damage type: Pure. Damage: {finalDamage:F0} (ignores defense)");
        }

        targetEnemy.TakeDamage(finalDamage, this.transform); // Pass player's transform as attacker
    }

    // Handles Area of Effect damage
    void HandleAreaOfEffectDamage(AbilityData ability, float damageAmount, Vector3 center) // Add center parameter
    {
        // For AoE, we need a center point. For now, let's use the player's position for simplicity.
        // In a real game, this might be a mouse click location or another defined point.
        // Removed: Vector3 center = transform.position; 

        Collider[] hitColliders = Physics.OverlapSphere(center, ability.areaRadius, LayerMask.GetMask("Enemy"));
        Debug.Log($"AoE ability {ability.abilityName} hit {hitColliders.Length} enemies within radius {ability.areaRadius} centered at {center}.");

        foreach (var hitCollider in hitColliders)
        {
            EnemyStats enemyStats = hitCollider.GetComponent<EnemyStats>();
            if (enemyStats != null)
            {
                ApplyDamageToTarget(enemyStats, damageAmount, ability.damageType);
            }
        }
    }
}
