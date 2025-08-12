using UnityEngine;
using UnityEngine.AI;

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

    [Header("Smart Cast Settings")]
    public float smartCastRadius = 5f;
    public bool drawSmartCastDebug = true;

    [Header("UI Prefabs")]
    public GameObject floatingDamageNumberPrefab; // Assign the FloatingDamageNumberPrefab here

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

        lastAttackTime = -playerStats.attackSpeed; // Initialize to allow immediate attack
        isAttackingTarget = false; // Initialize to false
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if the hit object is on the "Enemy" layer
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    // If a new enemy is clicked, or the current target is different OR
                    // if the same enemy is clicked but we are not currently attacking it (e.g., moved away)
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
                        }
                        else
                        {
                            navMeshAgent.ResetPath(); // Stop any previous movement to immediately pursue new target
                            isAttackingTarget = true; // Now attacking this target
                        }
                    }
                    // If the same enemy is clicked again and we are already attacking it,
                    // do nothing, as it means we are continuing the attack.
                }
                else // If not an enemy (i.e., ground or other interactable), prioritize movement
                {
                    // Clear target and stop current actions
                    navMeshAgent.ResetPath(); // Stop any previous movement/attack intent
                    SetDestinationToMousePosition(); // Execute new movement command
                    isAttackingTarget = false; // Not attacking if moving to ground
                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            // If holding left click, and no target is selected or not attacking a target, chase cursor
            if (currentTarget == null || !isAttackingTarget)
            {
                SetDestinationToMousePosition();
                isAttackingTarget = false; // Ensure attacking is false if chasing cursor
            }
        }

        if (Input.GetMouseButtonDown(1)) // Right-click to unselect target or select if none
        {
            if (currentTarget != null)
            {
                Debug.Log("Target unselected.");
                currentTarget = null;
                currentEnemyStats = null;
                navMeshAgent.ResetPath(); // Stop moving towards target
                isAttackingTarget = false; // Not attacking anymore
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
                        }
                        else
                        {
                            isAttackingTarget = false; // Explicitly NOT attacking
                        }
                    }
                }
            }
        }

        // Combat Logic - Only execute if a target is selected AND the player is meant to be attacking it
        if (currentTarget != null && currentEnemyStats != null && isAttackingTarget)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= playerStats.attackRange)
            {
                navMeshAgent.ResetPath(); // Stop moving
                // Look at target (optional, but good for combat visuals)
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * navMeshAgent.angularSpeed);

                if (Time.time >= lastAttackTime + playerStats.attackSpeed)
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
                        }
                        else
                        {
                            navMeshAgent.ResetPath();
                            isAttackingTarget = true;
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
                }
            }
            else
            {
                // Debug.Log("Smart Cast: Raycast did not hit ground.");
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
            currentEnemyStats.currentHealth -= damage;
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

            if (currentEnemyStats.currentHealth <= 0)
            {
                Debug.Log(currentEnemyStats.gameObject.name + " has been defeated!");
                Destroy(currentEnemyStats.gameObject); // Destroy enemy GameObject
                currentTarget = null;
                currentEnemyStats = null;
                navMeshAgent.ResetPath();
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
