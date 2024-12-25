using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;
    public Image damageFlash;  // Reference to a white image overlay

    [Header("Visual Effects")]
    public float smoothSpeed = 8f;
    public float flashDuration = 0.2f;
    public float shakeAmount = 5f;
    public float shakeDecrease = 5f;

    private float targetHealth;
    private float currentShake;
    private Vector3 originalPosition;
    private float flashTime;

    void Start()
    {
        originalPosition = transform.position;
        targetHealth = slider.maxValue;
        if (damageFlash != null)
        {
            damageFlash.color = new Color(1, 1, 1, 0);
        }
    }

    void Update()
    {
        // Smooth health bar movement
        if (slider.value != targetHealth)
        {
            slider.value = Mathf.Lerp(slider.value, targetHealth, Time.deltaTime * smoothSpeed);
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }

        // Handle damage flash
        if (flashTime > 0)
        {
            flashTime -= Time.deltaTime;
            if (damageFlash != null)
            {
                damageFlash.color = new Color(1, 1, 1, flashTime / flashDuration);
            }
        }

        // Handle shake effect
        if (currentShake > 0)
        {
            currentShake -= Time.deltaTime * shakeDecrease;
            Vector2 shakeOffset = Random.insideUnitCircle * currentShake;
            transform.position = originalPosition + new Vector3(shakeOffset.x, shakeOffset.y, 0);
        }
        else
        {
            transform.position = originalPosition;
        }
    }

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        targetHealth = health;
        slider.value = health;
        fill.color = gradient.Evaluate(1f);
    }

    public void SetHealth(int health)
    {
        // Calculate damage taken
        float damageTaken = targetHealth - health;
        targetHealth = health;

        // Trigger effects based on damage
        if (damageTaken > 0)
        {
            // Flash effect
            flashTime = flashDuration;

            // Shake effect - bigger damage = bigger shake
            float damagePercent = damageTaken / slider.maxValue;
            currentShake = shakeAmount * damagePercent;
        }
    }

    // Call this when boss enters different phases
    public void TriggerPhaseTransition()
    {
        currentShake = shakeAmount * 2; // Extra strong shake
        flashTime = flashDuration * 2;  // Longer flash
    }
}