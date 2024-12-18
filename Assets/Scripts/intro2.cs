using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Untuk Scene Management

public class Intro2 : MonoBehaviour
{
    public float waitTime = 22f; // Waktu tunggu sebelum pindah ke MainMenu

    void Start()
    {
        StartCoroutine(WaitAndLoadMainMenu());
    }

    IEnumerator WaitAndLoadMainMenu()
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("Game2"); // Pastikan nama scene sesuai dengan Build Settings
    }
}
