using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float airControlMultiplier = 0.7f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Roll Settings")]
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private float rollDuration = 0.25f;
    [SerializeField] private float rollCooldown = 0.5f;

    [Header("Attack Settings")]
    [SerializeField] private float lightAttackDuration = 0.517f; // 31 frames at 60fps
    [SerializeField] private float heavyAttackDuration = 0.8f;
    [SerializeField] private float specialAttackDuration = 1f;

    private Rigidbody2D rb;
    private Vector3 originalScale;
    private bool isFacingRight = true;
    private bool isGrounded;
    private Transform groundCheck;
    private Animator animator;

    // Layer management
    private int playerLayer;
    private int enemyLayer;

    // State Management
    private enum PlayerState
    {
        Normal,
        Rolling,
        LightAttacking,
        HeavyAttacking,
        SpecialAttacking
    }
    private PlayerState currentState = PlayerState.Normal;
    private float currentStateTime = 0f;
    private float rollCooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;

        groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.parent = transform;
        groundCheck.localPosition = new Vector3(0, -0.5f, 0);

        // Cache the layers
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    private void Update()
    {
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        UpdateGroundState();
        HandleStateTimer();
        HandleInputs();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        ApplyMovement();
    }

    private void UpdateGroundState()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);
    }

    public bool CanAttack()
    {
        return currentState == PlayerState.Normal &&
               isGrounded &&
               currentState != PlayerState.Rolling &&
               rb.velocity.y >= -0.1f;
    }

    private void HandleStateTimer()
    {
        if (currentState != PlayerState.Normal)
        {
            currentStateTime -= Time.deltaTime;
            if (currentStateTime <= 0)
            {
                ExitCurrentState();
            }
        }

        if (rollCooldownTimer > 0)
        {
            rollCooldownTimer -= Time.deltaTime;
        }
    }

    private void HandleInputs()
    {
        // Only process new attacks if we can attack
        if (CanAttack())
        {
            // Light Attack
            if (Input.GetMouseButtonDown(0))
            {
                StartAttackState(PlayerState.LightAttacking, lightAttackDuration);
                return;
            }
            // Heavy Attack
            else if (Input.GetKeyDown(KeyCode.R))
            {
                StartAttackState(PlayerState.HeavyAttacking, heavyAttackDuration);
                return;
            }
            // Special Attack
            else if (Input.GetKeyDown(KeyCode.F))
            {
                StartAttackState(PlayerState.SpecialAttacking, specialAttackDuration);
                return;
            }
        }

        // Process movement inputs if in normal state
        if (currentState == PlayerState.Normal)
        {
            // Roll
            if (Input.GetButtonDown("Fire3") && isGrounded && rollCooldownTimer <= 0)
            {
                StartRoll();
                return;
            }
            // Jump
            else if (Input.GetButtonDown("Jump") && isGrounded)
            {
                Jump();
            }
        }
    }

    private void StartAttackState(PlayerState attackState, float duration)
    {
        currentState = attackState;
        currentStateTime = duration;

        // Set animation parameters
        animator.SetBool("IsAttacking", true);
        switch (attackState)
        {
            case PlayerState.LightAttacking:
                animator.SetInteger("AttackType", 1);
                break;
            case PlayerState.HeavyAttacking:
                animator.SetInteger("AttackType", 2);
                break;
            case PlayerState.SpecialAttacking:
                animator.SetInteger("AttackType", 3);
                break;
        }

        // Start attack coroutine
        StartCoroutine(EndAttackState(duration));
    }

    private IEnumerator EndAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentState != PlayerState.Normal)
        {
            ExitCurrentState();
        }
    }

    private void StartRoll()
    {
        currentState = PlayerState.Rolling;
        currentStateTime = rollDuration;
        rollCooldownTimer = rollCooldown;
        animator.SetTrigger("Roll");

        float rollDirection = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(rollDirection * rollSpeed, rb.velocity.y);

        // Ignore collisions with enemies during roll
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        animator.SetTrigger("Jump");
    }

    private void ExitCurrentState()
    {
        // If we're exiting the rolling state, re-enable collisions
        if (currentState == PlayerState.Rolling)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }

        // Reset all state-specific parameters
        animator.SetBool("IsAttacking", false);
        animator.SetInteger("AttackType", 0);
        currentState = PlayerState.Normal;
    }

    private void ApplyMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // Don't allow movement direction changes during attacks
        if (currentState == PlayerState.LightAttacking ||
            currentState == PlayerState.HeavyAttacking ||
            currentState == PlayerState.SpecialAttacking)
        {
            // Allow slight movement during attacks
            rb.velocity = new Vector2(rb.velocity.x * 0.95f, rb.velocity.y);
            return;
        }

        // Normal movement
        if (currentState == PlayerState.Normal)
        {
            float moveMultiplier = isGrounded ? 1f : airControlMultiplier;
            rb.velocity = new Vector2(horizontalInput * speed * moveMultiplier, rb.velocity.y);

            // Handle facing direction
            if (horizontalInput != 0)
            {
                bool shouldFaceRight = horizontalInput > 0;
                if (isFacingRight != shouldFaceRight)
                {
                    Flip();
                }
            }
        }
    }

    private void UpdateAnimator()
    {
        // Update movement animation
        float horizontalSpeed = Mathf.Abs(rb.velocity.x);
        animator.SetFloat("Speed", horizontalSpeed);

        // Update falling animation
        if (!isGrounded && rb.velocity.y < -0.1f)
        {
            animator.SetBool("IsFalling", true);
        }
        else
        {
            animator.SetBool("IsFalling", false);
        }
    }

    private void Flip()
    {
        if (currentState != PlayerState.Normal) return;

        isFacingRight = !isFacingRight;
        Vector3 newScale = originalScale;
        newScale.x = Mathf.Abs(newScale.x) * (isFacingRight ? 1 : -1);
        transform.localScale = newScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    private void OnDisable()
    {
        // Make sure collisions are re-enabled when the script is disabled
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
    }
}