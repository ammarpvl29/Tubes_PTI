using UnityEngine;
using System.Collections;

public class Enemy_AI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public bool isFlipped = false;

    [Header("Attack Settings")]
    public float laserAttackRange = 10f;    // Maximum range for laser attack
    public float minAttackInterval = 3f;     // Minimum time between attacks
    public float maxAttackInterval = 6f;     // Maximum time between attacks

    private Boss_Weapon bossWeapon;
    private SlimeLaserAttack laserAttack;
    private float nextAttackTime;
    private bool isAttacking = false;
    private Animator animator;

    private void Start()
    {
        bossWeapon = GetComponent<Boss_Weapon>();
        laserAttack = GetComponent<SlimeLaserAttack>();
        animator = GetComponent<Animator>();
        SetNextAttackTime();
    }


    private void Update()
    {
        if (!isAttacking && Time.time >= nextAttackTime)
        {
            if (IsPlayerInRange())
            {
                StartCoroutine(PerformLaserAttack());
            }
        }
    }

    public void LookAtPlayer()
    {
        Vector3 flipped = transform.localScale;
        flipped.z *= -1f;

        if (transform.position.x > player.position.x && isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = false;
        }
        else if (transform.position.x < player.position.x && !isFlipped)
        {
            transform.localScale = flipped;
            transform.Rotate(0f, 180f, 0f);
            isFlipped = true;
        }
    }

    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        return distanceToPlayer <= laserAttackRange;
    }

    private void SetNextAttackTime()
    {
        nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval);
    }

    private IEnumerator PerformLaserAttack()
    {
        if (bossWeapon == null) yield break;

        isAttacking = true;
        // Face the player before attacking
        LookAtPlayer();

        // Wait a small moment after turning
        yield return new WaitForSeconds(0.2f);

        // Trigger laser animation
        animator.SetTrigger("LaserAttack");

        // Wait for animation to reach firing point (1 second)
        yield return new WaitForSeconds(1f);

        // Trigger the laser attack through Boss_Weapon
        bossWeapon.FireLaser();

        // Hold the animation until laser attack is complete
        yield return new WaitForSeconds(laserAttack != null ? laserAttack.attackDuration : 3f);

        // Wait for cooldown
        yield return new WaitForSeconds(laserAttack != null ? laserAttack.cooldownTime : 5f);

        // Reset attack state and set next attack time
        isAttacking = false;
        SetNextAttackTime();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, laserAttackRange);
    }
}