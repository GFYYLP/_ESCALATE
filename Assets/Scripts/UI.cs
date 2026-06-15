using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private Image scrollbar;
    [SerializeField] private PhysicsManager physicsManager;
    [SerializeField] private Player player;

    [SerializeField] private GameObject homeScreen;
    [SerializeField] private GameObject settingScreen;
    [SerializeField] private GameObject overScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject navScreen;

    [SerializeField] private Button homeButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button returnButton;

    [SerializeField] private TMP_Text playButtonLabel;

    private GameObject activeScreen;
    private GameObject previousScreen;
    private bool paused = false;
    private bool gameStarted = false;
    private bool gameOver = false;
    private int bestScore = 0;
    private static bool restartRequested = false;
    
    private struct ScreenButtonConfig
    {
        public bool showPlay;
        public bool showRestart;
        public bool showReturn;
        public bool showHome;
        public bool showSetting;
        public string playLabel; // null = use default "Play"
    }

    private Dictionary<GameObject, ScreenButtonConfig> screenConfigs;

    private void Awake()
    {
        screenConfigs = new Dictionary<GameObject, ScreenButtonConfig>
        {
            [homeScreen] = new ScreenButtonConfig
            {
                showPlay    = true,  playLabel = "Play",
                showRestart = false,
                showReturn  = false,
                showHome    = false, // already here
                showSetting = true,
            },
            [pauseScreen] = new ScreenButtonConfig
            {
                showPlay    = true,  playLabel = "Resume",
                showRestart = true,
                showReturn  = false,
                showHome    = true,
                showSetting = true,
            },
            [settingScreen] = new ScreenButtonConfig
            {
                showPlay    = false,
                showRestart = false,
                showReturn  = true,
                showHome    = true,
                showSetting = false, // already here
            },
            [overScreen] = new ScreenButtonConfig
            {
                showPlay    = false,
                showRestart = true,
                showReturn  = false,
                showHome    = true,
                showSetting = false,
            },
            [winScreen] = new ScreenButtonConfig
            {
                showPlay    = false,
                showRestart = true,
                showReturn  = false,
                showHome    = true,
                showSetting = false,
            },
        };

        playButton.onClick.AddListener(OnPlayClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
        returnButton.onClick.AddListener(OnReturnClicked);
        homeButton.onClick.AddListener(OnHomeClicked);
        settingButton.onClick.AddListener(OnSettingClicked);
    }

    private void Start()
    {
        if (restartRequested)
        {
            restartRequested = false;
            // Skip menu, begin immediately
            gameStarted = true;
            HideNav();
            SetPaused(false);
        }
        else
        {
            OnHomeClicked();
        }
    }
    

    private void OnPlayClicked()
    {
        if (!gameStarted || activeScreen == homeScreen)
        {
            restartRequested = true; 
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else if (activeScreen == pauseScreen)
        {
            HideActiveScreen();
            HideNav();
            SetPaused(false);
        }
    }

    private void OnHomeClicked()
    {
        SetPaused(true);
        ShowScreen(homeScreen);
        ShowNav();
    }

    private void OnRestartClicked()
    {
        restartRequested = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnReturnClicked()
    {
        if (gameOver) return;

        if (previousScreen != null)
        {
            ShowScreen(previousScreen);
        }
        else if (gameStarted)
        {
            HideActiveScreen();
            HideNav();
            SetPaused(false);
        }
        else
        {
            OnHomeClicked();
        }
    }

    private void OnSettingClicked()
    {
        SetPaused(true);
        ShowScreen(settingScreen);
        ShowNav();
    }



    public void ShowOverScreen()
    {
        if (gameOver) return;
        gameOver = true;
        SetPaused(true);
        ShowScreen(overScreen);
        ShowNav();
    }

    public void ShowWinScreen()
    {
        if (gameOver) return;
        gameOver = true;
        SetPaused(true);
        ShowScreen(winScreen);
        ShowNav();
    }



    public void Update()
    {
        int scoreVal = Mathf.Max((int)player.transform.position.y, 0);
        bestScore = Mathf.Max(scoreVal, bestScore);
        bestScoreText.text = scoreVal + " : " + physicsManager.corruptScore.ToString();

        if (Input.GetKeyDown(KeyCode.V) && gameStarted && !gameOver)
        {
            if (paused)
                OnReturnClicked();
            else
            {
                SetPaused(true);
                ShowScreen(pauseScreen);
                ShowNav();
            }
        }

        scrollbar.fillAmount = physicsManager.stabilityRatio;

        if (!gameOver && physicsManager.stabilityRatio >= 1.0f)
            ShowOverScreen();

        // if (!gameOver && physicsManager.corruptScore >= 200.0f)
        //     ShowWinScreen();
    }



    private void ShowScreen(GameObject screen)
    {
        HideActiveScreen();
        previousScreen = activeScreen; 
        screen.SetActive(true);
        activeScreen = screen;
        ApplyButtonConfig(screen);
    }

    private void HideActiveScreen()
    {
        if (activeScreen != null)
        {
            activeScreen.SetActive(false);
            activeScreen = null;
        }
    }

    /// Reads the config for the given screen and shows/hides each nav button accordingly.
    private void ApplyButtonConfig(GameObject screen)
    {
        if (!screenConfigs.TryGetValue(screen, out ScreenButtonConfig cfg))
        {
            // Unknown screen: show everything as a safe fallback
            SetButtonVisible(playButton,    true);
            SetButtonVisible(restartButton, true);
            SetButtonVisible(returnButton,  true);
            SetButtonVisible(homeButton,    true);
            SetButtonVisible(settingButton, true);
            return;
        }

        SetButtonVisible(playButton,    cfg.showPlay);
        SetButtonVisible(restartButton, cfg.showRestart);
        SetButtonVisible(returnButton,  cfg.showReturn);
        SetButtonVisible(homeButton,    cfg.showHome);
        SetButtonVisible(settingButton, cfg.showSetting);

        if (cfg.showPlay && playButtonLabel != null)
            playButtonLabel.text = cfg.playLabel ?? "Play";
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void ShowNav() => navScreen.SetActive(true);
    private void HideNav() => navScreen.SetActive(false);

    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
    }
}