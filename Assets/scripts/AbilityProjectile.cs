using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f; // How long the projectile exists before being destroyed
    public GameObject floatingDamageNumberPrefab; // To show damage numbers over the enemy

    private Transform target; // The enemy to hit
    private float damageAmount; // The calculated damage to deal
    private AbilityData.DamageType damageType; // The type of damage (Physical, Magical, Pure)
    private Transform attackerTransform; // The player's transform (for aggro and damage calculation)

    void Start()
    {
        Destroy(gameObject, lifeTime); // Destroy projectile after a set lifetime
    }

    public void Initialize(Transform newTarget, float newDamageAmount, AbilityData.DamageType newDamageType, Transform newAttacker, GameObject damageNumberPrefab)
    {
        target = newTarget;
        damageAmount = newDamageAmount;
        damageType = newDamageType;
        attackerTransform = newAttacker;
        floatingDamageNumberPrefab = damageNumberPrefab;
    }

    void Update()
    {
        if (target != null)
        {
            // Move towards the target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Optional: Rotate to face target (for visual appeal)
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        else
        {
            // If target is null (e.g., destroyed), destroy projectile
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"AbilityProjectile: Trigger entered with {other.gameObject.name} (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // Check if the projectile hit the target or an enemy
        if (target != null && other.transform == target)
        {
            Debug.Log($"AbilityProjectile: Hit specific target {target.name}.");
            ApplyDamageAndDestroy(other);
        }
        // For AoE abilities, or if the target dies before projectile reaches, hit any enemy in its path
        else if (other.CompareTag("Enemy") || (target == null && other.GetComponent<EnemyStats>() != null))
        {
            Debug.Log($"AbilityProjectile: Hit a generic enemy or target was null. Hit: {other.gameObject.name}.");
            ApplyDamageAndDestroy(other);
        }
        else
        {
            Debug.Log($"AbilityProjectile: Hit non-enemy object {other.gameObject.name}. Destroying projectile without damage application.");
            Destroy(gameObject); // Destroy if it hits something irrelevant
        }
    }

    void ApplyDamageAndDestroy(Collider hitCollider)
    {
        Debug.Log($"AbilityProjectile: ApplyDamageAndDestroy called for {hitCollider.gameObject.name}.");
        EnemyStats enemyStats = hitCollider.GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            // Apply damage based on damage type (similar to PlayerAbilities.ApplyDamageToTarget)
            float finalDamage = damageAmount;
            if (damageType == AbilityData.DamageType.Physical || damageType == AbilityData.DamageType.Magical)
            {
                finalDamage = Mathf.Max(0, damageAmount - enemyStats.defense);
            }
            // Pure damage ignores defense - no change needed for finalDamage

            enemyStats.TakeDamage(finalDamage, attackerTransform); // Pass attacker for aggro

            // Show floating damage number over the enemy
            if (floatingDamageNumberPrefab != null)
            {
                GameObject go = Instantiate(floatingDamageNumberPrefab, enemyStats.transform.position, Quaternion.identity);
                FloatingDamageNumbers dmgText = go.GetComponent<FloatingDamageNumbers>();
                if (dmgText != null)
                {
                    dmgText.Initialize(finalDamage, false, false); // Assuming no critical hit or heal
                }
            }
        }
        Destroy(gameObject); // Destroy the projectile after hitting
    }
}
