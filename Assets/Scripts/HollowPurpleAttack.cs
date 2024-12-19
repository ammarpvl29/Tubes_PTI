// Part 1: Core Particle System Setup
using UnityEngine;

public class HollowPurpleAttack : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem chargeEffect;      // Purple energy gathering
    public ParticleSystem mainBeamEffect;    // Main blast beam
    public ParticleSystem sphereEffect;      // Sphere at point of convergence

    [Header("Properties")]
    public float chargeTime = 2f;            // Time to charge the attack
    public float beamDuration = 1.5f;        // How long the beam lasts
    public float damageAmount = 50f;         // Damage dealt by the beam
    public float beamRadius = 2f;            // Radius of the damage area

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip chargeSound;
    public AudioClip fireSound;

    private bool isCharging = false;
    private float chargeTimer = 0f;

    void Start()
    {
        // Initially disable all particle systems
        chargeEffect.Stop();
        mainBeamEffect.Stop();
        sphereEffect.Stop();
    }

    public void StartChargingAttack()
    {
        if (!isCharging)
        {
            isCharging = true;
            chargeTimer = 0f;
            chargeEffect.Play();
            if (audioSource && chargeSound)
            {
                audioSource.PlayOneShot(chargeSound);
            }
        }
    }

    void Update()
    {
        if (isCharging)
        {
            chargeTimer += Time.deltaTime;

            if (chargeTimer >= chargeTime)
            {
                FireHollowPurple();
                isCharging = false;
            }
        }
    }

    void FireHollowPurple()
    {
        // Play main beam effect
        mainBeamEffect.Play();
        sphereEffect.Play();

        if (audioSource && fireSound)
        {
            audioSource.PlayOneShot(fireSound);
        }

        // Check for hits
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            transform.position,
            beamRadius,
            transform.right,
            20f
        );

        foreach (RaycastHit2D hit in hits)
        {
            // Check if we hit something with a health component
            if (hit.collider.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage((int)damageAmount);
            }
        }

        // Stop effects after duration
        Invoke("StopEffects", beamDuration);
    }

    void StopEffects()
    {
        chargeEffect.Stop();
        mainBeamEffect.Stop();
        sphereEffect.Stop();
    }
}