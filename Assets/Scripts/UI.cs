using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text bestHeightText;
    [SerializeField] private Player player;
    private int bestHeight;
    
    public void Update()
    {
        int heightVal = Mathf.Max((int)player.transform.position.y, 0);
        heightText.text = (heightVal).ToString();
        
        bestHeight = Mathf.Max(heightVal, bestHeight);
        bestHeightText.text = bestHeight.ToString();
    }

}
