using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    public float waitTime = 6f; // Waktu tunggu sebelum pindah ke MainMenu

    void Start()
    {
        StartCoroutine(WaitAndLoadMainMenu());
    }  

    IEnumerator WaitAndLoadMainMenu()
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("MainMenu"); // Pastikan nama scene "MainMenu" sesuai dengan yang ada di Build Settings
    }
}
