using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Basic Attack Settings")]
    public Transform attackPos;
    public LayerMask whatIsEnemies;
    public float attackRange;
    public int basicAttackDamage = 10;
    public int specialAttackDamage = 20;

    [Header("Sound Effects")]
    public AudioClip basicAttackSound;
    public AudioClip specialAttackSound;
    public AudioClip hollowPurpleChargeSound;  // New sound for charging
    public AudioClip hollowPurpleFireSound;    // New sound for firing

    [Header("Hollow Purple Settings")]
    public ParticleSystem blueSphereEffect;    // Positive energy sphere
    public ParticleSystem redSphereEffect;     // Negative energy sphere
    public ParticleSystem purpleSphereEffect;  // Combined sphere
    public ParticleSystem purpleBeamEffect;    // The final beam
    public float chargeTime = 1f;              // Time to form each sphere
    public float combineTime = 0.5f;           // Time to combine spheres
    public float beamDuration = 1.5f;          // How long the beam lasts
    public float beamRange = 10f;              // How far the beam reaches
    public float beamRadius = 2f;              // Beam width
    public int hollowPurpleDamage = 50;        // Damage for Hollow Purple

    [Header("Hollow Purple Positions")]
    public Transform leftSpherePosition;        // Position for red sphere
    public Transform rightSpherePosition;       // Position for blue sphere
    public Transform combinePosition;           // Position where spheres combine

    private bool isChargingHollowPurple = false;
    private bool isFullyCharged = false;  // New variable to track charge state

    private AudioSource audioSource;
    private Animator animator;
    private bool isAttacking = false;

    // Animation Hash IDs
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
        }

        // Initially disable all particle systems
        if (blueSphereEffect) blueSphereEffect.Stop();
        if (redSphereEffect) redSphereEffect.Stop();
        if (purpleSphereEffect) purpleSphereEffect.Stop();
        if (purpleBeamEffect) purpleBeamEffect.Stop();

    }

    void Update()
    {
        // Basic Attack (Left Click)
        if (Input.GetKeyDown(KeyCode.Mouse0) && !isAttacking && !isChargingHollowPurple)
        {
            StartCoroutine(PerformAttack(1, basicAttackDamage, basicAttackSound));
        }
        // Special Attack (R key)
        if (Input.GetKeyDown(KeyCode.R) && !isAttacking && !isChargingHollowPurple)
        {
            StartCoroutine(PerformAttack(2, specialAttackDamage, specialAttackSound));
        }
        // Hollow Purple (Hold F to charge, release to fire)
        if (Input.GetKeyDown(KeyCode.F) && !isAttacking && !isChargingHollowPurple)
        {
            StartCoroutine(ChargeHollowPurple());
        }
        if (Input.GetKeyUp(KeyCode.F) && isChargingHollowPurple)
        {
            if (isFullyCharged)  // Only fire if fully charged
            {
                StartCoroutine(FireHollowPurple());
            }
            else  // If released too early, cancel the charging
            {
                StopHollowPurpleCharging();
            }
        }
    }

    IEnumerator PerformAttack(int attackType, int attackDamage, AudioClip attackSound)
    {
        // Your existing PerformAttack code...
        isAttacking = true;
        animator.SetBool(IsAttackingHash, true);
        animator.SetInteger(AttackTypeHash, attackType);

        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        yield return new WaitForSeconds(0.1f);

        Collider2D[] enemiesAttack = Physics2D.OverlapCircleAll(attackPos.position, attackRange, whatIsEnemies);
        foreach (Collider2D enemyCollider in enemiesAttack)
        {
            if (enemyCollider.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length - 0.1f);

        animator.SetBool(IsAttackingHash, false);
        animator.SetInteger(AttackTypeHash, 0);
        isAttacking = false;
    }

    IEnumerator ChargeHollowPurple()
    {
        isChargingHollowPurple = true;
        isFullyCharged = false;

        // Start charging animation if you have one
        animator.SetInteger(AttackTypeHash, 3);

        // Spawn blue sphere (positive energy)
        if (blueSphereEffect)
        {
            blueSphereEffect.transform.position = rightSpherePosition.position;
            blueSphereEffect.Play();
        }
        if (hollowPurpleChargeSound)
        {
            audioSource.PlayOneShot(hollowPurpleChargeSound);
        }

        yield return new WaitForSeconds(chargeTime);

        // Spawn red sphere (negative energy)
        if (redSphereEffect)
        {
            redSphereEffect.transform.position = leftSpherePosition.position;
            redSphereEffect.Play();
        }

        yield return new WaitForSeconds(chargeTime);

        // Move spheres to combine position
        StartCoroutine(MoveSphereToPosition(blueSphereEffect, combinePosition.position));
        StartCoroutine(MoveSphereToPosition(redSphereEffect, combinePosition.position));

        yield return new WaitForSeconds(combineTime);

        // Stop individual spheres and start purple sphere
        if (blueSphereEffect) blueSphereEffect.Stop();
        if (redSphereEffect) redSphereEffect.Stop();
        if (purpleSphereEffect)
        {
            purpleSphereEffect.transform.position = combinePosition.position;
            purpleSphereEffect.Play();
        }

        isFullyCharged = true;  // Mark as fully charged after spheres combine
    }

    IEnumerator MoveSphereToPosition(ParticleSystem sphere, Vector3 targetPos)
    {
        if (sphere == null) yield break;

        Vector3 startPos = sphere.transform.position;
        float elapsedTime = 0;

        while (elapsedTime < combineTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / combineTime;

            // Use smooth step for more dramatic movement
            t = t * t * (3f - 2f * t);

            sphere.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
    }

    void StopHollowPurpleCharging()
    {
        // Stop all particle effects
        if (blueSphereEffect) blueSphereEffect.Stop();
        if (redSphereEffect) redSphereEffect.Stop();
        if (purpleSphereEffect) purpleSphereEffect.Stop();

        // Reset states
        isChargingHollowPurple = false;
        isFullyCharged = false;
        animator.SetInteger(AttackTypeHash, 0);
    }

    IEnumerator FireHollowPurple()
    {
        if (!isFullyCharged) yield break;  // Safety check

        // Get the direction based on player's scale
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        // Stop the purple sphere and start the beam
        if (purpleSphereEffect) purpleSphereEffect.Stop();
        if (purpleBeamEffect)
        {
            purpleBeamEffect.transform.position = combinePosition.position;
            purpleBeamEffect.transform.right = direction;
            purpleBeamEffect.Play();
        }

        if (hollowPurpleFireSound)
        {
            audioSource.PlayOneShot(hollowPurpleFireSound);
        }

        // Deal damage in a line
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            combinePosition.position,
            new Vector2(beamRadius, beamRadius),
            0f,
            direction,
            beamRange,
            whatIsEnemies
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(hollowPurpleDamage);
            }
        }

        yield return new WaitForSeconds(beamDuration);

        // Stop all effects
        if (purpleBeamEffect) purpleBeamEffect.Stop();

        // Reset animation and states
        animator.SetInteger(AttackTypeHash, 0);
        isChargingHollowPurple = false;
        isFullyCharged = false;
    }

    // Also update the OnDrawGizmosSelected to show correct direction
    void OnDrawGizmosSelected()
    {
        // Draw basic attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);

        // Get the direction for the beam
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        // Draw Hollow Purple range in the correct direction
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(
            attackPos.position + new Vector3(direction.x * beamRange / 2, 0, 0),
            new Vector3(beamRange, beamRadius * 2, 0.1f)
        );
    }
}