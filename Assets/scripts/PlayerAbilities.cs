using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // New: Required for EventTrigger

public class PlayerAbilities : MonoBehaviour
{
    [Header("Ability Keybinds")]
    public AbilityData ability1;
    public AbilityData ability2;
    public AbilityData ability3;
    public AbilityData ability4;

    [Header("Ability Visuals")]
    public GameObject floatingDamageNumberPrefab; // Assign the FloatingDamageNumberPrefab here

    [Header("Hotbar UI References")]
    public Image ability1Icon;
    public TextMeshProUGUI ability1KeybindText;
    public TextMeshProUGUI ability1CooldownText;

    public Image ability2Icon;
    public TextMeshProUGUI ability2KeybindText;
    public TextMeshProUGUI ability2CooldownText;

    public Image ability3Icon;
    public TextMeshProUGUI ability3KeybindText;
    public TextMeshProUGUI ability3CooldownText;

    public Image ability4Icon;
    public TextMeshProUGUI ability4KeybindText;
    public TextMeshProUGUI ability4CooldownText;

    // Reference to player Stats script (for mana, etc.)
    private Stats playerStats;

    // Cooldown tracking for each ability slot
    private float ability1CooldownTimer = 0f;
    private float ability2CooldownTimer = 0f;
    private float ability3CooldownTimer = 0f;
    private float ability4CooldownTimer = 0f;

    // Reference to TopDownControls
    private TopDownControls topDownControls;

    // New: Reference to the Tooltip script
    public Tooltip abilityTooltip;

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

        // New: Programmatically ensure EventSystem exists
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("EventSystem created programmatically.");
        }

        // New: Ensure Graphic Raycaster is on the parent Canvas of the ability icons
        if (ability1Icon != null)
        {
            Canvas parentCanvas = ability1Icon.canvas;
            if (parentCanvas != null)
            {
                if (parentCanvas.GetComponent<GraphicRaycaster>() == null)
                {
                    parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"GraphicRaycaster added to Canvas: {parentCanvas.name}");
                }
            }
            else
            {
                Debug.LogWarning("Ability icon's parent Canvas not found. Cannot add GraphicRaycaster programmatically.");
            }
        }

        if (abilityTooltip == null)
        {
            Debug.LogError("Ability Tooltip reference is not assigned in PlayerAbilities.cs!");
        }

        // Setup UI event listeners for tooltips
        SetupAbilitySlotEvents(ability1, ability1Icon);
        SetupAbilitySlotEvents(ability2, ability2Icon);
        SetupAbilitySlotEvents(ability3, ability3Icon);
        SetupAbilitySlotEvents(ability4, ability4Icon);

        // NEW: Verify ability icon assignments at Start
        Debug.Log($"Ability1 Icon assigned: {ability1Icon != null}");
        if (ability1Icon != null) Debug.Log($"Ability1 Icon Name: {ability1Icon.name}");

        Debug.Log($"Ability2 Icon assigned: {ability2Icon != null}");
        if (ability2Icon != null) Debug.Log($"Ability2 Icon Name: {ability2Icon.name}");

        Debug.Log($"Ability3 Icon assigned: {ability3Icon != null}");
        if (ability3Icon != null) Debug.Log($"Ability3 Icon Name: {ability3Icon.name}");

        Debug.Log($"Ability4 Icon assigned: {ability4Icon != null}");
        if (ability4Icon != null) Debug.Log($"Ability4 Icon Name: {ability4Icon.name}");
    }

    void Update()
    {
        // Update cooldown timers
        ability1CooldownTimer = Mathf.Max(0, ability1CooldownTimer - Time.deltaTime);
        ability2CooldownTimer = Mathf.Max(0, ability2CooldownTimer - Time.deltaTime);
        ability3CooldownTimer = Mathf.Max(0, ability3CooldownTimer - Time.deltaTime);
        ability4CooldownTimer = Mathf.Max(0, ability4CooldownTimer - Time.deltaTime);

        // Update UI for all ability slots
        UpdateAbilitySlotUI(ability1, ability1Icon, ability1KeybindText, ability1CooldownText, ability1CooldownTimer, KeyCode.Alpha1);
        UpdateAbilitySlotUI(ability2, ability2Icon, ability2KeybindText, ability2CooldownText, ability2CooldownTimer, KeyCode.Alpha2);
        UpdateAbilitySlotUI(ability3, ability3Icon, ability3KeybindText, ability3CooldownText, ability3CooldownTimer, KeyCode.Alpha3);
        UpdateAbilitySlotUI(ability4, ability4Icon, ability4KeybindText, ability4CooldownText, ability4CooldownTimer, KeyCode.Alpha4);

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

    // New method to setup event triggers for ability slots
    private void SetupAbilitySlotEvents(AbilityData ability, Image iconImage)
    {
        if (iconImage == null || abilityTooltip == null) {
            Debug.LogWarning($"SetupAbilitySlotEvents: Missing iconImage or abilityTooltip. Icon: {(iconImage != null ? iconImage.name : "NULL")}, Tooltip: {(abilityTooltip != null ? abilityTooltip.name : "NULL")}");
            return;
        }

        Debug.Log($"SetupAbilitySlotEvents: Processing icon: {iconImage.name}");

        // Ensure the iconImage has a CanvasGroup to prevent raycast blocking if not present
        CanvasGroup canvasGroup = iconImage.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = iconImage.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = true; // Ensure it blocks raycasts for events
        canvasGroup.interactable = true; // Ensure it's interactable

        // Ensure the Image component itself has raycastTarget enabled
        if (iconImage != null)
        {
            iconImage.raycastTarget = true;
        }

        EventTrigger trigger = iconImage.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = iconImage.gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing triggers to prevent duplicates if Start is called multiple times (though it shouldn't be)
        trigger.triggers.Clear();

        // Pointer Enter event
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { 
            Debug.Log($"PointerEnter detected for ability: {(ability != null ? ability.abilityName : "NULL")}");
            OnPointerEnterAbilitySlot(ability); 
        });
        trigger.triggers.Add(entryEnter);

        // Pointer Exit event
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { 
            Debug.Log("PointerExit detected.");
            OnPointerExitAbilitySlot(); 
        });
        trigger.triggers.Add(entryExit);

        Debug.Log($"EventTriggers added to {iconImage.name} for PointerEnter and PointerExit.");
    }

    // Event handler for when mouse enters an ability slot
    private void OnPointerEnterAbilitySlot(AbilityData ability)
    {
        Debug.Log($"OnPointerEnterAbilitySlot called for ability: {(ability != null ? ability.abilityName : "NULL")}");
        if (abilityTooltip != null && ability != null)
        {
            string tooltipContent = GenerateAbilityTooltipContent(ability);
            abilityTooltip.ShowTooltip(tooltipContent);
        }
    }

    // Event handler for when mouse exits an ability slot
    private void OnPointerExitAbilitySlot()
    {
        Debug.Log("OnPointerExitAbilitySlot called.");
        if (abilityTooltip != null)
        {
            abilityTooltip.HideTooltip();
        }
    }

    // Generates the tooltip content string for an ability
    private string GenerateAbilityTooltipContent(AbilityData ability)
    {
        if (ability == null) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("<size=120%><color=#FFFF00>" + ability.abilityName + "</color></size>");
        sb.AppendLine(ability.description);
        sb.AppendLine($"\n<color=#00FFFF>Mana Cost: {ability.manaCost:F0}</color>");
        sb.AppendLine($"<color=#FF00FF>Cooldown: {ability.cooldown:F1}s</color>");

        // Add damage/healing info if applicable
        if (ability.abilityType == AbilityData.AbilityType.Damage)
        {
            float minDamage = ability.baseDamageOrHealing; // Simplistic, actual min/max would come from player stats + scaling
            float maxDamage = ability.baseDamageOrHealing; // Will be refined with proper scaling calculation

            if (ability.damageScalings.Count > 0)
            {
                // For tooltip, we can just show the base + an indicator of scaling or a calculated value
                // For a more accurate tooltip, you'd need to calculate based on current player stats.
                // For now, let's just show the calculated value with current player stats.
                minDamage = CalculateScaledValue(ability.damageScalings, ability.baseDamageOrHealing);
                maxDamage = minDamage; // Assume min/max are same for abilities for now unless explicitly defined
                sb.AppendLine($"<color=#FF5555>Damage: {minDamage:F0} {ability.damageType} Damage</color>");
            } else {
                 sb.AppendLine($"<color=#FF5555>Damage: {ability.baseDamageOrHealing:F0} {ability.damageType} Damage</color>");
            }
        }
        else if (ability.abilityType == AbilityData.AbilityType.Heal)
        {
            float healingAmount = CalculateScaledValue(ability.healingScalings, ability.baseDamageOrHealing);
            sb.AppendLine($"<color=#00FF00>Healing: {healingAmount:F0}</color>");
        }

        // Add cast time
        if (ability.castTime > 0)
        {
            sb.AppendLine($"<color=#AAAAAA>Cast Time: {ability.castTime:F1}s</color>");
        }

        // Add range/AoE info
        if (ability.targetType == AbilityData.TargetType.SingleTarget || ability.targetType == AbilityData.TargetType.AreaOfEffect || ability.targetType == AbilityData.TargetType.PointTarget)
        {
            sb.AppendLine($"<color=#AAAAAA>Range: {ability.range:F1}m</color>");
            if (ability.targetType == AbilityData.TargetType.AreaOfEffect)
            {
                sb.AppendLine($"<color=#AAAAAA>Area Radius: {ability.areaRadius:F1}m</color>");
            }
        }

        return sb.ToString();
    }

    // New method to update a single ability slot's UI
    void UpdateAbilitySlotUI(AbilityData ability, Image iconImage, TextMeshProUGUI keybindText, TextMeshProUGUI cooldownText, float currentCooldown, KeyCode keybind)
    {
        if (iconImage == null || keybindText == null || cooldownText == null) return; // Basic null check

        if (ability != null)
        {
            iconImage.gameObject.SetActive(true); // Ensure icon is visible if ability is assigned
            iconImage.sprite = ability.icon;
            keybindText.text = keybind.ToString().Replace("Alpha", ""); // Display '1', '2', etc. instead of 'Alpha1'

            if (currentCooldown > 0)
            {
                cooldownText.text = currentCooldown.ToString("F1"); // Display cooldown with one decimal
                cooldownText.gameObject.SetActive(true); // Show cooldown text
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Dim icon when on cooldown
            }
            else
            {
                cooldownText.gameObject.SetActive(false); // Hide cooldown text
                iconImage.color = Color.white; // Full color when off cooldown
            }

            // Mana cost display (optional, could be added to tooltip later)
            // For now, if insufficient mana, maybe dim slightly more or show a warning
            if (playerStats != null && playerStats.currentMana < ability.manaCost)
            {
                iconImage.color = new Color(0.3f, 0.3f, 1f, 0.8f); // Blue tint for insufficient mana
            }

        }
        else
        {
            // If no ability is assigned, hide the icon and texts or show empty state
            iconImage.gameObject.SetActive(false); // Hide the icon
            keybindText.text = keybind.ToString().Replace("Alpha", ""); // Still show keybind even if no ability
            cooldownText.gameObject.SetActive(false); // Hide cooldown text
        }
    }
}
