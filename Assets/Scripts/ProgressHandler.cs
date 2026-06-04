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
        int percent = Mathf.RoundToInt((player.Speed * 0.8f/ physicsManager.highCollideVal) * 100f);
        percentText.text = percent.ToString() + "% disrupted";
        
        progressBar.UpdateBar(percent);
    }
}
