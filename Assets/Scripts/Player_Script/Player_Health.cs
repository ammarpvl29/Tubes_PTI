using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player_Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar healthBar;
    public GameObject deathEffect;

    private PlayerAttack playerAttack;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth((int)maxHealth); // Convert to int for the health bar
        playerAttack = GetComponent<PlayerAttack>(); // Get the PlayerAttack component
    }

    public void TakeDamage(float damage)
    {
        // Check if player is using ultimate attack
        if (playerAttack != null && playerAttack.IsInvulnerableDuringUltimate())
        {
            return; // Skip damage if player is invulnerable during ultimate
        }

        currentHealth -= damage;
        healthBar.SetHealth((int)currentHealth); // Convert to int for the health bar
        StartCoroutine(DamageAnimation());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    System.Collections.IEnumerator DamageAnimation()
    {
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < 3; i++)
        {
            foreach (SpriteRenderer sr in srs)
            {
                Color c = sr.color;
                c.a = 0;
                sr.color = c;
            }
            yield return new WaitForSeconds(.1f);
            foreach (SpriteRenderer sr in srs)
            {
                Color c = sr.color;
                c.a = 1;
                sr.color = c;
            }
            yield return new WaitForSeconds(.1f);
        }
    }
}