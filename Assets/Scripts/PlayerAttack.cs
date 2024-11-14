using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if(timeBtwAttack <= 0) 
        {
            if (Input.GetKey(KeyCode.Mouse0) && !isAttacking) 
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
        animator.SetTrigger("Attack");

        // Spawn slash effect
        SpawnSlashEffect();

        // Apply damage
        Collider2D[] enemiesAttack = Physics2D.OverlapCircleAll(attackPos.position, attackRange, whatIsEnemies);
        for(int i = 0; i < enemiesAttack.Length; i++) 
        {
            enemiesAttack[i].GetComponent<Enemy>().TakeDamage(damage);
        }

        timeBtwAttack = startTimeBtwAttack;
        yield return new WaitForSeconds(attackAnimationDuration);
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
        SlashEffect slashEffectScript = slashEffect.GetComponent<SlashEffect>();
        if (slashEffectScript != null)
        {
            // Set the customization values
            slashEffectScript.moveSpeed = slashMoveSpeed;
            slashEffectScript.moveDistance = slashMoveDistance;
        }
        
        // Flip the effect based on player's facing direction
        if(transform.localScale.x < 0)
        {
            slashEffect.transform.localScale = new Vector3(
                -slashEffect.transform.localScale.x,
                slashEffect.transform.localScale.y,
                slashEffect.transform.localScale.z
            );
        }

        // Destroy the effect after duration
        Destroy(slashEffect, effectDuration);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(attackPos.position, attackRange);
    }
}