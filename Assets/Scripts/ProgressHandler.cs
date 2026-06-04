using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProgressHandler : MonoBehaviour
{
    [SerializeField] private TMP_Text percentText;
    private ProgressBar progressBar;
    
    private PhysicsManager physicsManager;
    private Player player;
    
    void Awake()
    {
        progressBar = GetComponent<ProgressBar>();
        
        physicsManager = FindObjectOfType<PhysicsManager>();
        player = FindObjectOfType<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        float scoreRatio = player.Speed * 0.8f / physicsManager.highCollideVal;
        int percent = progressBar.UpdateBar(scoreRatio);
        
        percentText.text = percent.ToString() + "% disrupted";
    }
}
