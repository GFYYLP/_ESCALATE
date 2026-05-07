using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Block blockPrefab;
    [SerializeField] private List<Sprite> sprites;
    
    [SerializeField] private float spawnCooldown = 0.5f; // Cooldown duration in seconds
    private float lastSpawnTime = -Mathf.Infinity;

    private bool isFirstSpawn = true;

    void Start()
    {
        player.onHighCollision += SpawnBlock;
        DoSpawn(new Vector2(player.transform.position.x, -2.0f));
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
        var block = Instantiate(
            blockPrefab,
            new Vector3(pos.x, isFirstSpawn ? pos.y : transform.position.y, 0f),
            Quaternion.identity
        );
        block.gameObject.layer = LayerMask.NameToLayer("Ground");
        block.sr.sprite = sprites[0];
        block.isKinematic = isFirstSpawn;
    }

    void Update()
    {
    }
}