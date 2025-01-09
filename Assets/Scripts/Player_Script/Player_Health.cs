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
        Debug.Log($"Player taking {damage} damage"); // Debug log

        // Check if player is using ultimate attack
        if (playerAttack != null && playerAttack.IsInvulnerableDuringUltimate())
        {
            Debug.Log("Player is invulnerable - no damage taken"); // Debug log
            return;
        }

        currentHealth -= damage;
        Debug.Log($"Player health now: {currentHealth}"); // Debug log

        healthBar.SetHealth((int)currentHealth);
        StartCoroutine(DamageAnimation());

        if (currentHealth <= 0)
        {
            Debug.Log("Player died"); // Debug log
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