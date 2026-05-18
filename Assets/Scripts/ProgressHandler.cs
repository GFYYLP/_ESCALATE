using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private GameObject progressBar;
    private PhysicsManager physicsManager;
    
    void Awake()
    {
        physicsManager = FindObjectOfType<PhysicsManager>();
    }

    // Update is called once per frame
    void Update()
    {
        int percent = Mathf.RoundToInt((physicsManager.highestImpactSpeed / physicsManager.highCollideVal) * 100f);
        percentText.text = percent.ToString() + "% complete";
        
        progressBar.transform.localScale =
            new Vector3(percent, 1f, 1f);

        progressBar.transform.localPosition =
            new Vector3(-(transform.localScale.x - transform.localScale.x * percent) * 0.5f, 0f, 0f);
    }
}
