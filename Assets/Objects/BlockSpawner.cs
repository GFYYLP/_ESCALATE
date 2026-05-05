using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private Block blockPrefab;

    [SerializeField] private List<Sprite> sprites;


    // Start is called before the first frame update
    void Start()
    {
        player.onHighCollision += spawnBlock;
    }

    void spawnBlock(Vector2 pos)
    {
        var block = Instantiate(
            blockPrefab,
            new Vector3(pos.x, transform.position.y, 0f),
            Quaternion.identity
        );
        block.gameObject.layer = LayerMask.NameToLayer("Ground");
        block.sr.sprite = sprites[0];//Random.Range(0, sprites.Count)];
    }

    // Update is called once per frame
    void Update()
    {

    }
}