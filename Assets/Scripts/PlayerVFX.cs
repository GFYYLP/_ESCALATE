using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem dashVFX;
    [SerializeField] private Player player;
    private TrailRenderer trail;
    private ProgressBar reflectBar;
    private ProgressBar warpBar;
    [SerializeField] private ProgressBar reflectTask;
    [SerializeField] private ProgressBar warpTask;
    [SerializeField] private ProgressBar dashTask;
    
    [SerializeField] private AfterImage afterImagePrefab;
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float lifetime = 0.3f;
    [SerializeField] private float speedThreshold = 10f;
    [SerializeField] private Color tint = new Color(1,1,1,0.8f);
    
    [SerializeField] private SpriteRenderer dashTab;
    [SerializeField] private SpriteRenderer reflectTab;
    [SerializeField] private SpriteRenderer warpTab;
    Color dashColor = Color.blue;
    
    private float timer;
    private Color defaultTint = new Color(0f, 0f, 0f, 0f);

    private SpriteRenderer playerSprite;

    void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        trail  = GetComponent<TrailRenderer>();
        
        ProgressBar[] bars   = GetComponentsInChildren<ProgressBar>();
        reflectBar  = bars[0].GetComponent<ProgressBar>();
        warpBar  = bars[1].GetComponent<ProgressBar>();
    }

    void Update()
    {
        if (player.PreWarpPos != Vector2.zero)
        {
            trail.emitting = true;
        }
        else
        {
            trail.emitting = false;
        }


        if (player.DashUsed)
        {
            dashTab.color = defaultTint;
            dashTask.UpdateBar(0f);
        }
        else
        {
            dashTab.color = dashColor;
            dashTask.UpdateBar(1f);
        }
        
        reflectBar.UpdateBar(player.ReflectVal);
        reflectTask.UpdateBar(player.ReflectVal);
        
        warpBar.UpdateBar(player.WarpProgress());
        warpTask.UpdateBar(player.WarpProgress());
    }
}
