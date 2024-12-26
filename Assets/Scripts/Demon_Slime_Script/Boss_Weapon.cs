using UnityEngine;
using System.Collections;

public class Boss_Weapon : MonoBehaviour
{
    [System.Serializable]
    public class AttackSettings
    {
        public int normalDamage = 20;
        public int enragedDamage = 40;
        public Vector3 offset;
        public float range = 1f;
        public LayerMask targetMask;
    }

    [System.Serializable]
    public class AudioSettings
    {
        public AudioClip swordSlash;
        public AudioClip monsterRoar;
        public AudioClip laserCharge;
        [Range(0f, 1f)] public float slashVolume = 1f;
        [Range(0f, 1f)] public float roarVolume = 1f;
        [Range(0f, 1f)] public float laserVolume = 1f;
    }

    [Header("Attack Configuration")]
    [SerializeField] public AttackSettings attackSettings;

    [Header("Audio Configuration")]
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private float minRoarInterval = 3f;
    [SerializeField] private float maxRoarInterval = 7f;

    private AudioSource audioSource;
    private SlimeLaserAttack laserAttack;
    private Animator animator;
    private Enemy enemyComponent;
    
    private float nextRoarTime;
    private bool isLaserAttacking;
    public delegate void OnPlayerHitHandler(int damage);
    public event OnPlayerHitHandler OnPlayerHit;


    // Cached animation parameter hashes
    private static readonly int IsLaserChargingHash = Animator.StringToHash("isLaserCharging");
    private static readonly int LaserAttackHash = Animator.StringToHash("LaserAttack");

    private void Awake()
    {
        // Component caching
        audioSource = GetComponent<AudioSource>();
        laserAttack = GetComponent<SlimeLaserAttack>();
        animator = GetComponent<Animator>();
        enemyComponent = GetComponent<Enemy>();

        // Ensure we have an AudioSource
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        SetNextRoarTime();
    }

    private void Update()
    {
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        HandleRoarTimer();
    }

    private void HandleRoarTimer()
    {
        if (Time.time >= nextRoarTime)
        {
            PlaySound(audioSettings.monsterRoar, audioSettings.roarVolume);
            SetNextRoarTime();
        }
    }

    private void SetNextRoarTime()
    {
        nextRoarTime = Time.time + Random.Range(minRoarInterval, maxRoarInterval);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public void AnimationAttack()
    {
        Debug.Log("Animation Attack called"); // Debug log
        Attack(false); // Call the main Attack method with normal damage
    }

    public void Attack(bool isEnraged = false)
    {
        PlaySound(audioSettings.swordSlash, audioSettings.slashVolume);

        // Get the facing direction based on scale
        float facingDirection = Mathf.Sign(transform.localScale.x);

        // Adjust the offset X based on facing direction
        Vector3 adjustedOffset = new Vector3(
            attackSettings.offset.x * facingDirection,  // Flip X offset based on direction
            attackSettings.offset.y,                    // Keep Y offset the same
            attackSettings.offset.z                     // Keep Z offset the same
        );

        Vector3 attackPosition = transform.position +
            transform.right * adjustedOffset.x +
            transform.up * adjustedOffset.y;

        if (Physics2D.OverlapCircle(attackPosition, attackSettings.range, attackSettings.targetMask)
            is Collider2D hitCollider)
        {
            if (hitCollider.TryGetComponent<Player_Health>(out var playerHealth))
            {
                int damage = isEnraged ? attackSettings.enragedDamage : attackSettings.normalDamage;
                Debug.Log($"Player hit with damage: {damage}"); // Debug log
                playerHealth.TakeDamage(damage);
                OnPlayerHit?.Invoke(damage);
            }
        }
    }

    public void EnragedAttack() => Attack(true);

    public void FireLaser()
    {
        if (laserAttack && !isLaserAttacking)
        {
            StartCoroutine(LaserAttackSequence());
        }
    }

    // In Boss_Weapon.cs, modify the LaserAttackSequence method
    private IEnumerator LaserAttackSequence()
    {
        if (isLaserAttacking) yield break;

        isLaserAttacking = true;

        // Start laser charge sequence
        animator.SetBool(IsLaserChargingHash, true);
        animator.SetTrigger(LaserAttackHash);

        // Update enemy state
        enemyComponent?.SetLaserState(true);

        // Play charge sound
        PlaySound(audioSettings.laserCharge, audioSettings.laserVolume);

        // Wait for charge animation
        const float chargeTime = 1f;
        yield return new WaitForSeconds(chargeTime);

        if (laserAttack)
        {
            // Fire laser
            laserAttack.StartLaserAttack();

            // Wait for attack and cooldown
            float totalDuration = laserAttack.Settings.attackDuration +
                                laserAttack.Settings.cooldownTime;
            yield return new WaitForSeconds(totalDuration);
        }

        // Reset states
        animator.SetBool(IsLaserChargingHash, false);
        enemyComponent?.SetLaserState(false);
        isLaserAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        Gizmos.color = Color.red;
        Vector3 attackPosition = transform.position + 
            transform.right * attackSettings.offset.x + 
            transform.up * attackSettings.offset.y;
        
        Gizmos.DrawWireSphere(attackPosition, attackSettings.range);
    }

    // Public accessor for laser attack state
    public bool IsPerformingLaser => isLaserAttacking;
}