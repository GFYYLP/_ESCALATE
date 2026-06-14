using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private PhysicsManager physicsManager;
    [SerializeField] private Player player;
    
    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject gameOver;

    [SerializeField] Image scrollbar;
    
    public void ShowMainMenu()
    {
        HideAll();
        mainMenu.SetActive(true);
    }

    public void ShowGameOver()
    {
        HideAll();
        gameOver.SetActive(true);
    }

    private void HideAll()
    {
        mainMenu.SetActive(false);
        // hud.SetActive(false);
        gameOver.SetActive(false);
    }
    
    private int bestScore;
    
    public void Update()
    {
        //int scoreVal = Mathf.Max((int)physicsManager.corruptScore, 0);
        int scoreVal = Mathf.Max((int)player.transform.position.y, 0);
        
        bestScore = Mathf.Max(scoreVal, bestScore);
        bestScoreText.text = scoreVal + ":" + physicsManager.corruptScore.ToString();

        scrollbar.fillAmount = physicsManager.stabilityRatio;
        // if (physicsManager.stabilityRatio >= 1.0f)
        // {
        //     ShowGameOver();
        // }
    }

}
