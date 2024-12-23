using UnityEngine;
using static UnityEngine.GridBrushBase;

public class SlimeLaserAttack : MonoBehaviour
{
    [Header("Laser Settings")]
    public float laserDamage = 30f;
    public float rotationSpeed = 45f;
    public float warmupTime = 1f;
    public float attackDuration = 3f;
    public float cooldownTime = 5f;
    public float laserWidth = 0.3f;
    public float laserLength = 10f;  // Added to control laser length

    [Header("Rotation Control")]
    public bool shouldRotate = true;         // Toggle rotation on/off
    public float rotationDelay = 0.5f;       // Delay before rotation starts
    public float maxRotationAngle = 60f;     // Maximum rotation angle (total sweep range will be double this)

    [Header("References")]
    public GameObject slimeLaserPrefab;
    public Transform laserOrigin;
    public LayerMask targetLayers;
    public Transform playerTransform; // Reference to the player

    private GameObject currentLaser;
    private LineRenderer lineRenderer;
    private ParticleSystem[] particleSystems;
    private bool isAttacking = false;
    private float currentWarmup = 0f;
    private float attackTimer = 0f;
    private Vector3 currentDirection;
    private float currentAngle = 0f;
    private int rotationDirection = 1;

    private void Start()
    {
        if (laserOrigin == null)
        {
            laserOrigin = transform;
        }
    }

    public void StartLaserAttack()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            currentWarmup = 0f;
            attackTimer = 0f;

            // Calculate direction to player
            if (playerTransform != null)
            {
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                currentDirection = directionToPlayer;
            }
            else
            {
                // Fallback to facing direction if player reference is missing
                currentDirection = transform.right * (transform.localScale.x > 0 ? 1 : -1);
            }

            currentAngle = 0f;
            rotationDirection = 1;
            SpawnLaser();
        }
    }

    private void SpawnLaser()
    {
        if (slimeLaserPrefab != null)
        {
            // Instantiate the laser prefab
            currentLaser = Instantiate(slimeLaserPrefab, laserOrigin.position, Quaternion.identity);

            // Get components
            lineRenderer = currentLaser.GetComponent<LineRenderer>();
            particleSystems = currentLaser.GetComponentsInChildren<ParticleSystem>();

            // Setup line renderer
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.startWidth = laserWidth;
                lineRenderer.endWidth = laserWidth * 0.8f;
                lineRenderer.useWorldSpace = true;

                // Set initial positions
                lineRenderer.SetPosition(0, laserOrigin.position);
                lineRenderer.SetPosition(1, laserOrigin.position + transform.right * 10f);
            }

            // Setup particle systems
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 0f; // Ensure no gravity affects the particles

                // If it's the drip effect, configure it separately
                if (ps.gameObject.name.Contains("Drip"))
                {
                    main.gravityModifier = 1f; // Only drips should have gravity
                }
            }

            // Parent to the origin for proper movement
            currentLaser.transform.SetParent(laserOrigin);
            currentLaser.transform.localPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        if (!isAttacking) return;

        if (currentWarmup < warmupTime)
        {
            currentWarmup += Time.deltaTime;
            float warmupProgress = currentWarmup / warmupTime;

            if (lineRenderer != null)
            {
                lineRenderer.startWidth = laserWidth * warmupProgress;
                lineRenderer.endWidth = (laserWidth * 0.8f) * warmupProgress;
            }

            UpdateLaserPosition(currentDirection);
        }
        else
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackDuration)
            {
                StopLaserAttack();
                return;
            }

            if (shouldRotate && attackTimer > rotationDelay)
            {
                float rotationAmount = rotationSpeed * Time.deltaTime * rotationDirection;
                currentAngle += rotationAmount;

                if (Mathf.Abs(currentAngle) >= maxRotationAngle)
                {
                    rotationDirection *= -1;
                    currentAngle = Mathf.Sign(currentAngle) * maxRotationAngle;
                }

                currentDirection = Quaternion.Euler(0, 0, currentAngle) * Vector3.right;
            }

            UpdateLaserPosition(currentDirection);

            // Updated hit detection
            RaycastHit2D[] hits = Physics2D.RaycastAll(laserOrigin.position, currentDirection, laserLength, targetLayers);
            foreach (RaycastHit2D hit in hits)
            {
                Player_Health playerHealth = hit.collider.GetComponent<Player_Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(laserDamage * Time.deltaTime);
                }
            }
        }
    }

    private void UpdateLaserPosition(Vector3 direction)
    {
        if (lineRenderer != null)
        {
            Vector3 startPos = laserOrigin.position;
            Vector3 endPos = startPos + direction * 10f;

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            // Update particle systems
            foreach (var ps in particleSystems)
            {
                ParticleSystem.ShapeModule shape = ps.shape;

                // Calculate angle in degrees
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                shape.rotation = new Vector3(0, 0, angle);

                // Get the midpoint for particle position
                Vector3 particlePos = (startPos + endPos) * 0.5f;
                ps.transform.position = particlePos;

                // Check if this is the LaserCore particle system
                if (ps.gameObject.name.Contains("LaserCore"))
                {
                    ParticleSystem.MainModule main = ps.main;
                    var localScale = ps.transform.localScale;

                    // If shooting more horizontally
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                    {
                        // Adjust scale for horizontal laser
                        localScale.x = 1f;  // Maintain width
                        localScale.y = 0.5f; // Reduce height for horizontal beam
                    }
                    // If shooting more vertically
                    else
                    {
                        // Adjust scale for vertical laser
                        localScale.x = 0.5f; // Reduce width for vertical beam
                        localScale.y = 1f;  // Maintain height
                    }

                    ps.transform.localScale = localScale;
                }

                // If you want the particles to stretch in the direction of the laser
                ParticleSystem.MainModule mainModule = ps.main;
                if (ps.gameObject.name.Contains("LaserCore"))
                {
                    // Adjust velocity based on direction
                    var velocityOverLifetime = ps.velocityOverLifetime;
                    velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(direction.x * 5f); // Adjust multiplier as needed
                    velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(direction.y * 5f);
                }
            }
        }
    }

    private void StopLaserAttack()
    {
        isAttacking = false;

        // Stop and destroy the laser with particles
        if (currentLaser != null)
        {
            float maxParticleDuration = 0f;
            foreach (var ps in particleSystems)
            {
                ps.Stop();
                maxParticleDuration = Mathf.Max(maxParticleDuration, ps.main.duration);
            }
            Destroy(currentLaser, maxParticleDuration);
        }

        Invoke(nameof(ResetAttack), cooldownTime);
    }

    private void ResetAttack()
    {
        // Ready for next attack
    }

    private void OnDestroy()
    {
        if (currentLaser != null)
        {
            Destroy(currentLaser);
        }
    }
}