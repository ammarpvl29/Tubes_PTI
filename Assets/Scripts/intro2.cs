using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Tambahkan ini

public class intro2 : MonoBehaviour
{
    public float waitTime = 30f; // Waktu tunggu sebelum pindah ke MainMenu

    void Start()
    {
        StartCoroutine(WaitAndLoadMainMenu());
    }  

    IEnumerator WaitAndLoadMainMenu()
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("Game3"); // Dari intro ke 2 lanjut level 3
    }
}
