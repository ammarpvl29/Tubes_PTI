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
    [SerializeField] private GameObject blueSpherePrefab;    // Reference to blue sphere prefab
    [SerializeField] private GameObject redSpherePrefab;     // Reference to red sphere prefab
    [SerializeField] private GameObject purpleSpherePrefab;  // Reference to purple sphere prefab
    public float chargeTime = 1f;              // Time to form each sphere
    public float combineTime = 0.5f;           // Time to combine spheres
    public float shootSpeed = 20f;             // New: Speed of the purple orb
    public float maxShootDistance = 10f;       // New: Maximum distance the orb can travel
    public float beamRadius = 2f;              // Beam width
    public int hollowPurpleDamage = 50;        // Damage for Hollow Purple

    [Header("Hollow Purple Positions")]
    public Transform leftSpherePosition;        // Position for red sphere
    public Transform rightSpherePosition;       // Position for blue sphere
    public Transform combinePosition;           // Position where spheres combine

    // Instance references
    private GameObject blueSphereInstance;
    private GameObject redSphereInstance;
    private GameObject purpleSphereInstance;

    // State variables
    private bool isChargingHollowPurple = false;
    private bool isFullyCharged = false;
    private bool isPlayingChargingAnimation = false;
    private AudioSource audioSource;
    private Animator animator;
    private bool isAttacking = false;

    // Animation Hash IDs
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int TriggerChargeHash = Animator.StringToHash("TriggerCharge");

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

        // Make sure any existing instances are cleaned up
        DestroyAllSpheres();
    }

    void OnDisable()
    {
        // Clean up when disabled
        DestroyAllSpheres();
    }

    private void DestroyAllSpheres()
    {
        if (blueSphereInstance) Destroy(blueSphereInstance);
        if (redSphereInstance) Destroy(redSphereInstance);
        if (purpleSphereInstance) Destroy(purpleSphereInstance);
    }


    void Update()
    {
        // Check if charging animation is playing
        bool isInChargingState = animator.GetCurrentAnimatorStateInfo(0).IsName("Charging");

        // Only allow new inputs if not in charging state or if charging is complete
        if (!isInChargingState || isFullyCharged)
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
            // Hollow Purple input handling
            if (Input.GetKey(KeyCode.F) && !isAttacking && !isChargingHollowPurple && !isPlayingChargingAnimation)
            {
                StartCoroutine(ChargeHollowPurple());
            }
        }

        // Handle releasing F key
        if (Input.GetKeyUp(KeyCode.F))
        {
            if (isFullyCharged)
            {
                StartCoroutine(ShootPurpleOrb());
            }
            StopHollowPurpleCharging();
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
        isPlayingChargingAnimation = true;

        animator.SetBool(IsAttackingHash, true);
        animator.SetInteger(AttackTypeHash, 3);
        animator.SetTrigger(TriggerChargeHash);

        yield return null;

        float chargeProgress = 0f;

        while (Input.GetKey(KeyCode.F) && chargeProgress < chargeTime * 2)
        {
            chargeProgress += Time.deltaTime;

            // Spawn blue sphere at half charge
            if (chargeProgress >= chargeTime && blueSphereInstance == null)
            {
                blueSphereInstance = Instantiate(blueSpherePrefab, rightSpherePosition.position, Quaternion.identity);
                if (hollowPurpleChargeSound)
                {
                    audioSource.PlayOneShot(hollowPurpleChargeSound);
                }
            }

            // Spawn red sphere at full charge
            if (chargeProgress >= chargeTime * 2 && redSphereInstance == null)
            {
                redSphereInstance = Instantiate(redSpherePrefab, leftSpherePosition.position, Quaternion.identity);

                // Start combining spheres
                StartCoroutine(MoveSphereToPosition(blueSphereInstance, combinePosition.position));
                StartCoroutine(MoveSphereToPosition(redSphereInstance, combinePosition.position));

                yield return new WaitForSeconds(combineTime);

                // Combine into purple sphere and immediately shoot
                Destroy(blueSphereInstance);
                Destroy(redSphereInstance);

                purpleSphereInstance = Instantiate(purpleSpherePrefab, combinePosition.position, Quaternion.identity);
                StartCoroutine(ShootPurpleOrb());

                isFullyCharged = true;
            }

            yield return null;
        }

        isPlayingChargingAnimation = false;

        if (!isFullyCharged)
        {
            StopHollowPurpleCharging();
        }
    }

    IEnumerator MoveSphereToPosition(GameObject sphere, Vector3 targetPos)
    {
        if (sphere == null) yield break;

        Vector3 startPos = sphere.transform.position;
        float elapsedTime = 0;

        // Get all particle systems in the sphere
        ParticleSystem[] particles = sphere.GetComponentsInChildren<ParticleSystem>();
        Light sphereLight = sphere.GetComponentInChildren<Light>();
        float initialLightIntensity = sphereLight ? sphereLight.intensity : 0;

        while (elapsedTime < combineTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / combineTime;

            // Smooth step for more dramatic movement
            t = t * t * (3f - 2f * t);

            // Move the sphere
            sphere.transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Scale particle effects for dramatic effect
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.startSizeMultiplier = Mathf.Lerp(1f, 1.5f, t);
            }

            // Intensify light as spheres combine
            if (sphereLight)
            {
                sphereLight.intensity = Mathf.Lerp(initialLightIntensity, initialLightIntensity * 1.5f, t);
            }

            yield return null;
        }
    }

    void StopHollowPurpleCharging()
    {
        DestroyAllSpheres();

        isChargingHollowPurple = false;
        isFullyCharged = false;
        isPlayingChargingAnimation = false;
        animator.SetBool(IsAttackingHash, false);
        animator.SetInteger(AttackTypeHash, 0);
    }

    IEnumerator ShootPurpleOrb()
    {
        if (purpleSphereInstance == null) yield break;

        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector3 startPosition = purpleSphereInstance.transform.position;
        float distanceTraveled = 0f;

        if (hollowPurpleFireSound)
        {
            audioSource.PlayOneShot(hollowPurpleFireSound);
        }

        while (distanceTraveled < maxShootDistance && purpleSphereInstance != null)
        {
            float moveStep = shootSpeed * Time.deltaTime;
            purpleSphereInstance.transform.position += (Vector3)(direction * moveStep);
            distanceTraveled += moveStep;

            // Check for enemies along the path
            Collider2D[] hits = Physics2D.OverlapCircleAll(purpleSphereInstance.transform.position, beamRadius, whatIsEnemies);
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent<Enemy>(out var enemy))
                {
                    enemy.TakeDamage(hollowPurpleDamage);
                }
            }

            yield return null;
        }

        // Destroy the orb at the end of its journey
        if (purpleSphereInstance != null)
        {
            Destroy(purpleSphereInstance);
        }

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

        // Draw Hollow Purple range in the correct direction
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector3 centerPoint = attackPos.position + new Vector3(direction.x * maxShootDistance / 2, 0, 0);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(centerPoint, new Vector3(maxShootDistance, beamRadius * 2, 0.1f));
    }
}