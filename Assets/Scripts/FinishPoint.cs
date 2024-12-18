using UnityEngine;
using UnityEngine.SceneManagement; // Required for Scene Management

public class FinishPoint : MonoBehaviour
{
    [SerializeField] private bool goNextLevel = true; // Untuk menentukan apakah harus lanjut ke level berikutnya
    [SerializeField] private string levelName; // Nama level jika ingin memuat scene tertentu

    // This method is called when a collider enters the trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object that collided has the tag "Player"
        if (collision.CompareTag("Player"))
        {
            if (goNextLevel)
            {
                // Load the next scene in the build order
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
            else if (!string.IsNullOrEmpty(levelName))
            {
                // Load a specific scene by name
                SceneManager.LoadScene(levelName);
            }
        }
    }
}
