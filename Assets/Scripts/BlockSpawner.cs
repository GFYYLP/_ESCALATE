using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PhysicsManager physicsManager;
    [SerializeField] private Block blockPrefab;
    [SerializeField] private List<Sprite> sprites;
    [SerializeField] private float directorySpacing = 5f;
    
    private List<Vector2> directoryPositions;
    private int highestDirPos=0;
    
    [SerializeField] private float spawnCooldown = 0.5f; // Cooldown duration in seconds
    private float lastSpawnTime = -Mathf.Infinity;
    private int spawnCounter = 0;
    
    private bool isFirstSpawn = true;

    void Start()
    {
        physicsManager.onHighCollision  += SpawnBlock;
        DoSpawn(new Vector2(0f, -9.0f), true);
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

    void DoSpawn(Vector2 pos, bool isKinematic = false)
    {
        ++spawnCounter;
        
        var block = Instantiate(
            blockPrefab,
            new Vector3(pos.x, isFirstSpawn ? pos.y : transform.position.y, 20f),
            Quaternion.identity,
            transform
        );
        block.gameObject.layer = LayerMask.NameToLayer("Ground");
        block.name = isKinematic? "Kinematic Block" : "Block [" + spawnCounter + "]";
        block.isKinematic = isKinematic;//isFirstSpawn;

        if (isKinematic)
        {
            var sr = block.GetComponentInChildren<SpriteRenderer>();
            sr.sprite = sprites[0];
            //block.transform.localScale = new Vector3(2f, 2f, 2f);
        }

        if (isFirstSpawn)
        {
            float scale = 5f;

            block.transform.localScale = new Vector3(scale*2f, scale, scale);
            
        }
    }

    void Update()
    {
        transform.position = new Vector3(0, Camera.main.transform.position.y + 6f, 0f);
        
        //spawn a new kinematic block every time the player transform.y increases by directory spacing value
        if ((int)player.transform.position.y > highestDirPos)
        {
            highestDirPos = (int)player.transform.position.y;
            
            if (highestDirPos % directorySpacing == 0) DoSpawn(transform.position, true);
        }
    }
}