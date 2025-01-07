using UnityEngine;

public class LavaAttack : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public float animationSpeed = 0.1f;
    public float damagePerSecond = 20f;
    public float damageTickRate = 0.5f;
    public LayerMask playerLayer;

    private Sprite[] animationFrames;
    private float frameTimer;
    private int currentFrame;
    private float damageTimer;
    private Player_Health playerHealth;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Load all sprites from gasburner1 to gasburner8
        animationFrames = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            animationFrames[i] = Resources.Load<Sprite>($"gasburner{i + 1}");
        }
    }

    void Update()
    {
        // Animation
        frameTimer += Time.deltaTime;
        if (frameTimer >= animationSpeed)
        {
            frameTimer = 0;
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[currentFrame];
        }

        // Damage
        damageTimer += Time.deltaTime;
        if (damageTimer >= damageTickRate)
        {
            damageTimer = 0;
            // Check for player in the damage area
            Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, 0.5f, playerLayer);
            if (playerCollider != null)
            {
                if (playerHealth == null)
                {
                    playerHealth = playerCollider.GetComponent<Player_Health>();
                }

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damagePerSecond * damageTickRate);
                }
            }
        }
    }
}