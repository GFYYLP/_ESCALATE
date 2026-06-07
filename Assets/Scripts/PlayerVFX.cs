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
    
    [SerializeField] private AfterImage afterImagePrefab;
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float lifetime = 0.3f;
    [SerializeField] private float speedThreshold = 10f;
    [SerializeField] private Color tint = new Color(1,1,1,0.8f);
    
    [SerializeField] private SpriteRenderer dashTab;
    [SerializeField] private SpriteRenderer reflectTab;
    [SerializeField] private SpriteRenderer warpTab;
    Color dashColor = Color.blue;
    Color reflectColor = Color.yellow;
    Color warpColor = Color.magenta;
    
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

        
        if (player.DashUsed) dashTab.color = defaultTint;
        else dashTab.color = dashColor;
        
        // dash: binary — available or not
        //dashBar.UpdateBar(player.DashUsed ? 0f : 1f);

        // reflect: also mostly binary but use ReflectVal (0, 0.3, or 1.0)
        reflectBar.UpdateBar(player.ReflectVal);
        reflectTask.UpdateBar(player.ReflectVal);
        
        // warp: continuous charge
        warpBar.UpdateBar(player.WarpProgress());
        warpTask.UpdateBar(player.WarpProgress());
    }

    IEnumerator PulseSprite(Color src, Color first, Color second)
    {
        float t = 0f;
        float pulseDuration = 2f;
        while (t < 1f)
        {
            t += Time.deltaTime / pulseDuration;
            
            //pulse src color
            src = Color.Lerp(first, second, t);
            
            yield return null;
        }
    }

    void SpawnAfterImage()
    {
        var img = Instantiate(afterImagePrefab);

        img.Init(
            playerSprite.sprite,
            new Vector3(transform.position.x, transform.position.y, transform.position.z - 1f),
            transform.rotation,
            transform.localScale,
            tint,
            lifetime
        );
    }

}
