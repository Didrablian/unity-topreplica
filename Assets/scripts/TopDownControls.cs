using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems; // Add this line

public class TopDownControls : MonoBehaviour
{
    public float moveSpeed = 5f;
    public GameObject clickIndicatorPrefab;

    private NavMeshAgent navMeshAgent;
    private GameObject currentIndicator;
    private Stats playerStats;
    private Transform currentTarget;
    private EnemyStats currentEnemyStats; // Reference to the enemy's Stats script
    private float lastAttackTime;
    private bool isAttackingTarget; // New flag to indicate if player is currently attacking a target

    // Reference to Player Inventory
    private PlayerInventory playerInventory;

    // New: For click-to-loot functionality
    private Transform targetLootItem; // The WorldItem transform being targeted for looting
    public float lootRange = 2.5f; // Range within which items can be looted (should be slightly more than 2f from E key logic)

    // New: For click debounce
    private float lastMovementClickTime = -1f; // Initialize to -1 to allow first click
    public float movementClickDebounceTime = 0.15f; // Time in seconds to ignore subsequent rapid clicks for movement

    // New variable to control input
    private bool inputEnabled = true;

    // Public getter for inputEnabled
    public bool IsInputEnabled { get { return inputEnabled; } }

    // New method to set input state
    public void SetInputEnabled(bool enable)
    {
        inputEnabled = enable;
        // Potentially stop movement if input is disabled mid-move
        if (!inputEnabled && navMeshAgent != null && navMeshAgent.hasPath)
        {
            navMeshAgent.ResetPath();
        }
    }

    [Header("Smart Cast Settings")]
    public float smartCastRadius = 5f;
    public bool drawSmartCastDebug = true;

    [Header("UI Prefabs")]
    public GameObject floatingDamageNumberPrefab; // Assign the FloatingDamageNumberPrefab here
    public GameObject enemyNameplateUI; // Assign your enemy nameplate GameObject here
    public GameObject statsPanelUI; // New: Assign your Stats Panel UI GameObject here

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found on the player character.");
            enabled = false; // Disable script if no NavMeshAgent
        }

        playerStats = GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("Stats component not found on the player character.");
        }

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory component not found on the player character. Looting will not work.");
        }

        lastAttackTime = -playerStats.attackSpeed; // Initialize to allow immediate attack
        isAttackingTarget = false; // Initialize to false

        // Hide the enemy nameplate at the start
        if (enemyNameplateUI != null)
        {
            enemyNameplateUI.SetActive(false);
        }
    }

    void Update()
    {
        if (!inputEnabled) return; // Exit if input is disabled

        // New: Check if the pointer is over a UI element
        // This check should only apply to mouse-based input for game actions.
        // Keyboard inputs (like 'C' for panel toggle) should always work.

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"TopDownControls: Mouse clicked. Ray hit {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}).");
                // NEW: If we click something different than the current loot target, or if we click something else entirely, clear the loot target.
                // This allows other commands to override pending loot movement.
                if (targetLootItem != null && hit.transform != targetLootItem)
                {
                    targetLootItem = null; // Clear pending loot target
                    navMeshAgent.ResetPath(); // Stop movement towards old loot target
                }

                // Check if the hit object is a WorldItem (lootable)
                WorldItem clickedWorldItem = hit.collider.GetComponent<WorldItem>();

                if (clickedWorldItem != null && clickedWorldItem.itemData != null) // Prioritize looting clicked item
                {
                    lastMovementClickTime = Time.time; // Reset debounce timer for looting
                    float distanceToItem = Vector3.Distance(transform.position, hit.transform.position);
                    if (distanceToItem <= lootRange) // Already in range, loot immediately
                    {
                        playerInventory.AddItem(clickedWorldItem.itemData);
                        Destroy(clickedWorldItem.gameObject);
                        playerInventory.LogAllItems();
                        targetLootItem = null; // Clear any pending loot target
                        navMeshAgent.ResetPath(); // Stop any current movement
                    }
                    else // Out of range, set as target to move towards
                    {
                        targetLootItem = hit.transform; // Set the item as the target to move to
                        navMeshAgent.SetDestination(targetLootItem.position); // Move towards it

                        // Clear other targets/actions if a loot target is set
                        currentTarget = null; // Prioritize loot over combat target
                        isAttackingTarget = false; 
                        if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false);
                    }
                }
                // Original logic for enemy targeting or ground movement
                else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    lastMovementClickTime = Time.time; // Reset debounce timer for enemy targeting
                    // If an enemy is clicked, clear any pending loot target
                    targetLootItem = null;
                    // Existing enemy targeting logic
                    if (hit.transform != currentTarget || !isAttackingTarget)
                    {
                        currentTarget = hit.transform;
                        currentEnemyStats = currentTarget.GetComponent<EnemyStats>();
                        if (currentEnemyStats == null)
                        {
                            Debug.LogError("EnemyStats component not found on the target.");
                            currentTarget = null; // Unselect if no EnemyStats
                            currentEnemyStats = null;
                            navMeshAgent.ResetPath(); // Stop any previous movement
                            isAttackingTarget = false; // Not attacking if target is invalid
                            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if target invalid
                            playerStats.currentTarget = null; // Also clear target in PlayerStats
                        }
                        else
                        {
                            navMeshAgent.ResetPath(); // Stop any previous movement to immediately pursue new target
                            isAttackingTarget = true; // Now attacking this target
                            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(true); // Show nameplate if enemy selected
                            playerStats.currentTarget = currentTarget; // Set target in PlayerStats
                        }
                    }
                    // If the same enemy is clicked again and we are already attacking it,
                    // do nothing, as it means we are continuing the attack.
                }
                else // If not an enemy (i.e., ground or other interactable), prioritize movement
                {
                    // NEW: Apply debounce for ground movement clicks
                    if (Time.time - lastMovementClickTime < movementClickDebounceTime)
                    {
                        return; // Ignore this click if it's too soon after another movement command
                    }

                    lastMovementClickTime = Time.time; // Record time of this processed movement click

                    navMeshAgent.ResetPath(); // Stop any previous movement/attack intent
                    SetDestinationToMousePosition(); // Execute new movement command

                    // If a target is currently selected, do NOT unselect it.
                    // Temporarily disable attacking mode if clicking to move on ground, but keep target selected.
                    if (currentTarget != null)
                    {
                        isAttackingTarget = false; // Player is choosing to move, not attack, for now
                    }
                    else 
                    {
                        // Only clear target-related flags if no target was selected in the first place.
                        isAttackingTarget = false; // Not attacking if moving to ground without a target
                        if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if moving to ground without a target
                        playerStats.currentTarget = null; // Clear target in PlayerStats if no target was selected
                    }
                }
            }
        }
        else if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            // If holding left click, and no target is selected or not attacking a target, chase cursor
            // Only clear target-related flags if no target was selected in the first place.
            if (currentTarget == null)
            {
                // NEW: Apply debounce for held-click movement (to prevent stuttering on continuous drag)
                if (Time.time - lastMovementClickTime < movementClickDebounceTime)
                {
                    return; // Ignore this click if it's too soon
                }
                lastMovementClickTime = Time.time; // Record time of this processed movement click

                SetDestinationToMousePosition();
                isAttackingTarget = false; // Ensure attacking is false if chasing cursor
                if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if chasing cursor
                playerStats.currentTarget = null; // Clear target in PlayerStats if no target was selected
            }
            else
            {
                // If a target is selected, continue moving towards cursor without changing target state or nameplate visibility
                SetDestinationToMousePosition();
            }
        }

        if (Input.GetMouseButtonDown(1)) // Right-click to unselect target or select if none
        {
            // The following conditional block has been changed to allow right click to unselect target
            // even when over UI. If this is not desired, add !EventSystem.current.IsPointerOverGameObject()
            // to the condition above as well.
            if (currentTarget != null)
            {
                Debug.Log("Target unselected.");
                currentTarget = null;
                currentEnemyStats = null;
                navMeshAgent.ResetPath(); // Stop moving towards target
                isAttackingTarget = false; // Not attacking anymore
                if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate on unselect
                playerStats.currentTarget = null; // Clear target in PlayerStats
            }
            else
            {
                // If no target selected, attempt to select an enemy with right-click
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                    {
                        currentTarget = hit.transform;
                        currentEnemyStats = currentTarget.GetComponent<EnemyStats>();
                        if (currentEnemyStats == null)
                        {
                            Debug.LogError("EnemyStats component not found on the target during right-click selection.");
                            currentTarget = null;
                            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if target invalid
                            playerStats.currentTarget = null; // Also clear target in PlayerStats
                        }
                        else
                        {
                            isAttackingTarget = false; // Explicitly NOT attacking
                            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(true); // Show nameplate if enemy selected
                            playerStats.currentTarget = currentTarget; // Set target in PlayerStats
                        }
                    } else {
                        // If right-clicking on non-enemy and no target, hide nameplate
                        if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false);
                        playerStats.currentTarget = null; // Clear target in PlayerStats
                    }
                }
            }
        }

        // Combat Logic - Only execute if a target is selected AND the player is meant to be attacking it
        if (currentTarget != null && currentEnemyStats != null && isAttackingTarget)
        {
            // Clear any active loot target if player is engaging in combat
            if (targetLootItem != null) 
            {
                targetLootItem = null;
                navMeshAgent.ResetPath(); // Stop moving to loot if re-engaging combat
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= playerStats.attackRange)
            {
                navMeshAgent.ResetPath(); // Stop moving
                // Look at target (optional, but good for combat visuals)
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * navMeshAgent.angularSpeed);

                // Calculate attack cooldown based on attacks per second
                if (Time.time >= lastAttackTime + (1f / playerStats.attackSpeed))
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }
            }
            else
            {
                // Move towards target if out of range
                navMeshAgent.SetDestination(currentTarget.position);
            }
        } else if (currentTarget == null) {
            // If currentTarget becomes null (e.g., enemy died), hide nameplate
            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false);
        }

        // Continuous Looting Logic (if moving towards an item)
        if (targetLootItem != null)
        {
            // Stop combat if moving to loot a specific item
            currentTarget = null; // Clear combat target
            isAttackingTarget = false;
            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false);

            float distanceToLootItem = Vector3.Distance(transform.position, targetLootItem.position);
            if (distanceToLootItem <= lootRange)
            {
                // Get the WorldItem component from the target
                WorldItem worldItem = targetLootItem.GetComponent<WorldItem>();
                if (worldItem != null && worldItem.itemData != null)
                {
                    playerInventory.AddItem(worldItem.itemData);
                    Destroy(worldItem.gameObject);
                    playerInventory.LogAllItems();
                }
                targetLootItem = null; // Clear loot target after looting/failure
                navMeshAgent.ResetPath(); // Stop movement after looting
            }
            else
            {
                // Continue moving towards the item
                navMeshAgent.SetDestination(targetLootItem.position);
            }
        }

        // Add experience on '+' key press
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (playerStats != null)
            {
                playerStats.GainExp(10f); // Add 10 experience points
            }
        }

        // Smart Cast (A Key)
        if (Input.GetKeyDown(KeyCode.A))
        {
            // The following conditional block has been changed to allow Smart Cast (A) to be used
            // even when over UI. If this is not desired, add !EventSystem.current.IsPointerOverGameObject()
            // to the condition above as well.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
            {
                Vector3 sphereCenter = hit.point;

                if (drawSmartCastDebug)
                {
                    // Draw a cross to visualize the smart cast sphere center
                    Debug.DrawLine(sphereCenter - Vector3.right * smartCastRadius * 0.1f, sphereCenter + Vector3.right * smartCastRadius * 0.1f, Color.red, 2f);
                    Debug.DrawLine(sphereCenter - Vector3.forward * smartCastRadius * 0.1f, sphereCenter + Vector3.forward * smartCastRadius * 0.1f, Color.red, 2f);
                    Debug.DrawLine(sphereCenter - Vector3.up * smartCastRadius * 0.1f, sphereCenter + Vector3.up * smartCastRadius * 0.1f, Color.red, 2f);
                }

                Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, smartCastRadius, LayerMask.GetMask("Enemy"));

                if (hitColliders.Length > 0)
                {
                    // Enemies found, find the closest one to the cursor hit point
                    Transform closestEnemy = null;
                    float minDistance = Mathf.Infinity;

                    foreach (Collider col in hitColliders)
                    {
                        float dist = Vector3.Distance(sphereCenter, col.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestEnemy = col.transform;
                        }
                    }

                    if (closestEnemy != null)
                    {
                        currentTarget = closestEnemy;
                        currentEnemyStats = currentTarget.GetComponent<EnemyStats>();
                        if (currentEnemyStats == null)
                        {
                            Debug.LogError("EnemyStats component not found on smart-cast target.");
                            currentTarget = null;
                            isAttackingTarget = false;
                            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if target invalid
                            playerStats.currentTarget = null; // Also clear target in PlayerStats
                        }
                        else
                        {
                            navMeshAgent.ResetPath();
                            isAttackingTarget = true;
                            if (enemyNameplateUI != null) enemyNameplateUI.SetActive(true); // Show nameplate if enemy selected via smart cast
                            playerStats.currentTarget = currentTarget; // Set target in PlayerStats
                        }
                    }
                }
                else
                {
                    // No enemies found, move to cursor location
                    if (currentTarget != null)
                    {
                        currentTarget = null;
                        isAttackingTarget = false;
                    }
                    navMeshAgent.ResetPath();
                    navMeshAgent.SetDestination(hit.point);
                    ShowClickIndicator(hit.point); // Show indicator for movement
                    if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if no enemy found with smart cast
                    playerStats.currentTarget = null; // Clear target in PlayerStats
                }
            }
            else
            {
                // Debug.Log("Smart Cast: Raycast did not hit ground.");
                if (enemyNameplateUI != null) enemyNameplateUI.SetActive(false); // Hide nameplate if smart cast ray doesn't hit ground
                playerStats.currentTarget = null; // Clear target in PlayerStats
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (statsPanelUI != null)
            {
                bool currentPanelState = statsPanelUI.activeSelf;
                statsPanelUI.SetActive(!currentPanelState);
            }
            else
            {
                Debug.LogWarning("Stats Panel UI GameObject is NOT assigned in TopDownControls.cs!");
            }
        }

        // Looting input (Press 'E')
        if (inputEnabled && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInventory != null)
            {
                // Check for nearby WorldItems
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Item")); // 2f is arbitrary radius, adjust as needed

                if (hitColliders.Length > 0)
                {
                    // For simplicity, pick up the first item found
                    WorldItem worldItem = hitColliders[0].GetComponent<WorldItem>();
                    if (worldItem != null && worldItem.itemData != null)
                    {
                        playerInventory.AddItem(worldItem.itemData); // Add item to inventory
                        Destroy(worldItem.gameObject); // Destroy the world item
                        playerInventory.LogAllItems(); // Log current inventory for debug
                    }
                    else
                    {
                        Debug.LogWarning("Nearby object is not a valid WorldItem.");
                    }
                }
                else
                {
                    Debug.Log("No items nearby to loot.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerInventory reference is null. Cannot loot.");
            }
        }
    }

    void SetDestinationToMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                navMeshAgent.SetDestination(hit.point);
                ShowClickIndicator(hit.point);
            }
        }
    }

    void PerformAttack()
    {
        if (currentEnemyStats == null) return;

        float hitChance = playerStats.hitPoints / (playerStats.hitPoints + currentEnemyStats.dodgePoints);
        float randomValue = Random.value; // Value between 0.0 and 1.0

        if (randomValue <= hitChance)
        {
            // Hit!
            float damage = Random.Range(playerStats.minPhysicalDamage, playerStats.maxPhysicalDamage);
            currentEnemyStats.TakeDamage(damage, this.transform); // Pass player's transform as attacker
            Debug.Log(playerStats.gameObject.name + " attacked " + currentEnemyStats.gameObject.name + " with damage: " + damage.ToString("F2") + ". Enemy Health: " + currentEnemyStats.currentHealth.ToString("F2"));

            // Show floating damage number
            if (floatingDamageNumberPrefab != null)
            {
                Debug.Log("Instantiating floating damage number.");
                GameObject go = Instantiate(floatingDamageNumberPrefab, currentEnemyStats.transform.position, Quaternion.identity);
                FloatingDamageNumbers dmgText = go.GetComponent<FloatingDamageNumbers>();
                if (dmgText != null)
                {
                    dmgText.Initialize(damage, false, false); // Assuming no critical hit or heal for now
                }
                else
                {
                    Debug.LogError("FloatingDamageNumbers component not found on instantiated prefab. Check prefab setup!");
                }
            }
            else
            {
                Debug.LogWarning("Floating Damage Number Prefab is not assigned in TopDownControls.");
            }

            // Grant experience if enemy is defeated
            if (currentEnemyStats.currentHealth <= 0)
            {
                if (playerStats != null && currentEnemyStats.monsterData != null)
                {
                    playerStats.GainExp(currentEnemyStats.monsterData.experienceGiven);
                    Debug.Log(playerStats.gameObject.name + " gained " + currentEnemyStats.monsterData.experienceGiven + " experience from defeating " + currentEnemyStats.monsterData.monsterName + ".");
                }
                // Death and destruction handled by EnemyStats.TakeDamage()
                currentTarget = null; // Clear target in TopDownControls once enemy is defeated
                currentEnemyStats = null;
                navMeshAgent.ResetPath();
                playerStats.currentTarget = null; // Also clear target in PlayerStats
            }
        }
        else
        {
            // Miss!
            Debug.Log(playerStats.gameObject.name + " missed " + currentEnemyStats.gameObject.name + ".");

            // Show MISS! floating text
            if (floatingDamageNumberPrefab != null)
            {
                GameObject go = Instantiate(floatingDamageNumberPrefab, currentEnemyStats.transform.position, Quaternion.identity);
                FloatingDamageNumbers dmgText = go.GetComponent<FloatingDamageNumbers>();
                if (dmgText != null)
                {
                    dmgText.Initialize(0, false, false, true); // Value 0, isMiss = true
                }
                else
                {
                    Debug.LogError("FloatingDamageNumbers component not found on instantiated MISS prefab. Check prefab setup!");
                }
            }
            else
            {
                Debug.LogWarning("Floating Damage Number Prefab is not assigned in TopDownControls for MISS.");
            }
        }
    }

    void ShowClickIndicator(Vector3 position)
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }

        if (clickIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
            Destroy(currentIndicator, 1f); // Destroy indicator after 1 second
        } else {
             // If no prefab is assigned, create a simple sphere for visual feedback
            currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentIndicator.transform.position = position + Vector3.up * 0.1f; // Slightly above ground
            currentIndicator.transform.localScale = Vector3.one * 0.5f;
            
            // Optional: Add a simple material for visibility
            Renderer renderer = currentIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue; 
            }
            Destroy(currentIndicator, 1f); // Destroy indicator after 1 second
        }
    }
}
