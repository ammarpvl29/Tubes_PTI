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

    public float hitRecoveryTime = 0.75f; // Time for hit animation to complete
    public float significantDamagePercent = 0.2f; // 20% of max health

    private bool isPerformingLaser = false;

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
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        if (currentHealth <= 0)
        {
            OnEnemyDeath?.Invoke();
            Destroy(gameObject);
            return;
        }
        if (!isPerformingLaser) 
        { 
        
        }
    }

    public void TakeDamage(int damage)
    {
        int previousHealth = currentHealth;
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        // Check if significant damage was taken
        float damagePercent = (float)damage / maxHealth;
        if (damagePercent >= significantDamagePercent)
        {
            healthBar.TriggerPhaseTransition();
        }

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
        // Don't play hit animation if performing laser
        if (isPerformingLaser) yield break;

        isHit = true;
        animator.SetTrigger("Hit");
        yield return new WaitForSeconds(hitRecoveryTime);
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

    public void SetLaserState(bool performing)
    {
        isPerformingLaser = performing;
    }
}