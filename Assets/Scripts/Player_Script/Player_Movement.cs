using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [SerializeField] private float rollDuration = 0.25f;
    [SerializeField] private float rollCooldown = 0.5f;

    private Rigidbody2D rb;
    private Vector3 originalScale;
    private bool isFacingRight = true;
    private bool isGrounded;
    private Transform groundCheck;
    private Animator animator;

    private bool isRolling = false;
    private bool canRoll = true;
    private float rollTimeLeft;
    private float rollCooldownTimeLeft;

    // Layer variables
    private int playerLayer;
    private int enemyLayer;
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        animator = GetComponent<Animator>();

        groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.parent = transform;
        groundCheck.localPosition = new Vector3(0, -0.5f, 0);

        // Cache layer numbers
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");

        // Setup contact filter for potential future use
        contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = false;
        contactFilter.useLayerMask = true;
        contactFilter.layerMask = LayerMask.GetMask("Enemy");
    }

    void Update()
    {

        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        if (!canRoll)
        {
            rollCooldownTimeLeft -= Time.deltaTime;
            if (rollCooldownTimeLeft <= 0)
            {
                canRoll = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && canRoll && !isRolling
            && !animator.GetBool("IsAttacking") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Powering"))
        {
            StartRoll();
        }

        if (isRolling)
        {
            UpdateRoll();
        }
        else
        {
            HandleNormalMovement();
        }
    }

    private void HandleNormalMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

        if (!animator.GetBool("IsAttacking"))
        {
            rb.velocity = new Vector2(horizontalInput * speed, rb.velocity.y);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !animator.GetBool("IsAttacking"))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            animator.SetTrigger("Jump");
        }

        if (!animator.GetBool("IsAttacking"))
        {
            if (horizontalInput > 0 && !isFacingRight)
            {
                Flip();
            }
            else if (horizontalInput < 0 && isFacingRight)
            {
                Flip();
            }
        }

        // Check both local and PlayerAttack states
        bool canMove = !animator.GetBool("IsAttacking") &&
                      !isRolling &&
                      !GetComponent<PlayerAttack>().IsChargingHollowPurple();

        if (canMove)
        {
            // Add air control
            float moveMultiplier = isGrounded ? 1f : 0.7f;
            rb.velocity = new Vector2(horizontalInput * speed * moveMultiplier, rb.velocity.y);
        }
        // Add movement dampening when transitioning states
        else if (!isRolling)
        {
            rb.velocity = new Vector2(rb.velocity.x * 0.9f, rb.velocity.y);
        }
    }

    private void StartRoll()
    {
        isRolling = true;
        canRoll = false;
        rollTimeLeft = rollDuration;
        rollCooldownTimeLeft = rollCooldown;

        animator.SetBool("IsAttacking", false);
        animator.ResetTrigger("TriggerCharge");
        animator.SetInteger("AttackType", 0);

        animator.SetTrigger("Roll");

        float rollDirection = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(rollDirection * rollSpeed, rb.velocity.y);

        // Ignore collisions with enemies during roll
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
    }

    private void UpdateRoll()
    {
        rollTimeLeft -= Time.deltaTime;

        float rollDirection = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(rollDirection * rollSpeed, rb.velocity.y);

        if (rollTimeLeft <= 0)
        {
            EndRoll();
        }
    }

    private void EndRoll()
    {
        isRolling = false;
        rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);

        // Re-enable collisions with enemies
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
    }

    private void Flip()
    {
        if (isRolling) return;

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

    // Optional: Handle cases where roll is interrupted
    private void OnDisable()
    {
        // Make sure collisions are re-enabled if the script is disabled
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
    }
}