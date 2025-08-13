using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

public class EnemyAI : MonoBehaviour
{
    public EnemyStats enemyStats; // Reference to EnemyStats script
    public Transform playerTransform; // Reference to the player's transform
    public LayerMask whatIsGround; // To define where the enemy can wander

    // Wandering
    private Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange; // How far the enemy can wander from its initial position

    // Chasing
    public float sightRange; // How far the enemy can see the player (should be similar to chaseRadius)
    public float attackRange; // How close the enemy needs to be to attack

    // New: Assign the FloatingDamageNumberPrefab here for enemy attacks
    public GameObject floatingDamageNumberPrefab;

    // State machine
    public enum EnemyState { Wandering, Chasing, Attacking, Idle }
    public EnemyState currentState;

    private NavMeshAgent agent;
    private float lastAttackTime; // To control attack speed
    public Vector3 initialPosition; // Store the initial spawn position

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (enemyStats == null)
        {
            enemyStats = GetComponent<EnemyStats>();
        }
        // Assuming the player is tagged as "Player"
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        initialPosition = transform.position; // Store initial position on Awake
    }

    private void Start()
    {
        // Initialize values from EnemyStats if available
        if (enemyStats != null)
        {
            sightRange = enemyStats.chaseRadius;
            attackRange = enemyStats.attackRange;
            walkPointRange = enemyStats.hoveringRadius;
            agent.speed = enemyStats.movementSpeed;
        }
        currentState = EnemyState.Wandering; // Start in wandering state
        lastAttackTime = Time.time; // Initialize last attack time
    }

    private void Update()
    {
        // Debugging: Log current state

        switch (currentState)
        {
            case EnemyState.Wandering:
                Wander();
                break;
            case EnemyState.Chasing:
                ChasePlayer();
                break;
            case EnemyState.Attacking:
                AttackPlayer();
                break;
            case EnemyState.Idle:
                // Do nothing
                break;
        }
    }

    void Wander()
    {
        if (!walkPointSet) 
        {
            SearchWalkPoint();
        }

        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        // Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }

        // Check for player in sight range
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= sightRange)
            {
                currentState = EnemyState.Chasing;
            }
        }
    }

    void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(walkPoint, out hit, walkPointRange, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
        }
    }

    void ChasePlayer()
    {
        if (playerTransform == null)
        {
            currentState = EnemyState.Wandering; // Revert to wandering if player is lost
            return;
        }

        agent.SetDestination(playerTransform.position);

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        // New: Calculate distance from initial spawn point to player
        float distanceToInitialSpawn = Vector3.Distance(initialPosition, playerTransform.position);

        // If player is out of chase range (from initial spawn), go back to wandering
        if (distanceToInitialSpawn > enemyStats.chaseRadius)
        {
            currentState = EnemyState.Wandering;
            agent.SetDestination(initialPosition); // Return to initial spawn point
            // Removed: playerTransform = null; // Do NOT clear target, allow re-engagement
        }
        // If player is within attack range, stop and attack
        else if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            agent.isStopped = true; // Stop moving to attack
        }
        else
        {
            agent.isStopped = false; // Ensure agent is not stopped if just chasing
        }
    }

    // New method for enemy to attack the player
    void AttackPlayer()
    {
        if (playerTransform == null) return; // No player to attack

        // Ensure enemy is stopped and facing the player
        agent.isStopped = true;
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed);

        // Attack cooldown
        if (Time.time >= lastAttackTime + (1f / enemyStats.attackSpeed))
        {
            // Perform attack logic here
            Stats playerStats = playerTransform.GetComponent<Stats>();
            if (playerStats != null)
            {
                float hitChance = enemyStats.hitPoints / (enemyStats.hitPoints + playerStats.dodgePoints);
                float randomValue = Random.value;

                if (randomValue <= hitChance)
                {
                    // Hit!
                    float damageToDeal = Random.Range(enemyStats.minPhysicalDamage, enemyStats.maxPhysicalDamage);
                    playerStats.TakeDamage(damageToDeal); // Enemy attacking player

                    // Show floating damage number over player
                    if (floatingDamageNumberPrefab != null)
                    {
                        GameObject go = Instantiate(floatingDamageNumberPrefab, playerTransform.position, Quaternion.identity);
                        FloatingDamageNumbers dmgText = go.GetComponent<FloatingDamageNumbers>();
                        if (dmgText != null)
                        {
                            dmgText.Initialize(damageToDeal, false, false); // Assuming no critical hit or heal for now
                        }
                    }
                }
                else
                {
                    // Miss!

                    // Show MISS! floating text over player
                    if (floatingDamageNumberPrefab != null)
                    {
                        GameObject go = Instantiate(floatingDamageNumberPrefab, playerTransform.position, Quaternion.identity);
                        FloatingDamageNumbers dmgText = go.GetComponent<FloatingDamageNumbers>();
                        if (dmgText != null)
                        {
                            dmgText.Initialize(0, false, false, true); // Value 0, isMiss = true
                        }
                    }
                }
            }
            lastAttackTime = Time.time; // Reset attack cooldown
        }

        // Check if player moved out of attack range, then chase again
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > attackRange)
        {
            currentState = EnemyState.Chasing;
            agent.isStopped = false; // Start moving again
        }
    }

    // This method will be called by the player when attacking
    public void TakeAggro(Transform attacker)
    {
        playerTransform = attacker; // Set the attacker as the target
        currentState = EnemyState.Chasing; // Immediately start chasing
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Access hoveringRadius from enemyStats
        if (enemyStats != null) {
            Gizmos.DrawWireSphere(transform.position, enemyStats.hoveringRadius); // Visualize hovering radius
        }

        Gizmos.color = Color.red;
        // Access chaseRadius from enemyStats
        if (enemyStats != null) {
            Gizmos.DrawWireSphere(transform.position, enemyStats.chaseRadius); // Visualize chase radius
        }
    }
}
