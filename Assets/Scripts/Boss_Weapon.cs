using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_Weapon : MonoBehaviour
{
    public int attackDamage = 20;
    public int enragedAttackDamage = 40;
    public Vector3 attackOffset;
    public float attackRange = 1f;
    public LayerMask attackMask;

    // Audio variables
    private AudioSource audioSource;
    public AudioClip swordSlashSound;
    public AudioClip monsterRoarSound;

    // Roar timing variables
    public float minRoarInterval = 3f;    // Minimum time between roars
    public float maxRoarInterval = 7f;   // Maximum time between roars
    private float nextRoarTime;

    void Start()
    {
        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();
        
        // If there's no AudioSource, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Set initial roar time
        SetNextRoarTime();
    }

    void Update()
    {
        // Check if it's time to roar
        if (Time.time >= nextRoarTime)
        {
            PlayRoarSound();
            SetNextRoarTime();
        }
    }

    private void SetNextRoarTime()
    {
        // Set the next roar time to a random value between minRoarInterval and maxRoarInterval
        nextRoarTime = Time.time + Random.Range(minRoarInterval, maxRoarInterval);
    }

    private void PlayRoarSound()
    {
        if (audioSource != null && monsterRoarSound != null)
        {
            audioSource.PlayOneShot(monsterRoarSound);
        }
    }

    public void Attack()
    {
        // Play the slash sound
        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.PlayOneShot(swordSlashSound);
        }

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
        if (colInfo != null)
        {
            colInfo.GetComponent<Player_Health>().TakeDamage(attackDamage);
        }
    }

    public void EnragedAttack()
    {
        // Play the slash sound (you might want a different sound for enraged attacks)
        if (audioSource != null && swordSlashSound != null)
        {
            audioSource.PlayOneShot(swordSlashSound);
        }

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
        if (colInfo != null)
        {
            colInfo.GetComponent<Player_Health>().TakeDamage(enragedAttackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;
        Gizmos.DrawWireSphere(pos, attackRange);
    }
}