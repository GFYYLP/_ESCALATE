using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private Block blockPrefab;

    [SerializeField] private List<Sprite> sprites;

    private bool isFirstSpawn = true;

    // Start is called before the first frame update
    void Start()
    {
        player.onHighCollision += spawnBlock;
        bool isBase = true;
        spawnBlock(new Vector2(player.transform.position.x, -2.0f));
        isFirstSpawn = false;
    }

    void spawnBlock(Vector2 pos)
    {
        var block = Instantiate(
            blockPrefab,
            new Vector3(pos.x, isFirstSpawn? pos.y : transform.position.y, 0f),
            Quaternion.identity
        );
        block.gameObject.layer = LayerMask.NameToLayer("Ground");
        block.sr.sprite = sprites[0];//Random.Range(0, sprites.Count)];
        block.isKinematic = isFirstSpawn;
    }

    // Update is called once per frame
    void Update()
    {

    }
}