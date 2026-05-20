using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private GameObject progressBar;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private PhysicsManager physicsManager;
    private Player player;
    
    void Awake()
    {
        physicsManager = FindObjectOfType<PhysicsManager>();
        player = FindObjectOfType<Player>();
    }
    
    private void Start()
    {
        originalScale = progressBar.transform.localScale;
        originalPosition = progressBar.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        int percent = Mathf.RoundToInt((player.Speed * 0.8f/ physicsManager.highCollideVal) * 100f);
        percentText.text = percent.ToString() + "% disrupted";
        
        float scaledWidth = originalScale.x * percent;

        progressBar.transform.localScale =
            new Vector3(
                scaledWidth,
                originalScale.y,
                originalScale.z
            );

        progressBar.transform.localPosition =
            new Vector3(
                originalPosition.x
                - (originalScale.x - scaledWidth) * 0.5f,
                originalPosition.y,
                originalPosition.z
            );
    }
}
