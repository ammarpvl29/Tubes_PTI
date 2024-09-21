using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded = false;
    private float moveHorizontal;
    private bool isFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Get input
        moveHorizontal = Input.GetAxisRaw("Horizontal");

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false; // Immediately set to false on jump
            animator.SetBool("IsJumping", true); // Play jump animation
            animator.SetBool("IsGrounded", false); // Set IsGrounded to false when jumping
        }

        // Update animations
        if (animator != null)
        {
            animator.SetBool("IsRunning", Mathf.Abs(moveHorizontal) > 0.1f);

            // Update IsJumping based on vertical velocity
            if (Mathf.Abs(rb.velocity.y) > 0.1f)
            {
                animator.SetBool("IsJumping", true);
            }
            else
            {
                animator.SetBool("IsJumping", false);
            }
        }

        // Flip character
        if (moveHorizontal > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveHorizontal < 0 && isFacingRight)
        {
            Flip();
        }
    }


    void FixedUpdate()
    {
        // Move character
        rb.velocity = new Vector2(moveHorizontal * moveSpeed, rb.velocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            animator.SetBool("IsGrounded", true); // Set IsGrounded to true when touching the ground
            animator.SetBool("IsJumping", false); // Ensure Jumping is false when grounded
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}