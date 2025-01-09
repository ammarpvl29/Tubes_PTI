using UnityEngine;
using System.Collections;

public class LavaProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 15f;
    public float lifetime = 3f;
    private Rigidbody2D rb;
    private Vector2 direction;
    private CircleCollider2D projectileCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Setup collider
        projectileCollider = GetComponent<CircleCollider2D>();
        if (projectileCollider == null)
        {
            projectileCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        projectileCollider.isTrigger = true;

        // Temporarily disable collider
        projectileCollider.enabled = false;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Initialize(Vector2 newDirection, float newDamage)
    {
        direction = newDirection.normalized;
        damage = newDamage;

        // Set initial velocity
        rb.velocity = direction * speed;

        // Rotate sprite to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Enable collider after a short delay
        StartCoroutine(EnableColliderAfterDelay());

        Destroy(gameObject, lifetime);
    }

    private IEnumerator EnableColliderAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);  // Short delay to avoid initial collisions
        projectileCollider.enabled = true;
    }

    private void FixedUpdate()
    {
        // Ensure constant velocity
        rb.velocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Add explicit null check
        if (other == null) return;

        // Ignore collisions with the spawning enemy
        if (other.CompareTag("Enemy")) return;

        Debug.Log($"Projectile hit: {other.tag}"); // Debug log

        if (other.CompareTag("Player"))
        {
            Debug.Log("Hit player!"); // Debug log

            // Try to get Player_Health from parent if not found on this object
            Player_Health playerHealth = other.GetComponent<Player_Health>();
            if (playerHealth == null)
            {
                playerHealth = other.GetComponentInParent<Player_Health>();
            }

            if (playerHealth != null)
            {
                Debug.Log($"Dealing {damage} damage to player"); // Debug log
                playerHealth.TakeDamage(damage);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Could not find Player_Health component on player or its parents!");
            }
        }
        else if (other.CompareTag("Ground"))
        {
            Debug.Log("Hit ground!"); // Debug log
            Destroy(gameObject);
        }
    }
}