using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_Weapon : MonoBehaviour
{
    public int attackDamage = 20;
    public int enragedAttackDamage = 40;
    public Vector3 attackOffset;
    public float attackRange = 1f;
    public LayerMask attackMask;

    // Audio variables
    private AudioSource audioSource;
    public AudioClip swordSlashSound;
    public AudioClip monsterRoarSound;
    public AudioClip laserChargeSound;  // Add this for laser attack

    // Roar timing variables
    public float minRoarInterval = 3f;
    public float maxRoarInterval = 7f;
    private float nextRoarTime;

    private SlimeLaserAttack laserAttack;
    private Animator animator;
    private bool isLaserAttacking = false;
    private Enemy enemyComponent;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetNextRoarTime();
        laserAttack = GetComponent<SlimeLaserAttack>();
        animator = GetComponent<Animator>();
        enemyComponent = GetComponent<Enemy>();
    }

    void Update()
    {
        if (Time.time >= nextRoarTime)
        {
            PlayRoarSound();
            SetNextRoarTime();
        }
    }

    private void SetNextRoarTime()
    {
        nextRoarTime = Time.time + Random.Range(minRoarInterval, maxRoarInterval);
    }

    private void PlayRoarSound()
    {
        if (audioSource != null && monsterRoarSound != null)
        {
            audioSource.PlayOneShot(monsterRoarSound);
        }
    }

    public void Attack()
    {
        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.PlayOneShot(swordSlashSound);
        }

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
        if (colInfo != null)
        {
            colInfo.GetComponent<Player_Health>().TakeDamage(attackDamage);
        }
    }

    public void FireLaser()
    {
        if (laserAttack != null && !isLaserAttacking)
        {
            StartCoroutine(LaserAttackSequence());
        }
    }

    private IEnumerator LaserAttackSequence()
    {
        if (isLaserAttacking) yield break;

        isLaserAttacking = true;

        // Set the bool parameter to prevent other animations
        animator.SetBool("isLaserCharging", true);
        animator.SetTrigger("LaserAttack");

        // Notify Enemy component about laser state
        if (enemyComponent != null)
        {
            enemyComponent.SetLaserState(true);
        }

        // Play laser charge sound if available
        if (audioSource != null && laserChargeSound != null)
        {
            audioSource.PlayOneShot(laserChargeSound);
        }

        // Wait for animation to reach firing point
        yield return new WaitForSeconds(1f);

        // Start the actual laser attack
        if (laserAttack != null)
        {
            laserAttack.StartLaserAttack();

            // Wait for the full duration of the laser attack
            yield return new WaitForSeconds(laserAttack.attackDuration);

            // Wait for cooldown
            yield return new WaitForSeconds(laserAttack.cooldownTime);
        }

        // Reset states
        animator.SetBool("isLaserCharging", false);
        isLaserAttacking = false;
        if (enemyComponent != null)
        {
            enemyComponent.SetLaserState(false);
        }
    }

    public void EnragedAttack()
    {
        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.PlayOneShot(swordSlashSound);
        }

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
        if (colInfo != null)
        {
            colInfo.GetComponent<Player_Health>().TakeDamage(enragedAttackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Gizmos.DrawWireSphere(pos, attackRange);
    }
}