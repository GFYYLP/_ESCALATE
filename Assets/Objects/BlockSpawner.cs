using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    [SerializeField] private AfterImage afterImagePrefab;
    
    [SerializeField] private List<Sprite> sprites;
    
    private SpriteRenderer sr;
    
    // Start is called before the first frame update
    void Start()
    {
        player.onHighCollision += spawnBlock;
        sr = GetComponent<SpriteRenderer>();
    }
    
    void spawnBlock()
    {
        
        var ball = Instantiate(afterImagePrefab, transform.position, Quaternion.identity);
        var img = Instantiate(afterImagePrefab);

        // img.Init(
        //     playerSprite.sprite,
        //     transform.position,
        //     transform.rotation,
        //     transform.localScale,
        //     tint,
        //     lifetime
        // );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
