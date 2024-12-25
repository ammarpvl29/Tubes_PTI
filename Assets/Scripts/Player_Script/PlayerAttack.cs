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

    [Header("Combat Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackRadius = 1f;

    [Header("Hollow Purple Settings")]
    [SerializeField] private HollowPurpleConfig hollowPurpleConfig;

    [Header("Enemy Reference")]
    [SerializeField] private Enemy enemyBoss;  // Reference to the boss enemy
    private bool isHollowPurpleAvailable = false;  // Track if hollow purple can be used

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

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
    }

    private void OnDisable() => CleanupHollowPurple();

    private void Update()
    {
        // Check hollow purple availability based on enemy health
        CheckHollowPurpleAvailability();

        // Check if in charging animation
        bool isInChargingState = animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Powering");

        // Only allow new inputs if not in charging state or if charging is complete
        if (!isInChargingState || isFullyCharged)
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
        }

        // Handle releasing F key
        if (Input.GetKeyUp(KeyCode.F))
        {
            if (isFullyCharged)
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
        if (config.isOnCooldown || isAttacking) return;

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

        if (config.sound) audioSource.PlayOneShot(config.sound);

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

        // Reset attack state
        ResetAttackState();

        // Wait for the remaining cooldown if any
        float remainingCooldown = config.cooldown - animationLength;
        if (remainingCooldown > 0)
        {
            await Task.Delay((int)(remainingCooldown * 1000));
        }

        config.isOnCooldown = false;
    }

    private void ResetAttackState()
    {
        // Only reset if we're not in the middle of another attack animation
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName("Player_Attack1") || currentState.IsName("Player_Attack2") || currentState.IsName("Player_Powering"))
        {
            animator.SetBool(IsAttackingHash, false);
            animator.SetInteger(AttackTypeHash, 0);
        }
        isAttacking = false;
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

    // Called by Animation Event at the end of attack animations
    public void OnAttackAnimationComplete()
    {
        ResetAttackState();
    }

    public bool IsHollowPurpleAvailable()
    {
        return isHollowPurpleAvailable;
    }
}