using UnityEngine;

public class GarbageItem : MonoBehaviour
{
    [Header("Power-up Settings")]
    public float moveSpeed = 5f;
    public int damageBoost = 10;

    private bool isBeingCollected = false;
    private Transform target;
    private Boss_Weapon bossWeapon;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBeingCollected) return;

        if (other.CompareTag("Player"))
        {
            // Player collected it - prevent slime from getting stronger
            StartCollection(other.transform);
        }
        else if (other.CompareTag("Enemy"))
        {
            bossWeapon = other.GetComponent<Boss_Weapon>();
            if (bossWeapon != null)
            {
                // Modify the boss weapon's damage
                bossWeapon.attackSettings.normalDamage += damageBoost;
                bossWeapon.attackSettings.enragedDamage += damageBoost * 2;

                // Visual feedback
                StartCoroutine(FlashEffect(other.GetComponent<SpriteRenderer>()));
            }
            StartCollection(other.transform);
        }
    }

    private System.Collections.IEnumerator FlashEffect(SpriteRenderer renderer)
    {
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            renderer.color = originalColor;
        }
    }

    private void StartCollection(Transform collector)
    {
        isBeingCollected = true;
        target = collector;
    }

    private void Update()
    {
        if (isBeingCollected && target != null)
        {
            // Move towards collector
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            // Check if reached the collector
            if (Vector2.Distance(transform.position, target.position) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}