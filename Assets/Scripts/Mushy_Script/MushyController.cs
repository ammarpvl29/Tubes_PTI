using UnityEngine;
using System.Collections;

public class MushyController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public bool isFlipped = false;

    [Header("Attack Settings")]
    public float detectionRange = 5f;
    public float attackRange = 2f;
    public float minAttackInterval = 2f;
    public float maxAttackInterval = 4f;
    public float meleeDamage = 20f;
    public LayerMask groundLayer;

    [Header("Particle Systems")]
    public ParticleSystem chargeParticles;    // Swirling charge effect
    public ParticleSystem orbitingParticles;  // Orbiting lava balls
    public ParticleSystem scatterParticles;   // Scattered lava projectiles
    public ParticleSystem attackParticles;    // Final burst effect

    [Header("Movement")]
    public float moveSpeed = 2f;

    private Animator animator;
    private Rigidbody2D rb;
    private Enemy enemyComponent;
    private float nextAttackTime;
    private bool isAttacking = false;
    private Player_Health playerHealth;

    [Header("Projectile Settings")]
    public GameObject lavaProjectilePrefab;
    public int projectilesPerAttack = 3;
    public float projectileSpreadAngle = 15f;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyComponent = GetComponent<Enemy>();
        playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_Health>();
        rb.gravityScale = 1f;
        SetNextAttackTime();
    }

    private void Update()
    {
        if (enemyComponent.currentHealth <= 0)
        {
            animator.SetBool("IsDead", true);
            return;
        }

        // Reset running state at the start of each frame
        animator.SetBool("IsRunning", false);

        // Look at player and check for attack
        if (IsPlayerInRange())
        {
            LookAtPlayer();

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // If not in attack range and not currently attacking, move towards player
            if (distanceToPlayer > attackRange && !isAttacking)
            {
                // Only modify x velocity, keep y velocity (gravity) unchanged
                float currentYVelocity = rb.velocity.y;
                Vector2 direction = (player.position - transform.position).normalized;
                rb.velocity = new Vector2(direction.x * moveSpeed, currentYVelocity);
                animator.SetBool("IsRunning", true);
            }
            else
            {
                // Only stop horizontal movement
                rb.velocity = new Vector2(0, rb.velocity.y);
            }

            if (!isAttacking && Time.time >= nextAttackTime)
            {
                StartCoroutine(PerformAttackSequence());
            }
        }
        else
        {
            // Only stop horizontal movement
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        return distanceToPlayer <= detectionRange;
    }

    public void LookAtPlayer()
    {
        Vector3 flipped = transform.localScale;
        flipped.z *= -1f;

        if (transform.position.x > player.position.x && isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = false;
        }
        else if (transform.position.x < player.position.x && !isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = true;
        }
    }

    private void SetNextAttackTime()
    {
        nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval);
    }

    private IEnumerator PerformAttackSequence()
    {
        isAttacking = true;
        Debug.Log("[Mushy] Starting lava attack sequence");

        // Particle effects remain the same...
        if (chargeParticles != null)
        {
            chargeParticles.Play();
        }
        yield return new WaitForSeconds(0.5f);

        if (orbitingParticles != null)
        {
            orbitingParticles.Play();
        }
        yield return new WaitForSeconds(1.5f);

        if (scatterParticles != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            scatterParticles.transform.right = directionToPlayer;
            scatterParticles.Play();
        }

        // Modified projectile spawning logic
        if (lavaProjectilePrefab != null && player != null)
        {
            Vector3 spawnPosition = transform.position;

            // Calculate direct vector to player
            Vector2 directionToPlayer = (player.position - spawnPosition).normalized;

            // Calculate spread angles for multiple projectiles
            float angleStep = projectileSpreadAngle / (projectilesPerAttack - 1);
            float startAngle = -projectileSpreadAngle / 2;

            for (int i = 0; i < projectilesPerAttack; i++)
            {
                // Calculate rotation for this projectile
                float currentAngle = startAngle + (angleStep * i);
                Vector2 rotatedDirection = RotateVector2(directionToPlayer, currentAngle);

                // Spawn projectile
                GameObject projectile = Instantiate(lavaProjectilePrefab, spawnPosition, Quaternion.identity);

                LavaProjectile lavaProjectile = projectile.GetComponent<LavaProjectile>();
                if (lavaProjectile != null)
                {
                    lavaProjectile.Initialize(rotatedDirection);

                    // Debug visualization
                    Debug.DrawRay(spawnPosition, rotatedDirection * 5f, Color.red, 2f);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        // 5. Check for melee attack if player is close
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            animator.SetTrigger("Attack");

            // Calculate exact frame timings (53 total frames)
            float frameRate = 60f; // Assuming 60fps animation
            float totalAnimationTime = 53f / frameRate;
            float damageFrameTime = 36f / frameRate;

            // Wait until damage frame
            yield return new WaitForSeconds(damageFrameTime);

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(meleeDamage);
            }

            // Wait for remaining frames
            float remainingTime = totalAnimationTime - damageFrameTime;
            yield return new WaitForSeconds(remainingTime);
        }

        isAttacking = false;
        SetNextAttackTime();
    }

    // Add this helper method to your class
    private Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private IEnumerator CleanupLavaProjectiles(GameObject[] projectiles)
    {
        yield return new WaitForSeconds(5f); // Adjust time based on how long you want projectiles to exist
        foreach (GameObject projectile in projectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}