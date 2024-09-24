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
            Jump();
        }

        // Update animations
        UpdateAnimations();

        // Flip character
        FlipCharacter();
    }

    void FixedUpdate()
    {
        // Move character horizontally
        rb.velocity = new Vector2(moveHorizontal * moveSpeed, rb.velocity.y);
    }

    void Jump()
    {
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        isGrounded = false;
        animator.SetTrigger("Jump");
    }

    void UpdateAnimations()
    {
        if (animator != null)
        {
            // Set the speed parameter in the animator
            animator.SetFloat("Speed", Mathf.Abs(moveHorizontal));

            // Update grounded state
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    void FlipCharacter()
    {
        if (moveHorizontal > 0 && !isFacingRight || moveHorizontal < 0 && isFacingRight)
        {
            isFacingRight = !isFacingRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}