using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Tambahkan ini

public class Ending : MonoBehaviour
{
    public float waitTime = 35f; // Waktu tunggu sebelum pindah ke MainMenu

    void Start()
    {
        StartCoroutine(WaitAndLoadMainMenu());
    }

    IEnumerator WaitAndLoadMainMenu()
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("MainMenu");
    }
}
