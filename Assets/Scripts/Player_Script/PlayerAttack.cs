using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [System.Serializable]
    public class AttackConfig
    {
        public int damage;
        public AudioClip sound;
        public float cooldown;
        [HideInInspector] public bool isOnCooldown;
    }

    [Header("Attack Configurations")]
    [SerializeField] private AttackConfig basicAttack = new() { damage = 10, cooldown = 0.5f };
    [SerializeField] private AttackConfig specialAttack = new() { damage = 20, cooldown = 1f };
    [SerializeField] private AttackConfig hollowPurple = new() { damage = 50, cooldown = 3f };
    [SerializeField] private AttackConfig ultimateAttack = new() { damage = 75, cooldown = 10f }; // High damage, long cooldown


    [Header("Combat Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRadius = 1f;

    [Header("Ultimate Attack Settings")]
    [SerializeField] private UltimateAttackConfig ultimateConfig;


    [Header("Hollow Purple Settings")]
    [SerializeField] private HollowPurpleConfig hollowPurpleConfig;

    [Header("Enemy Reference")]
    [SerializeField] public Enemy enemyBoss;  // Reference to the boss enemy
    private bool isHollowPurpleAvailable = false;  // Track if hollow purple can be used

    private PlayerMovement playerMovement;
    private AudioSource audioSource;
    private Animator animator;
    private bool isAttacking;
    private bool isChargingHollowPurple;
    private bool isFullyCharged;
    private bool isPlayingChargingAnimation;

    // Cache animator parameters
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int TriggerChargeHash = Animator.StringToHash("TriggerCharge");

    // Add these public methods to your PlayerAttack class
    public bool IsBasicAttackOnCooldown() => basicAttack.isOnCooldown;
    public bool IsSpecialAttackOnCooldown() => specialAttack.isOnCooldown;
    public bool IsHollowPurpleOnCooldown() => hollowPurple.isOnCooldown;

    public float GetBasicAttackCooldown() => basicAttack.cooldown;
    public float GetSpecialAttackCooldown() => specialAttack.cooldown;
    public float GetHollowPurpleCooldown() => hollowPurple.cooldown;

    public bool IsChargingHollowPurple() => isChargingHollowPurple;

    [System.Serializable]
    private class HollowPurpleConfig
    {
        public GameObject blueSpherePrefab;
        public GameObject redSpherePrefab;
        public GameObject purpleSpherePrefab;
        public Transform leftSpherePosition;
        public Transform rightSpherePosition;
        public Transform combinePosition;
        public float chargeTime = 1f;
        public float combineTime = 0.5f;
        public float shootSpeed = 20f;
        public float maxDistance = 10f;
        public float beamRadius = 2f;
        public AudioClip chargeSound;
        public AudioClip fireSound;

        [HideInInspector] public GameObject blueInstance;
        [HideInInspector] public GameObject redInstance;
        [HideInInspector] public GameObject purpleInstance;
    }

    [System.Serializable]
    private class UltimateAttackConfig
    {
        public float attackDuration = 1.5f; // Duration of the entire attack animation
        public float damageDelay = 0.8f; // When during the animation to apply damage
        public float knockbackForce = 10f; // Force applied to enemies
        public bool requiresGrounded = true; // Whether attack can only be performed on ground
        public ParticleSystem ultimateVFX; // Optional VFX for the ultimate
        public AudioClip chargeSound; // Sound played when starting the attack
        public AudioClip impactSound; // Sound played on impact
    }

    // Add reference to the player's health component
    private Player_Health playerHealth; // Add this line
    private bool isUsingUltimate = false; // Add this to track ultimate state

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<Player_Health>();

        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;

        playerMovement = GetComponent<PlayerMovement>();
    }
    public bool IsInvulnerableDuringUltimate()
    {
        return isUsingUltimate;
    }

    private bool CanPerformActions()
    {
        if (playerMovement == null) return false;
        return playerMovement.CanAttack();
    }

    private void OnDisable() => CleanupHollowPurple();
    public bool IsUltimateAttackOnCooldown() => ultimateAttack.isOnCooldown;
    public float GetUltimateAttackCooldown() => ultimateAttack.cooldown;


    private void Update()
    {
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        // Check hollow purple availability based on enemy health
        CheckHollowPurpleAvailability();

        // Check if in charging animation
        bool isInChargingState = animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Powering");

        // Only allow new inputs if not in charging state or if charging is complete
        if (!isInChargingState || isFullyCharged)
        {
            // Only process attacks if we can perform actions
            if (CanPerformActions())
            {
                // Basic Attack
                if (Input.GetMouseButtonDown(0) && !basicAttack.isOnCooldown && !isAttacking && !isChargingHollowPurple)
                {
                    PerformAttack(1, basicAttack);
                }
                // Special Attack
                else if (Input.GetKeyDown(KeyCode.R) && !specialAttack.isOnCooldown && !isAttacking && !isChargingHollowPurple)
                {
                    PerformAttack(2, specialAttack);
                }
                // Hollow Purple - only if available and enemy health is below 50%
                else if (Input.GetKey(KeyCode.F) && !isAttacking && !isChargingHollowPurple
                        && !isPlayingChargingAnimation && isHollowPurpleAvailable)
                {
                    StartCoroutine(ChargeHollowPurple());
                }
                if (Input.GetKeyDown(KeyCode.Q) && !ultimateAttack.isOnCooldown && !isAttacking && !isChargingHollowPurple && (!ultimateConfig.requiresGrounded))
                {
                    StartCoroutine(PerformUltimateAttack());
                }
            }
        }

        // Handle releasing F key - allowed even when can't perform actions to cancel charge
        if (Input.GetKeyUp(KeyCode.F))
        {
            if (isFullyCharged && CanPerformActions())
            {
                StartCoroutine(FireHollowPurple());
            }
            else
            {
                CleanupHollowPurple();
            }
        }
    }

    private void CheckHollowPurpleAvailability()
    {
        if (enemyBoss != null)
        {
            float healthPercentage = (float)enemyBoss.currentHealth / enemyBoss.maxHealth;
            isHollowPurpleAvailable = healthPercentage <= 0.5f;  // Available when health is 50% or lower
        }
    }

    private async void PerformAttack(int attackType, AttackConfig config)
    {
        // Check if attack is already on cooldown or if we're in an attack animation
        if (config.isOnCooldown || isAttacking || animator == null) return;  // Add null check

        try
        {
            // Check if we're currently in an attack animation state
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("Player_Attack1") || currentState.IsName("Player_Attack2"))
            {
                return;
            }

            isAttacking = true;
            config.isOnCooldown = true;

            // Set animation parameters
            animator.SetBool(IsAttackingHash, true);
            animator.SetInteger(AttackTypeHash, attackType);

            if (config.sound && audioSource != null) audioSource.PlayOneShot(config.sound);

            // Wait for attack animation to reach the damage frame
            await Task.Delay(100);

            // Perform the damage check
            var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Enemy>(out var enemy))
                {
                    enemy.TakeDamage(config.damage);
                }
            }

            // Wait for animation to complete (you might need to adjust this time)
            float animationLength = attackType == 1 ? 0.5f : 0.7f; // Adjust these values based on your animation lengths
            await Task.Delay((int)(animationLength * 1000));

            // Only reset if the component still exists
            if (this != null && animator != null)
            {
                ResetAttackState();
            }
            else
            {
                isAttacking = false;
            }

            // Wait for the remaining cooldown if any
            float remainingCooldown = config.cooldown - animationLength;
            if (remainingCooldown > 0)
            {
                await Task.Delay((int)(remainingCooldown * 1000));
            }

            config.isOnCooldown = false;
        }
        catch (MissingReferenceException)
        {
            // Handle the case where components are destroyed during execution
            isAttacking = false;
            config.isOnCooldown = false;
        }
    }

    private void ResetAttackState()
    {
        if (animator == null) return;  // Add null check

        // Only reset if we're not in the middle of another attack animation
        try
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("Player_Attack1") || currentState.IsName("Player_Attack2") || currentState.IsName("Player_Powering"))
            {
                animator.SetBool(IsAttackingHash, false);
                animator.SetInteger(AttackTypeHash, 0);
            }
        }
        catch (MissingReferenceException)
        {
            // Handle the case where animator is destroyed during execution
            isAttacking = false;
            return;
        }

        isAttacking = false;
    }

    private IEnumerator PerformUltimateAttack()
    {
        isAttacking = true;
        ultimateAttack.isOnCooldown = true;
        isUsingUltimate = true;

        // Play charge sound
        if (ultimateConfig.chargeSound)
        {
            audioSource.PlayOneShot(ultimateConfig.chargeSound);
        }

        // Start VFX if assigned
        if (ultimateConfig.ultimateVFX != null)
        {
            ultimateConfig.ultimateVFX.Play();
        }

        // Set animation parameters
        animator.SetBool(IsAttackingHash, true);
        animator.SetInteger(AttackTypeHash, 4); // Use 4 for ultimate attack

        // Wait for the damage timing
        yield return new WaitForSeconds(ultimateConfig.damageDelay);

        // Play impact sound
        if (ultimateConfig.impactSound)
        {
            audioSource.PlayOneShot(ultimateConfig.impactSound);
        }

        // Perform the damage check
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(ultimateAttack.damage);

                // Apply knockback
                if (hit.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 knockbackDirection = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(knockbackDirection * ultimateConfig.knockbackForce, ForceMode2D.Impulse);
                }
            }
        }

        // Wait for the remaining animation
        yield return new WaitForSeconds(ultimateConfig.attackDuration - ultimateConfig.damageDelay);

        // Reset attack state
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, false);
            animator.SetInteger(AttackTypeHash, 0);
        }
        isAttacking = false;
        isUsingUltimate = false;

        // Wait for cooldown
        yield return new WaitForSeconds(ultimateAttack.cooldown);
        ultimateAttack.isOnCooldown = false;
    }

    private IEnumerator ChargeHollowPurple()
    {
        var config = hollowPurpleConfig;
        isChargingHollowPurple = true;
        isPlayingChargingAnimation = true;

        animator.SetBool(IsAttackingHash, true);
        animator.SetInteger(AttackTypeHash, 3);
        animator.SetTrigger(TriggerChargeHash);

        float chargeProgress = 0f;

        while (Input.GetKey(KeyCode.F) && chargeProgress < config.chargeTime * 2)
        {
            chargeProgress += Time.deltaTime;

            if (chargeProgress >= config.chargeTime && !config.blueInstance)
            {
                config.blueInstance = SpawnSphere(config.blueSpherePrefab, config.rightSpherePosition);
                PlaySound(config.chargeSound);
            }

            if (chargeProgress >= config.chargeTime * 2 && !config.redInstance)
            {
                config.redInstance = SpawnSphere(config.redSpherePrefab, config.leftSpherePosition);
                yield return CombineSpheres();
                isFullyCharged = true;
                break;
            }

            yield return null;
        }

        isPlayingChargingAnimation = false;

        if (!isFullyCharged)
        {
            CleanupHollowPurple();
        }
    }

    private IEnumerator CombineSpheres()
    {
        var config = hollowPurpleConfig;

        StartCoroutine(MoveSphere(config.blueInstance, config.combinePosition.position));
        StartCoroutine(MoveSphere(config.redInstance, config.combinePosition.position));

        yield return new WaitForSeconds(config.combineTime);

        Destroy(config.blueInstance);
        Destroy(config.redInstance);
        config.purpleInstance = SpawnSphere(config.purpleSpherePrefab, config.combinePosition);
    }

    private IEnumerator FireHollowPurple()
    {
        var config = hollowPurpleConfig;
        if (config.purpleInstance == null) yield break;

        PlaySound(config.fireSound);

        // Reset animation state immediately when firing
        animator.SetBool(IsAttackingHash, false);
        animator.SetInteger(AttackTypeHash, 0);

        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float distanceTraveled = 0f;

        while (distanceTraveled < config.maxDistance && config.purpleInstance != null)
        {
            float step = config.shootSpeed * Time.deltaTime;
            config.purpleInstance.transform.position += (Vector3)(direction * step);
            distanceTraveled += step;

            var hits = Physics2D.OverlapCircleAll(config.purpleInstance.transform.position,
                                                config.beamRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Enemy>(out var enemy))
                {
                    enemy.TakeDamage(hollowPurple.damage);
                }
            }

            yield return null;
        }

        CleanupHollowPurple();
        StartCooldown(hollowPurple);
    }

    private async void StartCooldown(AttackConfig attack)
    {
        attack.isOnCooldown = true;
        await Task.Delay((int)(attack.cooldown * 1000));
        attack.isOnCooldown = false;
    }

    private GameObject SpawnSphere(GameObject prefab, Transform position) =>
        Instantiate(prefab, position.position, Quaternion.identity);

    private IEnumerator MoveSphere(GameObject sphere, Vector3 targetPos)
    {
        if (!sphere) yield break;

        Vector3 startPos = sphere.transform.position;
        float elapsed = 0f;

        while (elapsed < hollowPurpleConfig.combineTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hollowPurpleConfig.combineTime;
            t = t * t * (3f - 2f * t);

            sphere.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip) audioSource.PlayOneShot(clip);
    }

    private void CleanupHollowPurple()
    {
        if (hollowPurpleConfig.blueInstance) Destroy(hollowPurpleConfig.blueInstance);
        if (hollowPurpleConfig.redInstance) Destroy(hollowPurpleConfig.redInstance);
        if (hollowPurpleConfig.purpleInstance) Destroy(hollowPurpleConfig.purpleInstance);

        isChargingHollowPurple = false;
        isFullyCharged = false;
        isPlayingChargingAnimation = false;
        ResetAttackState();
    }

    private void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);

        if (hollowPurpleConfig != null)
        {
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Vector3 center = attackPoint.position + new Vector3(direction.x * hollowPurpleConfig.maxDistance / 2, 0, 0);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(center, new Vector3(hollowPurpleConfig.maxDistance,
                                                   hollowPurpleConfig.beamRadius * 2, 0.1f));
        }
    }

    public bool IsHollowPurpleAvailable()
    {
        return isHollowPurpleAvailable && CanPerformActions();
    }
}