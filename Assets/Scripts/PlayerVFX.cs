using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem dashVFX;
    [SerializeField] private Player player;
    private TrailRenderer trail;
    
    [SerializeField] private AfterImage afterImagePrefab;
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float lifetime = 0.3f;
    [SerializeField] private float speedThreshold = 10f;
    [SerializeField] private Color tint = new Color(1,1,1,0.8f);

    private float timer;

    private SpriteRenderer playerSprite;

    void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        trail  = GetComponent<TrailRenderer>();
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
        
        // float speed = body.Speed;
        // //bool isDash = player.IsDashing;
        //
        // if (speed > speedThreshold)
        // {
        //     timer += Time.deltaTime;
        //
        //     if (timer >= spawnInterval)
        //     {
        //         SpawnAfterImage();
        //         timer = 0f;
        //     }
        // }
        // else
        // {
        //     timer = 0f;
        // }
        
        
        //update tint on reflection value
        // playerSprite.color = new Color((!player.IsDashing)? tint.r : 0f, 
        //                               (!player.IsDashing)? tint.g : 0f, 
        //                               tint.b*(1f - player.ReflectVal), 1f);
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
