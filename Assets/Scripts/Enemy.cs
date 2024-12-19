using UnityEngine;

public class Enemy : MonoBehaviour
{
    public event System.Action OnEnemyDeath;
    public int maxHealth = 500;
    public int currentHealth;
    public HealthBar healthBar;
    public GameObject bloodEffectPrefab;
    public float bloodEffectDuration = 1f;
    public int sortingOrderOffset = 1;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isHit = false;
    public float hitRecoveryTime = 0.5f; // Time for hit animation to complete

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); // Get the Animator component

        if (spriteRenderer == null)
        {
            Debug.LogWarning("No SpriteRenderer found on the enemy object.");
        }
        if (animator == null)
        {
            Debug.LogWarning("No Animator found on the enemy object.");
        }
    }

    void Update()
    {
        if (currentHealth <= 0)
        {
            OnEnemyDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        // Play hit animation if we're not already in hit state
        if (!isHit && animator != null)
        {
            StartCoroutine(PlayHitAnimation());
        }

        SpawnBloodEffect();
        Debug.Log("Damage Taken. Remaining Health: " + currentHealth);
    }

    private System.Collections.IEnumerator PlayHitAnimation()
    {
        isHit = true;

        // Trigger the hit animation
        animator.SetTrigger("Hit");

        // Wait for the animation to complete
        yield return new WaitForSeconds(hitRecoveryTime);

        // Reset hit state
        isHit = false;
    }

    private void SpawnBloodEffect()
    {
        // Your existing SpawnBloodEffect code remains unchanged
        if (bloodEffectPrefab != null)
        {
            GameObject bloodInstance = Instantiate(bloodEffectPrefab, transform.position, Quaternion.identity);
            ParticleSystem particleSystem = bloodInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null && spriteRenderer != null)
                {
                    particleRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
                    particleRenderer.sortingOrder = spriteRenderer.sortingOrder + sortingOrderOffset;
                }
                float duration = particleSystem.main.duration;
                Destroy(bloodInstance, Mathf.Max(duration, bloodEffectDuration));
            }
            else
            {
                Debug.LogWarning("Blood effect prefab does not have a ParticleSystem component.");
                Destroy(bloodInstance, bloodEffectDuration);
            }
        }
        else
        {
            Debug.LogError("Blood effect prefab is not assigned in the Enemy script.");
        }
    }
}