using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private PhysicsManager physicsManager;
    private int bestScore;
    
    public void Update()
    {
        int scoreVal = Mathf.Max((int)physicsManager.corruptScore, 0);
        scoreText.text = (scoreVal).ToString();
        
        bestScore = Mathf.Max(scoreVal, bestScore);
        bestScoreText.text = bestScore.ToString();
    }

}
