using System;
using System.Collections;
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

    private GameObject activeScreen;
    
    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayClicked); //hide all
        restartButton.onClick.AddListener(OnRestartClicked);
        returnButton.onClick.AddListener(OnReturnClicked);
        
        homeButton.onClick.AddListener(OnHomeClicked);
        settingButton.onClick.AddListener(OnSettingClicked);
    }

    void Start()
    {
        OnHomeClicked();
    }
    
    private void OnPlayClicked()
    {
        OnReturnClicked();  //resume previous screen
        if (activeScreen == homeScreen) OnRestartClicked();
    }
    
    private void OnHomeClicked()
    {
        homeScreen.SetActive(true);
        activeScreen = homeScreen;
        ShowNav();
    }
    
    private void OnRestartClicked()
    {
        //reset scene states
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
        OnReturnClicked();
    }

    private void ShowNav()
    {
        navScreen.SetActive(true);
    }

    private void OnSettingClicked()
    {
        settingScreen.SetActive(true);
        activeScreen = settingScreen;
        ShowNav();
    }
    
    private void OnReturnClicked()
    {
        activeScreen.SetActive(false);
    }

    public void ShowOverScreen()
    {
        overScreen.SetActive(true);
        activeScreen = overScreen;
        ShowNav();
    }
    
    public void ShowWinScreen()
    {
        winScreen.SetActive(true);
        activeScreen = winScreen;
        ShowNav();
    }
    
    private int bestScore;
    
    public void Update()
    {
        //int scoreVal = Mathf.Max((int)physicsManager.corruptScore, 0);
        int scoreVal = Mathf.Max((int)player.transform.position.y, 0);
        
        bestScore = Mathf.Max(scoreVal, bestScore);
        bestScoreText.text = scoreVal + ":" + physicsManager.corruptScore.ToString();

        scrollbar.fillAmount = physicsManager.stabilityRatio;
        if (physicsManager.stabilityRatio >= 1.0f)
        {
            ShowOverScreen();
        }
        
        // if (physicsManager.corruptScore >= 200.0f)
        // {
        //     ShowWinScreen();
        // }
    }

}
