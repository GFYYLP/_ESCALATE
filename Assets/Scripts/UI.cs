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
    [SerializeField] private Button quitButton;
    [SerializeField] private Button pauseButton;

    [SerializeField] private TMP_Text playButtonLabel;

    public static CounterUI Instance;

    private GameObject activeScreen;
    private GameObject previousScreen;
    private bool paused = false;
    private bool gameStarted = false;
    private bool gameOver = false;

    public bool IsPlaying => gameStarted && !paused && !gameOver;

    public void PauseFromButton()
    {
        if (!IsPlaying) return;
        SetPaused(true);
        ShowScreen(pauseScreen);
        ShowNav();
    }
    private static int bestScore = 0;
    private static bool restartRequested = false;
    
    private struct ScreenButtonConfig
    {
        public bool showPlay;
        public bool showRestart;
        public bool showReturn;
        public bool showHome;
        public bool showSetting;
        public bool showQuit;
        public string playLabel; //null = use default "Play"
    }

    private Dictionary<GameObject, ScreenButtonConfig> screenConfigs;

    private void Awake()
    {
        Instance = this;

        screenConfigs = new Dictionary<GameObject, ScreenButtonConfig>
        {
            [homeScreen] = new ScreenButtonConfig
            {
                showPlay    = true,  playLabel = "Play",
                showRestart = false,
                showReturn  = false,
                showHome    = false,
                showSetting = true,
                showQuit    = true,
            },
            [pauseScreen] = new ScreenButtonConfig
            {
                showPlay    = true,  playLabel = "Resume",
                showRestart = true,
                showReturn  = false,
                showHome    = true,
                showSetting = true,
                showQuit    = false,
            },
            [settingScreen] = new ScreenButtonConfig
            {
                showPlay    = false,
                showRestart = false,
                showReturn  = true,
                showHome    = false,
                showSetting = false, 
                showQuit    = false,
            },
            [overScreen] = new ScreenButtonConfig
            {
                showPlay    = false,
                showRestart = true,
                showReturn  = false,
                showHome    = true,
                showSetting = false,
                showQuit    = false,
            },
            [winScreen] = new ScreenButtonConfig
            {
                showPlay    = false,
                showRestart = true,
                showReturn  = false,
                showHome    = true,
                showSetting = false,
                showQuit    = false,
            },
        };

        playButton.onClick.AddListener(OnPlayClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
        returnButton.onClick.AddListener(OnReturnClicked);
        homeButton.onClick.AddListener(OnHomeClicked);
        settingButton.onClick.AddListener(OnSettingClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        pauseButton.onClick.AddListener(PauseFromButton);
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
        gameOver = false;
        gameStarted = false;
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

    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
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
        //SetPaused(true);
        ShowScreen(winScreen);
        ShowNav();
    }

    
    public void Update()
    {
        int scoreVal = Mathf.Max((int)player.transform.position.y, 0);
        bestScore = Mathf.Max(scoreVal, bestScore);

        bestScoreText.text = scoreVal + " : " + ((int)bestScore);

        if (Input.GetKeyDown(KeyCode.Escape) && gameStarted && !gameOver)
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

        if (gameStarted && !gameOver && physicsManager.stabilityRatio >= 1.0f)
            ShowOverScreen();

        if (gameStarted && !gameOver && physicsManager.corruptScore >= PhysicsManager.Instance.winScore)
        {
            ShowWinScreen();
        }
    }



    private void ShowScreen(GameObject screen)
    {
        previousScreen = activeScreen;
        HideActiveScreen();
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

    ///reads the config for the given screen and shows/hides each nav button accordingly.
    private void ApplyButtonConfig(GameObject screen)
    {
        if (!screenConfigs.TryGetValue(screen, out ScreenButtonConfig cfg))
        {
            //show everything as a safe fallback
            SetButtonVisible(playButton,    true);
            SetButtonVisible(restartButton, true);
            SetButtonVisible(returnButton,  true);
            SetButtonVisible(homeButton,    true);
            SetButtonVisible(settingButton, true);
            SetButtonVisible(quitButton,    true);
            return;
        }

        SetButtonVisible(playButton,    cfg.showPlay);
        SetButtonVisible(restartButton, cfg.showRestart);
        SetButtonVisible(returnButton,  cfg.showReturn);
        SetButtonVisible(homeButton,    cfg.showHome);
        SetButtonVisible(settingButton, cfg.showSetting);
        SetButtonVisible(quitButton,    cfg.showQuit);

        if (cfg.showPlay && playButtonLabel != null)
            playButtonLabel.text = cfg.playLabel ?? "Play";
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void ShowNav()
    {
        navScreen.SetActive(true);
        SetButtonVisible(pauseButton, false);
    }

    private void HideNav()
    {
        navScreen.SetActive(false);
        SetButtonVisible(pauseButton, gameStarted && !gameOver);
    }

    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
    }
}