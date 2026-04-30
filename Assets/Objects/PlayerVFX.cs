using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem dashVFX;
    [SerializeField] private PlayerMovement playerMovement;

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
    }

    void Update()
    {
        float speed = playerMovement.Speed;
        bool isDash = playerMovement.IsDashing;

        if (isDash)//speed > speedThreshold)
        {
            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                SpawnAfterImage();
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }

    void SpawnAfterImage()
    {
        var img = Instantiate(afterImagePrefab);

        img.Init(
            playerSprite.sprite,
            transform.position,
            transform.rotation,
            transform.localScale,
            tint,
            lifetime
        );
    }

}
