using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    private float timeBtwAttack;
    public float startTimeBtwAttack;

    public Transform attackPos;
    public LayerMask whatIsEnemies;
    public float attackRange;
    public int damage;

    // Animation variables
    private Animator animator;
    private bool isAttacking = false;
    public float attackAnimationDuration = 0.5f;

    // Slash effect variables
    public GameObject slashEffectPrefab;
    public float effectDuration = 0.3f;
    public Vector2 effectOffset = new Vector2(1f, 0f);
    
    // New variables for slash customization
    public float slashMoveSpeed = 5f;
    public float slashMoveDistance = 2f;

    private static readonly int AttackHash = Animator.StringToHash("IsAttacking");

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if(timeBtwAttack <= 0) 
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) && !isAttacking) 
            {
                StartCoroutine(PerformAttack());
            }
        }
        else 
        {
            timeBtwAttack -= Time.deltaTime;
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        animator.SetBool(AttackHash, true);

        // Spawn slash effect
        SpawnSlashEffect();

        // Apply damage
        Collider2D[] enemiesAttack = Physics2D.OverlapCircleAll(attackPos.position, attackRange, whatIsEnemies);
        for(int i = 0; i < enemiesAttack.Length; i++) 
        {
            if (enemiesAttack[i].TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
            }
        }

        timeBtwAttack = startTimeBtwAttack;
        
        // Wait for animation
        yield return new WaitForSeconds(attackAnimationDuration);
        
        // Reset attack state
        animator.SetBool(AttackHash, false);
        isAttacking = false;
    }

    void SpawnSlashEffect()
    {
        // Calculate the effect position based on player's facing direction
        Vector2 effectPosition = (Vector2)attackPos.position + 
            (transform.localScale.x > 0 ? effectOffset : new Vector2(-effectOffset.x, effectOffset.y));

        // Spawn the effect
        GameObject slashEffect = Instantiate(slashEffectPrefab, effectPosition, Quaternion.identity);
        
        // Get the SlashEffect component
        if (slashEffect.TryGetComponent<SlashEffect>(out var slashEffectScript))
        {
            // Set the customization values
            slashEffectScript.moveSpeed = slashMoveSpeed;
            slashEffectScript.moveDistance = slashMoveDistance;
        }
        
        // Flip the effect based on player's facing direction
        if(transform.localScale.x < 0)
        {
            Vector3 scale = slashEffect.transform.localScale;
            scale.x *= -1;
            slashEffect.transform.localScale = scale;
        }

        // Destroy the effect after duration
        Destroy(slashEffect, effectDuration);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);
    }
}