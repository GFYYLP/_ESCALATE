using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private PhysicsManager physicsManager;
    [SerializeField] private Block blockPrefab;
    [SerializeField] private List<Sprite> sprites;
    
    [SerializeField] private float spawnCooldown = 0.5f; // Cooldown duration in seconds
    private float lastSpawnTime = -Mathf.Infinity;
    private int spawnCounter = 0;
    
    private bool isFirstSpawn = true;

    void Start()
    {
        physicsManager.onHighCollision  += SpawnBlock;
        DoSpawn(new Vector2(0f, -5.0f));
        isFirstSpawn = false;
        lastSpawnTime = Time.time; 
    }

    void SpawnBlock(Vector2 pos)
    {
        //check if cooldown has elapsed
        //TODO: implement coroutine for better responsiveness if needed
        
        if (Time.time - lastSpawnTime >= spawnCooldown)
        {
            DoSpawn(pos);
            lastSpawnTime = Time.time; 
        }
    }

    void DoSpawn(Vector2 pos)
    {
        ++spawnCounter;
        
        var block = Instantiate(
            blockPrefab,
            new Vector3(pos.x, isFirstSpawn ? pos.y : transform.position.y, 105f),
            Quaternion.identity,
            transform
        );
        block.gameObject.layer = LayerMask.NameToLayer("Ground");
        block.sr.sprite = sprites[0];
        block.name = "Block [" + spawnCounter + "]";
        block.isKinematic = isFirstSpawn;

        if (isFirstSpawn)
        {
            float scale = 5f;

            block.transform.localScale = new Vector3(scale, scale, scale);
            
        }
    }

    void Update()
    {
        transform.position = new Vector3(0, Camera.main.transform.position.y + 6f, 0f);
    }
}