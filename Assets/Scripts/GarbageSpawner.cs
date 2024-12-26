using UnityEngine;

public class GarbageSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject[] garbagePrefabs;
    public int initialGarbageCount = 10;
    public float spawnWidth = 20f;
    public float heightAboveGround = 0.5f;
    public float minDistanceBetweenItems = 1f;
    public float respawnDelay = 5f;
    public int maxGarbageOnScreen = 15;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float maxGroundCheckDistance = 10f;

    private float nextSpawnTime;

    private void Start()
    {
        SpawnInitialGarbage();
    }

    private void SpawnInitialGarbage()
    {
        for (int i = 0; i < initialGarbageCount; i++)
        {
            SpawnGarbageItem();
        }
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime &&
            GameObject.FindGameObjectsWithTag("Garbage").Length < maxGarbageOnScreen)
        {
            SpawnGarbageItem();
            nextSpawnTime = Time.time + respawnDelay;
        }
    }

    private void SpawnGarbageItem()
    {
        float randomX = Random.Range(-spawnWidth / 2, spawnWidth / 2);
        Vector2 rayStart = new Vector2(randomX, transform.position.y + maxGroundCheckDistance);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, maxGroundCheckDistance * 2, groundLayer);

        if (hit.collider != null)
        {
            Vector2 spawnPosition = hit.point + Vector2.up * heightAboveGround;

            // Check only for other garbage items
            Collider2D[] nearbyGarbage = Physics2D.OverlapCircleAll(spawnPosition, minDistanceBetweenItems);
            bool positionClear = true;

            foreach (Collider2D col in nearbyGarbage)
            {
                if (col.CompareTag("Garbage"))
                {
                    positionClear = false;
                    break;
                }
            }

            if (positionClear)
            {
                GameObject selectedPrefab = garbagePrefabs[Random.Range(0, garbagePrefabs.Length)];
                GameObject garbage = Instantiate(
                    selectedPrefab,
                    spawnPosition,
                    Quaternion.Euler(0, 0, Random.Range(0f, 360f))
                );
                garbage.tag = "Garbage";
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            transform.position + new Vector3(-spawnWidth / 2, 0, 0),
            transform.position + new Vector3(spawnWidth / 2, 0, 0)
        );
    }
}