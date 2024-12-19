using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
    private AudioSource audioSource;
    private static BackgroundMusicManager instance;
    public string level1SceneName = "Game1";
    public string level2SceneName = "intro2";
    public AudioClip levelMusic; // Add this variable to reference the music file in Inspector

    void Awake()
    {
        // Implement singleton pattern to prevent multiple music sources
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Get or add AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource settings
            audioSource.loop = true;
            audioSource.playOnAwake = false;

            // Assign the music clip
            if (levelMusic != null)
            {
                audioSource.clip = levelMusic;
            }
            else
            {
                Debug.LogError("Please assign the music clip in the Inspector!");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start playing if we're in level 1
        if (SceneManager.GetActiveScene().name == level1SceneName)
        {
            PlayMusic();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == level1SceneName)
        {
            PlayMusic();
        }
        else if (scene.name == level2SceneName)
        {
            StopMusic();
        }
    }

    void PlayMusic()
    {
        if (audioSource && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void StopMusic()
    {
        if (audioSource && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}