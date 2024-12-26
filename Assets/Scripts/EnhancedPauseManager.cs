using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class EnhancedPauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private Image backgroundBlur;
    [SerializeField] private Button[] menuButtons;  // Reference to menu buttons for animation

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float backgroundFadeAlpha = 0.8f;
    [SerializeField] private float buttonPopupDelay = 0.1f;  // Delay between each button animation
    [SerializeField] private float buttonHoverScale = 1.1f;  // How much buttons scale on hover
    [SerializeField] private float buttonHoverDuration = 0.2f;  // How fast buttons scale on hover

    [Header("Visual Effects")]
    [SerializeField] private Color menuTitleColor = new Color(1f, 0.8f, 0.2f);  // Golden color for title
    [SerializeField] private TMP_Text pauseMenuTitle;  // Reference to "PAUSED" text

    [Header("Layer References")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask enemyLayer;

    public static EnhancedPauseManager Instance { get; private set; }
    public bool IsPaused { get; private set; }

    private void Awake()
    {
        DOTween.SetTweensCapacity(500, 50);
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

        if (Instance == null)
        {
            Instance = this;
            SetupUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupUI()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);

            if (pauseMenuCanvasGroup == null)
                pauseMenuCanvasGroup = pauseMenuUI.GetComponent<CanvasGroup>();
        }

        // Setup button animations
        foreach (var button in menuButtons)
        {
            SetupButtonAnimation(button);
        }

        // Setup title if assigned
        if (pauseMenuTitle != null)
        {
            pauseMenuTitle.color = menuTitleColor;
        }
    }

    private void SetupButtonAnimation(Button button)
    {
        // Add hover animations
        button.transform.localScale = Vector3.one;

        button.onClick.AddListener(() => {
            // Click animation
            button.transform.DOScale(0.9f, 0.1f).SetUpdate(true)
                .OnComplete(() => {
                    button.transform.DOScale(1f, 0.1f).SetUpdate(true);
                });
        });

        // Get or add an EventTrigger component
        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // Hover enter
        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            button.transform.DOScale(buttonHoverScale, buttonHoverDuration).SetUpdate(true);
        });
        eventTrigger.triggers.Add(enterEntry);

        // Hover exit
        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            button.transform.DOScale(1f, buttonHoverDuration).SetUpdate(true);
        });
        eventTrigger.triggers.Add(exitEntry);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        DOTween.Kill(pauseMenuUI.transform);

        // Animate buttons out first
        for (int i = 0; i < menuButtons.Length; i++)
        {
            menuButtons[i].transform.DOScale(0f, animationDuration * 0.5f)
                .SetDelay(i * buttonPopupDelay * 0.5f)
                .SetUpdate(true);
        }

        // Then fade out the menu
        DOTween.Sequence()
            .SetUpdate(true)
            .AppendInterval(menuButtons.Length * buttonPopupDelay * 0.5f)
            .OnComplete(() => {
                if (backgroundBlur != null)
                    backgroundBlur.DOFade(0f, animationDuration).SetUpdate(true);

                menuPanel.DOScale(0.8f, animationDuration).SetUpdate(true);
                pauseMenuCanvasGroup.DOFade(0f, animationDuration).SetUpdate(true);
            })
            .AppendInterval(animationDuration)
            .OnComplete(() => {
                pauseMenuUI.SetActive(false);
                Time.timeScale = 1f;
                IsPaused = false;
                EnableGameplayElements(true);
            });
    }

    void Pause()
    {
        DOTween.Kill(pauseMenuUI.transform);
        pauseMenuUI.SetActive(true);

        // Reset initial states
        menuPanel.localScale = Vector3.zero;
        pauseMenuCanvasGroup.alpha = 0f;
        foreach (var button in menuButtons)
        {
            button.transform.localScale = Vector3.zero;
        }

        if (backgroundBlur != null)
            backgroundBlur.color = new Color(0, 0, 0, 0);

        // Sequence for opening the menu
        DOTween.Sequence()
            .SetUpdate(true)
            .OnStart(() => {
                if (backgroundBlur != null)
                    backgroundBlur.DOFade(backgroundFadeAlpha, animationDuration).SetUpdate(true);

                menuPanel.DOScale(1f, animationDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);

                pauseMenuCanvasGroup.DOFade(1f, animationDuration).SetUpdate(true);

                // Animate buttons in sequence
                for (int i = 0; i < menuButtons.Length; i++)
                {
                    menuButtons[i].transform
                        .DOScale(1f, animationDuration)
                        .SetDelay(i * buttonPopupDelay)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                }
            })
            .OnComplete(() => {
                Time.timeScale = 0f;
                IsPaused = true;
                EnableGameplayElements(false);
            });
    }

    private void EnableGameplayElements(bool enable)
    {
        GameObject[] players = FindObjectsInLayer(playerLayer);
        foreach (var player in players)
        {
            var playerComponents = player.GetComponents<MonoBehaviour>();
            foreach (var component in playerComponents)
            {
                if (component != this)
                    component.enabled = enable;
            }
        }

        GameObject[] enemies = FindObjectsInLayer(enemyLayer);
        foreach (var enemy in enemies)
        {
            var enemyComponents = enemy.GetComponents<MonoBehaviour>();
            foreach (var component in enemyComponents)
            {
                component.enabled = enable;
            }
        }
    }

    private GameObject[] FindObjectsInLayer(LayerMask layerMask)
    {
        var goArray = FindObjectsOfType<GameObject>();
        var goList = new System.Collections.Generic.List<GameObject>();
        foreach (var go in goArray)
        {
            if ((layerMask.value & (1 << go.layer)) > 0)
            {
                goList.Add(go);
            }
        }
        return goList.ToArray();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}