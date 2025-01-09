using UnityEngine;
using System.Collections;

public class CthulhuController : MonoBehaviour
{
    private enum State { Idle, Walking, Flying, Attacking }

    [Header("References")]
    public Transform player;
    private Enemy enemyComponent;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isFacingRight = true;

    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float walkRange = 5f;    // Within this range, Cthulhu will walk
    public float attackRange = 2f;
    public LayerMask groundLayer;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float flySpeed = 5f;
    public float flyingHeight = 4f;
    private State currentState;

    [Header("Attack Settings")]
    public float attack1Damage = 25f;
    public float attack2Damage = 40f;
    public float minAttackCooldown = 2f;
    public float maxAttackCooldown = 4f;
    private float nextAttackTime;
    private bool isAttacking;

    [Header("Collision Settings")]
    private Collider2D[] myColliders;  // Array to store all colliders on this GameObject
    private int originalLayer;         // Store the original layer
    public LayerMask ignoreWhileFlying;  // Layers to ignore while flying

    [Header("Landing Settings")]
    private bool isLanding = false;
    private Vector2 landingPosition;
    public float landingDistance = 2f;    // How far from the player to land
    public float groundOffset = 4.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip walkSound;
    public AudioClip flySound;
    public AudioClip attack1Sound;
    public AudioClip attack2Sound;
    public AudioClip idleSound;
    public float idleSoundInterval = 5f;
    private float nextIdleSound;

    private void Start()
    {
        // Initialize components
        enemyComponent = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        // Add to existing Start()
        audioSource = GetComponent<AudioSource>();
        nextIdleSound = Time.time + idleSoundInterval;

        // Get all colliders attached to this GameObject
        myColliders = GetComponents<Collider2D>();
        originalLayer = gameObject.layer;

        currentState = State.Idle;
        SetNextAttackTime();
    }

    private void Update()
    {
        if (enemyComponent.currentHealth <= 0)
        {
            animator.SetBool("IsDead", true);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Update state based on distance
        UpdateState(distanceToPlayer);

        // Handle movement and attacks based on state
        HandleCurrentState(distanceToPlayer);

        // Update facing direction
        UpdateFacing();
    }

    private void UpdateState(float distanceToPlayer)
    {
        // Skip state changes if still landing or attacking
        if (isLanding || isAttacking) return;

        animator.SetBool("IsFlying", false);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsIdle", true);

        State previousState = currentState;

        // Check attack range only after landing
        if (!isLanding && distanceToPlayer <= attackRange)
        {
            currentState = State.Attacking;
            rb.velocity = Vector2.zero;
        }
        else if (distanceToPlayer <= walkRange)
        {
            currentState = State.Walking;
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsIdle", false);
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = State.Flying;
            animator.SetBool("IsFlying", true);
        }
        else
        {
            currentState = State.Idle;
            rb.velocity = new Vector2(0, rb.velocity.y);
            animator.SetBool("IsIdle", true);
        }

        // Handle state transition physics
        if (previousState != currentState)
        {
            HandleStateTransition(previousState, currentState);
        }
    }

    private void StopForAttack()
    {
        rb.velocity = Vector2.zero;
        if (!isAttacking && Time.time >= nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private void HandleStateTransition(State previousState, State newState)
    {
        if (newState == State.Flying)
        {
            // Enable flying mode
            rb.gravityScale = 0f;
            DisableCollisions();
            isLanding = false;
        }
        else if (previousState == State.Flying)
        {
            // Calculate landing position near player
            CalculateLandingPosition();
            isLanding = true;
            StartCoroutine(LandingSequence());
        }
    }

    private void CalculateLandingPosition()
    {
        // Get direction from player to enemy (normalized)
        Vector2 directionFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;

        // Calculate landing position at ground level with horizontal offset
        landingPosition = new Vector2(
            player.position.x + (directionFromPlayer.x * landingDistance),
            player.position.y - groundOffset  // Offset down from player's Y position
        );
    }

    private IEnumerator LandingSequence()
    {
        while (Vector2.Distance((Vector2)transform.position, landingPosition) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                landingPosition,
                flySpeed * Time.deltaTime
            );
            yield return null;
        }

        rb.gravityScale = 1f;
        EnableCollisions();
        isLanding = false;

        // Check if should attack immediately after landing
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            currentState = State.Attacking;
            rb.velocity = Vector2.zero;
            StartCoroutine(PerformAttack());
        }
    }

    private void DisableCollisions()
    {
        // Set layer to a flying layer that ignores collisions with environment
        gameObject.layer = LayerMask.NameToLayer("EnemyFlying"); // Make sure to create this layer in Unity

        // Optionally, you can also disable specific colliders
        foreach (Collider2D collider in myColliders)
        {
            // Keep trigger colliders enabled for damage detection
            if (!collider.isTrigger)
            {
                collider.enabled = false;
            }
        }
    }

    private void EnableCollisions()
    {
        // Restore original layer
        gameObject.layer = originalLayer;

        // Re-enable all colliders
        foreach (Collider2D collider in myColliders)
        {
            collider.enabled = true;
        }
    }

    private void HandleCurrentState(float distanceToPlayer)
    {
        switch (currentState)
        {
            case State.Walking:
                Walk();
                if (!audioSource.isPlaying || audioSource.clip != walkSound)
                {
                    audioSource.clip = walkSound;
                    audioSource.Play();
                }
                break;

            case State.Flying:
                Fly();
                if (!audioSource.isPlaying || audioSource.clip != flySound)
                {
                    audioSource.clip = flySound;
                    audioSource.Play();
                }
                break;

            case State.Idle:
                rb.velocity = new Vector2(0, rb.velocity.y);
                if (Time.time >= nextIdleSound)
                {
                    audioSource.PlayOneShot(idleSound);
                    nextIdleSound = Time.time + idleSoundInterval;
                }
                break;
        }
    }

    private void Walk()
    {
        if (currentState != State.Walking || isAttacking) return;
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * walkSpeed, rb.velocity.y);
    }

    private void Fly()
    {
        if (isLanding)
        {
            // Don't update position during landing sequence
            return;
        }

        Vector2 targetPosition = (Vector2)player.position + Vector2.up * flyingHeight;
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            flySpeed * Time.deltaTime
        );
    }

    private void UpdateFacing()
    {
        if ((player.position.x > transform.position.x && !isFacingRight) ||
            (player.position.x < transform.position.x && isFacingRight))
        {
            Flip();
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;
        animator.SetBool("IsWalking", false);  // Force stop walking animation

        bool useAttack2 = Random.value > 0.5f;
        animator.SetTrigger(useAttack2 ? "Attack2" : "Attack1");
        audioSource.PlayOneShot(useAttack2 ? attack2Sound : attack1Sound);

        // Wait for animation to reach damage frame
        yield return new WaitForSeconds(0.5f);

        // Check if player is still in range and apply damage
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            float damage = useAttack2 ? attack2Damage : attack1Damage;
            Player_Health playerHealth = player.GetComponent<Player_Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        // Wait for animation to complete
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
        SetNextAttackTime();
    }

    private void SetNextAttackTime()
    {
        nextAttackTime = Time.time + Random.Range(minAttackCooldown, maxAttackCooldown);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, walkRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}