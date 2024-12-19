using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishPoint : MonoBehaviour
{
    [SerializeField] private bool goNextLevel = true;
    [SerializeField] private string levelName;
    [SerializeField] private Enemy enemyToDefeat; // Reference to the enemy that must be defeated

    private void Start()
    {
        // Hide the finish point initially
        gameObject.SetActive(false);

        // Subscribe to enemy's death
        if (enemyToDefeat != null)
        {
            enemyToDefeat.OnEnemyDeath += EnableFinishPoint;
        }
    }

    private void OnDestroy()
    {
        // Clean up subscription when object is destroyed
        if (enemyToDefeat != null)
        {
            enemyToDefeat.OnEnemyDeath -= EnableFinishPoint;
        }
    }

    private void EnableFinishPoint()
    {
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (goNextLevel)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
            else if (!string.IsNullOrEmpty(levelName))
            {
                SceneManager.LoadScene(levelName);
            }
        }
    }
}