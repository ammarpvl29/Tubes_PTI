using UnityEngine;
public class SlimeLaserAttack : MonoBehaviour
{
    [System.Serializable]
    public class LaserSettings
    {
        public float damage = 30f;
        public float width = 0.3f;
        public float length = 10f;
        public float warmupTime = 1f;
        public float attackDuration = 3f;
        public float cooldownTime = 5f;
    }

    [System.Serializable]
    public class RotationSettings
    {
        public bool enableRotation = true;
        public float speed = 45f;
        public float startDelay = 0.5f;
        public float maxAngle = 60f;
    }

    [System.Serializable]
    public class VFXSettings
    {
        public GameObject laserVFXPrefab;
        public Vector3 vfxOffset = Vector3.zero;
        public bool flipXWithDirection = true;
        public bool adjustScaleWithLaser = true;
        public float baseScale = 1f;
    }

    [Header("Core Settings")]
    [SerializeField] private LaserSettings laserSettings;
    [SerializeField] private RotationSettings rotationSettings;
    [SerializeField] private VFXSettings vfxSettings;

    [Header("References")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private Transform laserOrigin;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Transform playerTransform;
    public LaserSettings Settings => laserSettings;

    private GameObject activeLaser;
    private GameObject activeVFX;
    private LineRenderer laserLine;
    private ParticleSystem[] laserParticles;
    private Animator animator;
    private Animator vfxAnimator;
    private static readonly int LaserAttackHash = Animator.StringToHash("LaserAttack");
    private static readonly int IsLaserChargingHash = Animator.StringToHash("isLaserCharging");
    private static readonly int PlayVFXHash = Animator.StringToHash("PlayVFX");

    private bool isAttacking;
    private float warmupProgress;
    private float attackTimer;
    private Vector3 laserDirection;
    private float currentRotationAngle;
    private int rotationDirection = 1;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        laserOrigin ??= transform;
    }

    public void StartLaserAttack()
    {
        if (isAttacking || !enabled) return;

        isAttacking = true;
        warmupProgress = 0f;
        attackTimer = 0f;
        currentRotationAngle = 0f;
        rotationDirection = 1;

        // Set animation parameters
        animator.SetTrigger(LaserAttackHash);
        animator.SetBool(IsLaserChargingHash, true);

        // Calculate initial direction
        laserDirection = (playerTransform != null)
            ? (playerTransform.position - transform.position).normalized
            : transform.right * Mathf.Sign(transform.localScale.x);

        InitializeLaser();
        InitializeVFX();
    }

    private void InitializeVFX()
    {
        if (!vfxSettings.laserVFXPrefab) return;

        // Spawn VFX object
        activeVFX = Instantiate(vfxSettings.laserVFXPrefab, laserOrigin.position, Quaternion.identity, laserOrigin);
        activeVFX.transform.localPosition = vfxSettings.vfxOffset;

        // Get and configure VFX animator
        vfxAnimator = activeVFX.GetComponent<Animator>();
        if (vfxAnimator)
        {
            // Ensure the animation doesn't play automatically
            vfxAnimator.keepAnimatorStateOnDisable = true;
            vfxAnimator.SetTrigger(PlayVFXHash);
        }

        UpdateVFXTransform(laserDirection);
    }

    private void UpdateVFXTransform(Vector3 direction)
    {
        if (!activeVFX) return;

        // Update position relative to laser origin
        activeVFX.transform.position = laserOrigin.position + vfxSettings.vfxOffset;

        // Update rotation to match laser direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        activeVFX.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Update scale if needed
        if (vfxSettings.adjustScaleWithLaser)
        {
            float scaleMultiplier = laserSettings.length / 10f; // Assuming 10 is the base length
            activeVFX.transform.localScale = new Vector3(
                vfxSettings.baseScale * (vfxSettings.flipXWithDirection ? Mathf.Sign(direction.x) : 1),
                vfxSettings.baseScale,
                vfxSettings.baseScale
            ) * scaleMultiplier;
        }
        else if (vfxSettings.flipXWithDirection)
        {
            Vector3 scale = activeVFX.transform.localScale;
            scale.x = vfxSettings.baseScale * Mathf.Sign(direction.x);
            activeVFX.transform.localScale = scale;
        }
    }

    private void InitializeLaser()
    {
        if (!laserPrefab) return;

        activeLaser = Instantiate(laserPrefab, laserOrigin.position, Quaternion.identity, laserOrigin);
        activeLaser.transform.localPosition = Vector3.zero;

        // Cache components
        laserLine = activeLaser.GetComponent<LineRenderer>();
        laserParticles = activeLaser.GetComponentsInChildren<ParticleSystem>();

        ConfigureLaser();
    }

    private void ConfigureLaser()
    {
        if (!laserLine) return;

        laserLine.positionCount = 2;
        laserLine.useWorldSpace = true;

        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0, 1),     // Start at full width
            new Keyframe(0.6f, 1),  // Maintain width for 60%
            new Keyframe(0.8f, 0.5f), // Start tapering
            new Keyframe(1, 0)      // End at a point
        );

        laserLine.widthCurve = widthCurve;
        laserLine.widthMultiplier = laserSettings.width;
    }

    private void Update()
    {
        if (EnhancedPauseManager.Instance.IsPaused)
            return;

        if (!isAttacking) return;

        if (warmupProgress < laserSettings.warmupTime)
        {
            HandleWarmup();
        }
        else
        {
            HandleAttack();
        }
        UpdateVFXTransform(laserDirection);
    }

    private void HandleWarmup()
    {
        warmupProgress += Time.deltaTime;
        float progress = warmupProgress / laserSettings.warmupTime;

        if (laserLine)
        {
            laserLine.startWidth = laserSettings.width * progress;
            laserLine.endWidth = laserSettings.width * 0.8f * progress;
        }

        UpdateLaserVisuals(laserDirection);
    }

    private void HandleAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= laserSettings.attackDuration)
        {
            StopLaserAttack();
            return;
        }

        if (rotationSettings.enableRotation && attackTimer > rotationSettings.startDelay)
        {
            UpdateRotation();
        }

        UpdateLaserVisuals(laserDirection);
        CheckDamage();
    }

    private void UpdateRotation()
    {
        float rotationDelta = rotationSettings.speed * Time.deltaTime * rotationDirection;
        currentRotationAngle += rotationDelta;

        if (Mathf.Abs(currentRotationAngle) >= rotationSettings.maxAngle)
        {
            rotationDirection *= -1;
            currentRotationAngle = Mathf.Sign(currentRotationAngle) * rotationSettings.maxAngle;
        }

        laserDirection = Quaternion.Euler(0, 0, currentRotationAngle) * Vector3.right;
    }

    private void UpdateLaserVisuals(Vector3 direction)
    {
        if (!laserLine) return;

        Vector3 start = laserOrigin.position;
        Vector3 end = start + direction * laserSettings.length;

        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);

        UpdateParticles(start, end, direction);
    }

    private void UpdateParticles(Vector3 start, Vector3 end, Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 midPoint = (start + end) * 0.5f;

        foreach (var ps in laserParticles)
        {
            ps.transform.position = midPoint;
            var shape = ps.shape;
            shape.rotation = new Vector3(0, 0, angle);

            if (ps.gameObject.name.Contains("LaserCore"))
            {
                ConfigureLaserCoreParticles(ps, direction);
            }
        }
    }

    private void ConfigureLaserCoreParticles(ParticleSystem ps, Vector3 direction)
    {
        var localScale = ps.transform.localScale;
        localScale.x = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? 1f : 0.5f;
        localScale.y = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? 0.5f : 1f;
        ps.transform.localScale = localScale;

        var velocity = ps.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(direction.x * 5f);
        velocity.y = new ParticleSystem.MinMaxCurve(direction.y * 5f);
    }

    private void CheckDamage()
    {
        var hits = Physics2D.RaycastAll(laserOrigin.position, laserDirection, laserSettings.length, targetLayers);
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<Player_Health>(out var playerHealth))
            {
                playerHealth.TakeDamage(laserSettings.damage * Time.deltaTime);
            }
        }
    }

    private void StopLaserAttack()
    {
        isAttacking = false;
        animator.SetBool(IsLaserChargingHash, false);

        if (activeLaser)
        {
            float maxDuration = 0f;
            foreach (var ps in laserParticles)
            {
                ps.Stop();
                maxDuration = Mathf.Max(maxDuration, ps.main.duration);
            }
            Destroy(activeLaser, maxDuration);
        }

        // Handle VFX cleanup
        if (activeVFX)
        {
            // Get the length of the VFX animation
            if (vfxAnimator != null)
            {
                float animationLength = vfxAnimator.GetCurrentAnimatorStateInfo(0).length;
                Destroy(activeVFX, animationLength);
            }
            else
            {
                Destroy(activeVFX);
            }
        }

        Invoke(nameof(ResetAttack), laserSettings.cooldownTime);
    }

    private void ResetAttack()
    {
        // Ready for next attack
    }

    private void OnDestroy()
    {
        if (activeLaser)
        {
            Destroy(activeLaser);
        }
    }
}