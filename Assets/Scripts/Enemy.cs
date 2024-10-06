using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 100;
    public GameObject bloodEffectPrefab;
    public float bloodEffectDuration = 1f;
    public int sortingOrderOffset = 1;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("No SpriteRenderer found on the enemy object.");
        }
    }

    void Update()
    {
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        SpawnBloodEffect();
        Debug.Log("Damage Taken. Remaining Health: " + health);
    }

    private void SpawnBloodEffect()
    {
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