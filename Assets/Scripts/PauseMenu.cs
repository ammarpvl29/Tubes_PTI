using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  // Fixed typo in namespace name

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;   // Changed variable name to be more descriptive
    public static bool isPaused;     // Changed variable name to match its usage

    void Start()
    {
        pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))  // Removed semicolon that was causing the if statement to be empty
        {
            if (isPaused)  // Changed Paused to isPaused to match the variable name
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

public void ResumeGame()
{
    Debug.Log("Resume clicked!");
    pauseMenuUI.SetActive(false);
    Time.timeScale = 1f;
    isPaused = false;
}

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;        // Reset timeScale before changing scene
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}